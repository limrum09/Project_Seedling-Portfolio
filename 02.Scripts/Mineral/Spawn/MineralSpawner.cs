using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MineralSpawner의 광물 재생성 방식
/// </summary>
public enum MineralRespawnMode
{
    None,
    MaintainCount
}

/// <summary>
/// 지정된 SurfaceSpawnArea에 광물을 생성하고 목표 개체 수 유지
/// </summary>
public sealed class MineralSpawner : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField]
    private SurfaceSpawnArea spawnArea;
    [SerializeField]
    private MineralSpawnProfile spawnProfile;
    [SerializeField, Min(0)]
    private int targetAliveCount = 10;
    [SerializeField, Min(1)]
    private int maxSpawnAttempts = 20;

    [Header("Placement")]
    [SerializeField]
    private LayerMask occupiedMask;
    [SerializeField, Min(0f)]
    private float baseClearanceRadius = 0.5f;
    [SerializeField, Min(0f)]
    private float surfaceOffset = 0.05f;

    [Header("Respawn")]
    [SerializeField]
    private MineralRespawnMode respawnMode = MineralRespawnMode.MaintainCount;
    [SerializeField]
    private Vector2 respawnDelayRange = new Vector2(30f, 60f);
    [SerializeField, Min(0.1f)]
    private float failedRetryDelay = 5f;

    [Header("Hierarchy")]
    [SerializeField]
    private Transform spawnedObjectRoot;

    private readonly HashSet<MineralRuntime> activeMinerals = new HashSet<MineralRuntime>();

    private Coroutine maintainPopulationCoroutine;
    private bool hasStarted;

    /// <summary>
    /// 필수 참조와 Spawn 설정 검증
    /// </summary>
    private void Awake()
    {
        ValidateConfiguration();
    }

    /// <summary>
    /// 다시 활성화되었을 때 부족한 광물 생성 재개
    /// </summary>
    private void OnEnable()
    {
        if (!hasStarted)
            return;

        BeginMaintainPopulation();
    }

    /// <summary>
    /// 최초 광물 개체 생성
    /// </summary>
    private void Start()
    {
        hasStarted = true;

        SpawnInitialPopulation();
        BeginMaintainPopulation();
    }

    /// <summary>
    /// 비활성화될 때 진행 중인 Respawn Coroutine 중단
    /// </summary>
    private void OnDisable()
    {
        if (maintainPopulationCoroutine == null)
            return;

        StopCoroutine(maintainPopulationCoroutine);
        maintainPopulationCoroutine = null;
    }

    /// <summary>
    /// 제거될 때 생성한 광물의 완료 이벤트 연결 해제
    /// </summary>
    private void OnDestroy()
    {
        foreach (MineralRuntime mineral in activeMinerals)
        {
            if (mineral == null)
                continue;

            mineral.OnCompleted -= HandleMineralCompleted;
        }

        activeMinerals.Clear();
    }

    /// <summary>
    /// Spawner와 Profile의 필수 설정 검증
    /// </summary>
    private void ValidateConfiguration()
    {
        if (spawnArea == null)
            throw new InvalidOperationException("MineralSpawner의 SurfaceSpawnArea가 연결되지 않음");

        if (spawnProfile == null)
            throw new InvalidOperationException("MineralSpawner의 MineralSpawnProfile이 연결되지 않음");

        if (respawnDelayRange.x < 0f || respawnDelayRange.y < respawnDelayRange.x)
            throw new InvalidOperationException("MineralSpawner의 Respawn Delay Range가 잘못됨");

        IReadOnlyList<MineralSpawnEntry> entries = spawnProfile.Entries;

        if (entries == null || entries.Count == 0)
            throw new InvalidOperationException("MineralSpawnProfile에 Entry가 없음");

        float totalWeight = 0f;

        for (int i = 0; i < entries.Count; i++)
        {
            MineralSpawnEntry entry = entries[i];

            if (entry == null)
                throw new InvalidOperationException($"MineralSpawnProfile의 {i}번 Entry가 비어 있음");

            if (entry.Prefab == null)
                throw new InvalidOperationException($"MineralSpawnProfile의 {i}번 Prefab이 비어 있음");

            if (entry.Weight < 0f)
                throw new InvalidOperationException($"MineralSpawnProfile의 {i}번 Weight가 음수임");

            if (entry.MinimumSize < 1f || entry.MaximumSize < entry.MinimumSize)
                throw new InvalidOperationException($"MineralSpawnProfile의 {i}번 Size 범위가 잘못됨");

            totalWeight += entry.Weight;
        }

        if (totalWeight <= 0f)
            throw new InvalidOperationException("MineralSpawnProfile의 전체 Weight가 0 이하임");
    }

    /// <summary>
    /// 목표 개체 수만큼 최초 광물 생성
    /// </summary>
    private void SpawnInitialPopulation()
    {
        int requiredCount = targetAliveCount - activeMinerals.Count;

        for (int i = 0; i < requiredCount; i++)
        {
            if (TrySpawnOne())
                continue;

            Debug.LogWarning($"MineralSpawner가 초기 광물을 모두 생성하지 못함. 현재 개체 수: {activeMinerals.Count}", this);

            break;
        }
    }

    /// <summary>
    /// Spawn 가중치에 따라 광물 Entry 하나 선택
    /// </summary>
    /// <returns>선택된 광물 Spawn Entry</returns>
    private MineralSpawnEntry SelectRandomEntry()
    {
        IReadOnlyList<MineralSpawnEntry> entries = spawnProfile.Entries;

        float totalWeight = 0f;

        for (int i = 0; i < entries.Count; i++)
        {
            totalWeight += entries[i].Weight;
        }

        float selection = UnityEngine.Random.value * totalWeight;

        MineralSpawnEntry lastAvailableEntry = null;

        for (int i = 0; i < entries.Count; i++)
        {
            MineralSpawnEntry entry = entries[i];

            if (entry.Weight <= 0f)
                continue;

            lastAvailableEntry = entry;
            selection -= entry.Weight;

            if (selection <= 0f)
                return entry;
        }

        return lastAvailableEntry;
    }

    /// <summary>
    /// 현재 Spawner가 생성한 광물과 후보 위치의 간격 확인
    /// </summary>
    /// <param name="position">검사할 Spawn 위치</param>
    /// <param name="clearanceRadius">후보 광물의 공간 확보 반경</param>
    /// <returns>다른 광물이나 장애물과 겹치면 true</returns>
    private bool IsOccupied(Vector3 position, float clearanceRadius)
    {
        foreach (MineralRuntime activeMineral in activeMinerals)
        {
            if (activeMineral == null)
                continue;

            float activeClearance = baseClearanceRadius * Mathf.Max(1f, activeMineral.MineralSize);

            float requiredDistance = clearanceRadius + activeClearance;

            float distanceSqr = (activeMineral.transform.position - position).sqrMagnitude;

            if (distanceSqr < requiredDistance * requiredDistance)
                return true;
        }

        if (clearanceRadius <= 0f || occupiedMask.value == 0)
            return false;

        return Physics.CheckSphere(position, clearanceRadius, occupiedMask, QueryTriggerInteraction.Ignore);
    }

    /// <summary>
    /// SurfaceSpawnArea에서 광물 생성이 가능한 위치 반복 검색
    /// </summary>
    /// <param name="mineralSize">생성할 광물 크기</param>
    /// <param name="spawnPoint">간격 보정까지 완료된 Spawn 위치</param>
    /// <returns>유효한 위치를 찾았으면 true</returns>
    private bool TryFindSpawnPoint(float mineralSize, out SurfaceSpawnPoint spawnPoint)
    {
        float clearanceRadius = baseClearanceRadius * mineralSize;

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            if (!spawnArea.TrySample(out SurfaceSpawnPoint sampledPoint))
                continue;

            Vector3 position = sampledPoint.Position + sampledPoint.Normal * surfaceOffset;

            if (IsOccupied(position, clearanceRadius))
                continue;

            spawnPoint = new SurfaceSpawnPoint(position, sampledPoint.Rotation, sampledPoint.Normal);

            return true;
        }

        spawnPoint = default;
        return false;
    }

    /// <summary>
    /// Profile에서 광물을 선택하고 유효한 표면 위치에 하나 생성
    /// </summary>
    /// <returns>광물을 생성했으면 true</returns>
    private bool TrySpawnOne()
    {
        MineralSpawnEntry entry = SelectRandomEntry();

        float mineralSize = UnityEngine.Random.Range(
            entry.MinimumSize,
            entry.MaximumSize);

        if (!TryFindSpawnPoint(
                mineralSize,
                out SurfaceSpawnPoint spawnPoint))
        {
            return false;
        }

        MineralRuntime mineral = Instantiate(entry.Prefab, spawnPoint.Position, spawnPoint.Rotation, spawnedObjectRoot);

        mineral.Init(mineralSize);
        mineral.OnCompleted += HandleMineralCompleted;

        activeMinerals.Add(mineral);

        return true;
    }

    /// <summary>
    /// 광물 채굴 완료 후 생존 목록에서 제거하고 Respawn 시작
    /// </summary>
    /// <param name="mineral">채굴이 완료된 광물</param>
    private void HandleMineralCompleted(MineralRuntime mineral)
    {
        mineral.OnCompleted -= HandleMineralCompleted;
        activeMinerals.Remove(mineral);

        BeginMaintainPopulation();
    }

    /// <summary>
    /// 목표 개체 수가 부족하면 Population 유지 Coroutine 시작
    /// </summary>
    private void BeginMaintainPopulation()
    {
        if (!isActiveAndEnabled)
            return;

        if (respawnMode != MineralRespawnMode.MaintainCount)
            return;

        if (activeMinerals.Count >= targetAliveCount)
            return;

        if (maintainPopulationCoroutine != null)
            return;

        maintainPopulationCoroutine = StartCoroutine(MaintainPopulation());
    }

    /// <summary>
    /// Respawn 대기 후 목표 개체 수까지 광물을 순차적으로 생성
    /// 위치 검색 실패 시 짧은 대기 후 다시 시도
    /// </summary>
    /// <returns>Coroutine 진행 상태</returns>
    private IEnumerator MaintainPopulation()
    {
        float initialDelay = UnityEngine.Random.Range(respawnDelayRange.x, respawnDelayRange.y);

        yield return new WaitForSeconds(initialDelay);

        while (activeMinerals.Count < targetAliveCount)
        {
            if (TrySpawnOne())
            {
                if (activeMinerals.Count < targetAliveCount)
                {
                    float nextDelay = UnityEngine.Random.Range(respawnDelayRange.x, respawnDelayRange.y);

                    yield return new WaitForSeconds(nextDelay);
                }

                continue;
            }

            yield return new WaitForSeconds(failedRetryDelay);
        }

        maintainPopulationCoroutine = null;
    }
}