using System;
using UnityEngine;

/// <summary>
/// 플레이어 자원의 현재값과 최대값을 함께 전달
/// </summary>
public readonly struct PlayerStatusSnapshot
{
    public float CurrentEnergy { get; }
    public int CurrentHP { get; }
    public int CurrentFood { get; }
    public int CurrentOxygen { get; }
    public float MaxEnergy { get; }
    public int MaxHP { get; }
    public int MaxFood { get; }
    public int MaxOxygen { get; }

    /// <summary>
    /// 플레이어 자원의 전체 상태를 생성
    /// </summary>
    /// <param name="currentEnergy">현재 에너지</param>
    /// <param name="currentHP">현재 체력</param>
    /// <param name="currentFood">현재 허기</param>
    /// <param name="currentOxygen">현재 산소</param>
    /// <param name="maxEnergy">최대 에너지</param>
    /// <param name="maxHP">최대 체력</param>
    /// <param name="maxFood">최대 허기</param>
    /// <param name="maxOxygen">최대 산소</param>
    public PlayerStatusSnapshot(float currentEnergy, int currentHP, int currentFood, int currentOxygen, float maxEnergy, int maxHP, int maxFood, int maxOxygen)
    {
        if (maxEnergy < 0f)
            throw new ArgumentOutOfRangeException(nameof(maxEnergy));

        if (maxHP < 0)
            throw new ArgumentOutOfRangeException(nameof(maxHP));

        if (maxFood < 0)
            throw new ArgumentOutOfRangeException(nameof(maxFood));

        if (maxOxygen < 0)
            throw new ArgumentOutOfRangeException(nameof(maxOxygen));

        if (currentEnergy < 0f || currentEnergy > maxEnergy)
            throw new ArgumentOutOfRangeException(nameof(currentEnergy));

        if (currentHP < 0 || currentHP > maxHP)
            throw new ArgumentOutOfRangeException(nameof(currentHP));

        if (currentFood < 0 || currentFood > maxFood)
            throw new ArgumentOutOfRangeException(nameof(currentFood));

        if (currentOxygen < 0 || currentOxygen > maxOxygen)
            throw new ArgumentOutOfRangeException(nameof(currentOxygen));

        CurrentEnergy = currentEnergy;
        CurrentHP = currentHP;
        CurrentFood = currentFood;
        CurrentOxygen = currentOxygen;
        MaxEnergy = maxEnergy;
        MaxHP = maxHP;
        MaxFood = maxFood;
        MaxOxygen = maxOxygen;
    }
}

/// <summary>
/// 플레이어 자원의 변경된 현재 상태를 전달
/// </summary>
public readonly struct PlayerStatusChangeSet
{
    public float CurrentEnergy { get; }
    public int CurrentHP { get; }
    public int CurrentFood { get; }
    public int CurrentOxygen { get; }

    /// <summary>
    /// 플레이어 자원의 현재 상태 변경값을 생성
    /// </summary>
    /// <param name="currentEnergy">현재 에너지</param>
    /// <param name="currentHP">현재 체력</param>
    /// <param name="currentFood">현재 허기</param>
    /// <param name="currentOxygen">현재 산소</param>
    public PlayerStatusChangeSet(float currentEnergy, int currentHP, int currentFood, int currentOxygen)
    {
        if (currentEnergy < 0f)
            throw new ArgumentOutOfRangeException(nameof(currentEnergy));

        if (currentHP < 0)
            throw new ArgumentOutOfRangeException(nameof(currentHP));

        if (currentFood < 0)
            throw new ArgumentOutOfRangeException(nameof(currentFood));

        if (currentOxygen < 0)
            throw new ArgumentOutOfRangeException(nameof(currentOxygen));

        CurrentEnergy = currentEnergy;
        CurrentHP = currentHP;
        CurrentFood = currentFood;
        CurrentOxygen = currentOxygen;
    }
}

/// <summary>
/// 클라이언트가 플레이어 자원 상태를 읽기 위한 접근점을 정의
/// </summary>
public interface IPlayerStatusReadAccess
{
    event Action<PlayerStatusSnapshot> OnStatusChanged;

    bool IsInitialized { get; }

    /// <summary>
    /// 현재 플레이어 자원 상태를 반환
    /// </summary>
    /// <returns>현재 플레이어 자원 상태</returns>
    PlayerStatusSnapshot GetSnapshot();
}

/// <summary>
/// 권한 영역에서 전달받은 플레이어 자원 상태를 클라이언트에 보관
/// </summary>
public class PlayerStatusReplica : MonoBehaviour, IPlayerStatusReadAccess
{
    private float currentEnergy;
    private int currentHP;
    private int currentFood;
    private int currentOxygen;
    private float maxEnergy;
    private int maxHP;
    private int maxFood;
    private int maxOxygen;

    public event Action<PlayerStatusSnapshot> OnStatusChanged;

    public bool IsInitialized { get; private set; }

    /// <summary>
    /// 현재 플레이어 자원 상태를 반환
    /// </summary>
    /// <returns>현재 플레이어 자원 상태</returns>
    public PlayerStatusSnapshot GetSnapshot()
    {
        if (!IsInitialized)
            throw new InvalidOperationException("Player Status Replica가 초기화되지 않음");

        return new PlayerStatusSnapshot(currentEnergy, currentHP, currentFood, currentOxygen, maxEnergy, maxHP, maxFood, maxOxygen);
    }

    /// <summary>
    /// 권한 영역의 전체 플레이어 자원 상태를 적용
    /// </summary>
    /// <param name="snapshot">적용할 전체 상태</param>
    public void ApplyFullSnapshot(PlayerStatusSnapshot snapshot)
    {
        currentEnergy = snapshot.CurrentEnergy;
        currentHP = snapshot.CurrentHP;
        currentFood = snapshot.CurrentFood;
        currentOxygen = snapshot.CurrentOxygen;
        maxEnergy = snapshot.MaxEnergy;
        maxHP = snapshot.MaxHP;
        maxFood = snapshot.MaxFood;
        maxOxygen = snapshot.MaxOxygen;

        IsInitialized = true;

        OnStatusChanged?.Invoke(GetSnapshot());
    }

    /// <summary>
    /// 권한 영역에서 전달받은 플레이어 자원 변경값을 적용
    /// </summary>
    /// <param name="changeSet">적용할 자원 변경값</param>
    public void ApplyChangeSet(PlayerStatusChangeSet changeSet)
    {
        if (!IsInitialized)
            throw new InvalidOperationException("전체 Player Status Snapshot 적용 전에 Change Set을 적용할 수 없음");

        if (changeSet.CurrentEnergy > maxEnergy)
            throw new ArgumentOutOfRangeException(nameof(changeSet), changeSet.CurrentEnergy, "현재 에너지가 최대 에너지보다 큼");

        if (changeSet.CurrentHP > maxHP)
            throw new ArgumentOutOfRangeException(nameof(changeSet), changeSet.CurrentHP, "현재 체력이 최대 체력보다 큼");

        if (changeSet.CurrentFood > maxFood)
            throw new ArgumentOutOfRangeException(nameof(changeSet), changeSet.CurrentFood, "현재 허기가 최대 허기보다 큼");

        if (changeSet.CurrentOxygen > maxOxygen)
            throw new ArgumentOutOfRangeException(nameof(changeSet), changeSet.CurrentOxygen, "현재 산소가 최대 산소보다 큼");

        currentEnergy = changeSet.CurrentEnergy;
        currentHP = changeSet.CurrentHP;
        currentFood = changeSet.CurrentFood;
        currentOxygen = changeSet.CurrentOxygen;

        OnStatusChanged?.Invoke(GetSnapshot());
    }
}
