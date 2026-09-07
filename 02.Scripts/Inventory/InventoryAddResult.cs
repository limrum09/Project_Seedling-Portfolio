using System;
using UnityEngine;

public enum InventoryCallStatus
{
    None,
    Success,
    PartialSuccess,
    Fail
}

public enum InventoryStopReason
{
    None,
    InvalidItem,
    InvalidAmount,
    InvalidSlot,
    ItemNotAllowed,
    NoEmptySlots,
    WeightLimit,
    NotEnoughAmount,
    NotDiscardable,
    SameSlot,
    SourceSlotEmpty,
    DestinationStackFull
}

public class InventoryAddResult
{
    public InventoryCallStatus Status { get; private set; }
    public InventoryStopReason Reason { get; private set; }
    public int RequestAmount { get; private set; }
    public int AddAmount { get; private set; }
    public int RemainAmount {  get; private set; }

    private InventoryAddResult(InventoryCallStatus resultCode, InventoryStopReason stopCode, int requestAmount, int addAmount)
    {
        Status = resultCode;
        Reason = stopCode;
        RequestAmount = requestAmount;
        AddAmount = addAmount;
        RemainAmount = Mathf.Max(0, requestAmount - addAmount);
    }

    public static InventoryAddResult Success(int requestAmount)
    {
        if(requestAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestAmount));

        return new InventoryAddResult(InventoryCallStatus.Success, InventoryStopReason.None, requestAmount, requestAmount);
    }

    public static InventoryAddResult PartialSuccess(InventoryStopReason stopReason, int requestAmount, int addAmount)
    {
        if (requestAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestAmount));

        if (stopReason == InventoryStopReason.None)
            throw new ArgumentOutOfRangeException(nameof(stopReason));

        if (addAmount <= 0 || addAmount >= requestAmount)
            throw new ArgumentOutOfRangeException(nameof(addAmount));

        return new InventoryAddResult(InventoryCallStatus.PartialSuccess, stopReason, requestAmount, addAmount);
    }

    public static InventoryAddResult Fail(InventoryStopReason stopReason, int requestAmount)
    {
        if (stopReason != InventoryStopReason.InvalidAmount && requestAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestAmount));

        if(stopReason == InventoryStopReason.None)
            throw new ArgumentOutOfRangeException(nameof(stopReason));

        return new InventoryAddResult(InventoryCallStatus.Fail, stopReason, requestAmount, 0);
    }
}