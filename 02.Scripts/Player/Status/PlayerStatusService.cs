using System;
using UnityEngine;

/// <summary>
/// 권한 영역에서 플레이어 자원 규칙을 검사하고 실제 상태를 변경
/// </summary>
public sealed class PlayerStatusService : MonoBehaviour
{
    private float currentEnergy;
    private int currentHP;
    private int currentFood;
    private int currentOxygen;
    private float maxEnergy;
    private int maxHP;
    private int maxFood;
    private int maxOxygen;
    private bool isInitialized;

    public event Action<PlayerStatusChangeSet> OnStatusChanged;

    public void Init(PlayerStatusSnapshot snapshot)
    {
        if (isInitialized)
            throw new InvalidOperationException("Player Status Service가 이미 초기화됨");

        currentEnergy = snapshot.CurrentEnergy;
        currentHP = snapshot.CurrentHP;
        currentFood = snapshot.CurrentFood;
        currentOxygen = snapshot.CurrentOxygen;
        maxEnergy = snapshot.MaxEnergy;
        maxHP = snapshot.MaxHP;
        maxFood = snapshot.MaxFood;
        maxOxygen = snapshot.MaxOxygen;

        isInitialized = true;
    }

    /// <summary>
    /// Service 초기화 여부를 검사
    /// </summary>
    private void EnsureInitialized()
    {
        if (!isInitialized)
            throw new InvalidOperationException("Player Status Service가 초기화되지 않음");
    }

    /// <summary>
    /// 현재 플레이어 자원의 전체 상태를 생성
    /// </summary>
    /// <returns>현재 플레이어 자원 상태</returns>
    public PlayerStatusSnapshot CreateStateSnapshot()
    {
        EnsureInitialized();

        return new PlayerStatusSnapshot(currentEnergy, currentHP, currentFood, currentOxygen, maxEnergy, maxHP, maxFood, maxOxygen);
    }

    /// <summary>
    /// 현재 플레이어 자원 변경 상태를 구독자에게 전달
    /// </summary>
    private void NotifyStatusChanged()
    {
        PlayerStatusChangeSet changeSet = new PlayerStatusChangeSet(currentEnergy, currentHP, currentFood, currentOxygen);

        OnStatusChanged?.Invoke(changeSet);
    }

    /// <summary>
    /// 에너지를 회복할 수 있는지 검사
    /// </summary>
    /// <param name="amount">회복할 에너지</param>
    /// <returns>에너지를 실제로 회복할 수 있으면 true</returns>
    public bool CanChargeEnergy(float amount)
    {
        EnsureInitialized();

        return amount > 0f;
    }

    /// <summary>
    /// 체력을 회복할 수 있는지 검사
    /// </summary>
    /// <param name="amount">회복할 체력</param>
    /// <returns>체력을 실제로 회복할 수 있으면 true</returns>
    public bool CanChargeHP(int amount)
    {
        EnsureInitialized();

        return amount > 0 && currentHP > 0;
    }

    /// <summary>
    /// 허기를 회복할 수 있는지 검사
    /// </summary>
    /// <param name="amount">회복할 허기</param>
    /// <returns>허기를 실제로 회복할 수 있으면 true</returns>
    public bool CanChargeFood(int amount)
    {
        EnsureInitialized();

        return amount > 0;
    }

    /// <summary>
    /// 산소를 회복할 수 있는지 검사
    /// </summary>
    /// <param name="amount">회복할 산소</param>
    /// <returns>산소를 실제로 회복할 수 있으면 true</returns>
    public bool CanChargeOxygen(int amount)
    {
        EnsureInitialized();

        return amount > 0;
    }

    /// <summary>
    /// 체력을 지정한 양만큼 감소
    /// </summary>
    /// <param name="amount">감소할 체력</param>
    /// <returns>체력이 감소했으면 true</returns>
    public bool TryConsumeHP(int amount)
    {
        EnsureInitialized();

        if (amount <= 0 || currentHP <= 0)
            return false;

        currentHP = Mathf.Max(0, currentHP - amount);

        NotifyStatusChanged();

        return true;
    }

    /// <summary>
    /// 허기를 지정한 양만큼 감소
    /// </summary>
    /// <param name="amount">감소할 허기</param>
    /// <returns>허기가 감소했으면 true</returns>
    public bool TryConsumeFood(int amount)
    {
        EnsureInitialized();

        if (amount <= 0 || currentFood <= 0)
            return false;

        currentFood = Mathf.Max(0, currentFood - amount);

        NotifyStatusChanged();

        return true;
    }

    /// <summary>
    /// 산소를 지정한 양만큼 감소
    /// </summary>
    /// <param name="amount">감소할 산소</param>
    /// <returns>산소가 감소했으면 true</returns>
    public bool TryConsumeOxygen(int amount)
    {
        EnsureInitialized();

        if (amount <= 0 || currentOxygen <= 0)
            return false;

        currentOxygen = Mathf.Max(0, currentOxygen - amount);

        NotifyStatusChanged();

        return true;
    }

    /// <summary>
    /// 요청한 양의 에너지를 전부 소비
    /// </summary>
    /// <param name="amount">소비할 에너지</param>
    /// <returns>요청한 에너지를 전부 소비했으면 true</returns>
    public bool TryConsumeEnergy(float amount)
    {
        EnsureInitialized();

        if (amount <= 0f || currentEnergy < amount)
            return false;

        currentEnergy -= amount;

        NotifyStatusChanged();

        return true;
    }

    /// <summary>
    /// 체력을 최대치 안에서 회복
    /// </summary>
    /// <param name="amount">회복할 체력</param>
    /// <param name="hp">회복 후 체력</param>
    /// <returns>체력이 실제로 회복되면 true</returns>
    public bool TryChargeHP(int amount, out int hp)
    {
        hp = currentHP;

        if (!CanChargeHP(amount))
            return false;

        currentHP = Mathf.Min(maxHP, currentHP + amount);
        hp = currentHP;

        NotifyStatusChanged();

        return true;
    }

    /// <summary>
    /// 허기를 최대치 안에서 회복
    /// </summary>
    /// <param name="amount">회복할 허기</param>
    /// <param name="food">회복 후 허기</param>
    /// <returns>허기가 실제로 회복되면 true</returns>
    public bool TryChargeFood(int amount, out int food)
    {
        food = currentFood;

        if (!CanChargeFood(amount))
            return false;

        currentFood = Mathf.Min(maxFood, currentFood + amount);
        food = currentFood;

        NotifyStatusChanged();

        return true;
    }

    /// <summary>
    /// 산소를 최대치 안에서 회복
    /// </summary>
    /// <param name="amount">회복할 산소</param>
    /// <param name="oxygen">회복 후 산소</param>
    /// <returns>산소가 실제로 회복되면 true</returns>
    public bool TryChargeOxygen(int amount, out int oxygen)
    {
        oxygen = currentOxygen;

        if (!CanChargeOxygen(amount))
            return false;

        currentOxygen = Mathf.Min(maxOxygen, currentOxygen + amount);
        oxygen = currentOxygen;

        NotifyStatusChanged();

        return true;
    }

    /// <summary>
    /// 에너지를 최대치 안에서 회복
    /// </summary>
    /// <param name="amount">회복할 에너지</param>
    /// <param name="energy">회복 후 에너지</param>
    /// <returns>에너지가 실제로 회복되면 true</returns>
    public bool TryChargeEnergy(float amount, out float energy)
    {
        energy = currentEnergy;

        if (!CanChargeEnergy(amount))
            return false;

        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + amount);
        energy = currentEnergy;

        NotifyStatusChanged();

        return true;
    }
}
