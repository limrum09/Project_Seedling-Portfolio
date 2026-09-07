/// <summary>
/// 채굴 요청 처리 상태
/// </summary>
public enum MiningCallStatus
{
    None,
    Success,
    PartialSuccess,
    Fail
}

/// <summary>
/// 채굴 요청 중단 사유
/// </summary>
public enum MiningStopReason
{
    None,
    InvalidGatherer,
    InvalidMineral,
    InvalidHoldItem,
    MiningNotAllowed,
    OutOfRange,
    Cooldown,
    AttackPowerTooLow,
    MineralCompleted,
    InventoryLimit
}

public class MiningResult
{
    public MiningCallStatus Status { get; }
    public MiningStopReason Reason { get; }
    public InventoryStopReason InventoryReason { get; }
    public float DamageAmount { get; }
    public ItemDefine RewardItem { get; }
    public int AddAmount { get; }
    public int DiscardAmount { get; }
    public bool MineralDepleted { get; }
    public bool MineralCompleted { get; }

    public bool Success => Status == MiningCallStatus.Success;

    /// <summary>
    /// 채굴 결과 생성
    /// </summary>
    /// <param name="status">채굴 요청 처리 상태</param>
    /// <param name="reason">채굴 요청 중단 사유</param>
    /// <param name="inventoryReason">Inventory 추가 중단 사유</param>
    /// <param name="damageAmount">실제 적용된 피해량</param>
    /// <param name="rewardItem">획득 대상 아이템</param>
    /// <param name="addAmount">실제 Inventory 추가 수량</param>
    /// <param name="discardAmount">Inventory에 추가되지 않고 폐기된 보상 수량</param>
    /// <param name="mineralDepleted">광물 체력 소진 여부</param>
    /// <param name="mineralCompleted">광물 보상 처리 완료 여부</param>
    private MiningResult(MiningCallStatus status, MiningStopReason reason, InventoryStopReason inventoryReason, float damageAmount, ItemDefine rewardItem, int addAmount, int discardAmount, bool mineralDepleted, bool mineralCompleted)
    {
        Status = status;
        Reason = reason;
        InventoryReason = inventoryReason;
        DamageAmount = damageAmount;
        RewardItem = rewardItem;
        AddAmount = addAmount;
        DiscardAmount = discardAmount;
        MineralDepleted = mineralDepleted;
        MineralCompleted = mineralCompleted;
    }

    /// <summary>
    /// 실패한 채굴 결과 생성
    /// </summary>
    /// <param name="reason">채굴 요청 중단 사유</param>
    /// <returns>실패한 채굴 결과</returns>
    public static MiningResult Failed(MiningStopReason reason)
    {
        return new MiningResult(MiningCallStatus.Fail, reason, InventoryStopReason.None, 0f, null, 0, 0, false, false);
    }

    /// <summary>
    /// 광물 체력만 감소한 채굴 결과 생성
    /// </summary>
    /// <param name="damageAmount">실제 적용된 피해량</param>
    /// <returns>피해 적용에 성공한 채굴 결과</returns>
    public static MiningResult Damaged(float damageAmount)
    {
        return new MiningResult(MiningCallStatus.Success, MiningStopReason.None, InventoryStopReason.None, damageAmount, null, 0, 0, false, false);
    }

    /// <summary>
    /// 광물 체력 소진 후 Inventory 지급 결과 생성
    /// </summary>
    /// <param name="damageAmount">실제 적용된 피해량</param>
    /// <param name="rewardItem">획득 대상 아이템</param>
    /// <param name="inventoryResult">Inventory 추가 결과</param>
    /// <param name="completed">광물 보상 처리 완료 여부</param>
    /// <returns>광물 보상 처리 결과</returns>
    public static MiningResult Rewarded(float damageAmount, ItemDefine rewardItem, InventoryAddResult inventoryResult, bool completed)
    {
        MiningCallStatus status;

        if (completed)
        {
            status = MiningCallStatus.Success;
        }
        else if (damageAmount > 0f || inventoryResult.AddAmount > 0)
        {
            status = MiningCallStatus.PartialSuccess;
        }
        else
        {
            status = MiningCallStatus.Fail;
        }

        MiningStopReason reason = completed ? MiningStopReason.None : MiningStopReason.InventoryLimit;

        return new MiningResult(status, reason, inventoryResult.Reason, damageAmount, rewardItem, inventoryResult.AddAmount, inventoryResult.RemainAmount, true, completed);
    }
}
