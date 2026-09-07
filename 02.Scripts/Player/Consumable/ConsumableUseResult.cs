using System;

/// <summary>
/// 소모품 사용이 중단된 이유를 정의
/// </summary>
public enum ConsumableUseStopReason
{
    None,
    InvalidType,
    StatusFull,
    NotEnoughItem
}

/// <summary>
/// 단일 소모품 사용 요청의 처리 결과를 전달
/// </summary>
public readonly struct ConsumableUseResult
{
    public bool Success { get; }
    public ConsumableUseStopReason Reason { get; }

    private ConsumableUseResult(bool success, ConsumableUseStopReason reason)
    {
        Success = success;
        Reason = reason;
    }

    public static ConsumableUseResult Succeeded()
    {
        return new ConsumableUseResult(true, ConsumableUseStopReason.None);
    }

    public static ConsumableUseResult Failed(ConsumableUseStopReason reason)
    {
        if (reason == ConsumableUseStopReason.None)
            throw new ArgumentOutOfRangeException(nameof(reason));

        return new ConsumableUseResult(false, reason);
    }
}
