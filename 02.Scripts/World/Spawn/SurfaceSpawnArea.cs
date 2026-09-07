using System;
using UnityEngine;

/// <summary>
/// 표면 Spawn 결과로 사용할 위치와 회전 정보
/// </summary>
public readonly struct SurfaceSpawnPoint
{
    public Vector3 Position { get; }
    public Quaternion Rotation { get; }
    public Vector3 Normal { get; }

    public SurfaceSpawnPoint(Vector3 position, Quaternion rotation, Vector3 normal)
    {
        Position = position;
        Rotation = rotation;
        Normal = normal;
    }
}

/// <summary>
/// BoxCollider 영역에서 지정 방향으로 표면 Spawn 위치 검색
/// Spawn할 오브젝트의 종류와 개체 수는 관리하지 않음
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public sealed class SurfaceSpawnArea : MonoBehaviour
{
    [Header("Surface")]
    [SerializeField]
    private LayerMask surfaceMask;

    [SerializeField]
    private Vector3 localCastDirection = Vector3.down;

    [SerializeField, Min(0.1f)]
    private float castDistance = 10f;

    [SerializeField, Range(0f, 1f)]
    private float minimumSurfaceAlignment = 0.7f;

    private BoxCollider spawnBounds;

    private void Awake()
    {
        spawnBounds = GetComponent<BoxCollider>();

        ValidateConfiguration();
    }

    /// <summary>
    /// 실행에 필요한 SpawnArea 설정 검증
    /// </summary>
    private void ValidateConfiguration()
    {
        if (localCastDirection.sqrMagnitude <= 0.0001f)
        {
            throw new InvalidOperationException("SurfaceSpawnArea의 Local Cast Direction이 0임");
        }

        if (surfaceMask.value == 0)
        {
            throw new InvalidOperationException("SurfaceSpawnArea의 Surface Mask가 설정되지 않음");
        }
    }

    /// <summary>
    /// BoxCollider 내부에서 무작위 Raycast 시작 위치 생성
    /// </summary>
    /// <returns>World 좌표로 변환된 Raycast 시작 위치</returns>
    private Vector3 CreateRandomRayOrigin()
    {
        Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f));

        Vector3 localPosition = spawnBounds.center + Vector3.Scale(spawnBounds.size, randomOffset);

        return spawnBounds.transform.TransformPoint(localPosition);
    }

    /// <summary>
    /// Prefab의 Local Up 방향을 표면 Normal에 정렬하고 무작위 회전 적용
    /// </summary>
    /// <param name="surfaceNormal">적중한 표면의 Normal</param>
    /// <returns>표면에 정렬된 World 회전</returns>
    private Quaternion CreateSurfaceRotation(Vector3 surfaceNormal)
    {
        Quaternion surfaceAlignment = Quaternion.FromToRotation(Vector3.up, surfaceNormal);

        float randomAngle = UnityEngine.Random.Range(0f, 360f);

        Quaternion randomRotation = Quaternion.AngleAxis(randomAngle, surfaceNormal);

        return randomRotation * surfaceAlignment;
    }

    /// <summary>
    /// Spawn 영역에서 생성 가능한 표면 위치 한 번 검색
    /// 간격과 다른 오브젝트의 충돌 검사는 호출자가 담당
    /// </summary>
    /// <param name="spawnPoint">검색된 표면 위치와 회전</param>
    /// <returns>조건에 맞는 표면을 찾았으면 true</returns>
    public bool TrySample(out SurfaceSpawnPoint spawnPoint)
    {
        Vector3 rayOrigin = CreateRandomRayOrigin();

        Vector3 castDirection = transform.TransformDirection(localCastDirection.normalized);

        if (!Physics.Raycast(rayOrigin, castDirection, out RaycastHit hit, castDistance, surfaceMask, QueryTriggerInteraction.Ignore))
        {
            spawnPoint = default;
            return false;
        }

        float surfaceAlignment = Vector3.Dot(hit.normal, -castDirection);

        if (surfaceAlignment < minimumSurfaceAlignment)
        {
            spawnPoint = default;
            return false;
        }

        Quaternion rotation = CreateSurfaceRotation(hit.normal);

        spawnPoint = new SurfaceSpawnPoint(hit.point, rotation, hit.normal);

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider currentBounds = GetComponent<BoxCollider>();

        if (currentBounds == null)
            return;

        Color previousColor = Gizmos.color;
        Matrix4x4 previousMatrix = Gizmos.matrix;

        Gizmos.color = Color.cyan;
        Gizmos.matrix = currentBounds.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(
            currentBounds.center,
            currentBounds.size);

        Gizmos.matrix = previousMatrix;

        if (localCastDirection.sqrMagnitude > 0.0001f)
        {
            Vector3 origin =
                currentBounds.transform.TransformPoint(
                    currentBounds.center);

            Vector3 direction =
                transform.TransformDirection(
                    localCastDirection.normalized);

            Vector3 destination =
                origin + direction * castDistance;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, destination);
            Gizmos.DrawSphere(destination, 0.1f);
        }

        Gizmos.color = previousColor;
        Gizmos.matrix = previousMatrix;
    }
}