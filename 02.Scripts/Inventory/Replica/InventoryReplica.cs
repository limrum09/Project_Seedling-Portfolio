using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 권환 인벤토리의 전체 슬롯과 무게 상태를 전달
/// </summary>
public readonly struct InventoryStateSnapshot
{
    public IReadOnlyList<InventorySlotSnapshot> Slots { get; }
    public float CurrentInventoryWeight { get; }
    public float NormalInventoryWeight{ get; }
    public float MaximumInventoryWeight { get; }

    public InventoryStateSnapshot(IReadOnlyList<InventorySlotSnapshot> slots, float currentInventoryWeight, float normalInventoryWeight, float maximumInventoryWeight)
    {
        Slots = slots;
        CurrentInventoryWeight = currentInventoryWeight;
        NormalInventoryWeight = normalInventoryWeight;
        MaximumInventoryWeight = maximumInventoryWeight;
    }
}

/// <summary>
/// 슬롯 이동이나 버리기 같은 단일 인벤토리 작업의 처리 결과
/// </summary>
public readonly struct InventoryOperationResult
{
    public bool Success { get; }
    public InventoryStopReason Reason { get; }

    private InventoryOperationResult(bool success, InventoryStopReason reason)
    {
        Success = success; 
        Reason = reason;
    }

    public static InventoryOperationResult Succeeded()
    {
        return new InventoryOperationResult(true, InventoryStopReason.None);
    }

    public static InventoryOperationResult Failed(InventoryStopReason reason)
    {
        if (reason == InventoryStopReason.None)
            throw new ArgumentOutOfRangeException(nameof(reason));

        return new InventoryOperationResult(false, reason);
    }
}

public sealed class InventoryReplica : MonoBehaviour, IInventoryReadAccess
{
    private InventorySlotSnapshot[] slotSnapshots = Array.Empty<InventorySlotSnapshot>();


    private float currentInventoryWeight;
    private float normalInventoryWeight;
    private float maximumInventoryWeight;
    private bool isInitialized;

    public event Action<InventoryChangeSet> OnInventoryChange;

    public int InvenSlotCount => slotSnapshots.Length;
    public float CurrentInventoryWeight => currentInventoryWeight;
    public float NormalInventoryWeight => normalInventoryWeight;
    public float MaximumInventoryWeight => maximumInventoryWeight;
    public bool IsInit => isInitialized;

    private void ValidateFullSnapshot(InventoryStateSnapshot stateSnapshot)
    {
        if (stateSnapshot.Slots == null)
            throw new ArgumentException("Inventory State Snapshot이 없음");

        if (stateSnapshot.CurrentInventoryWeight < 0)
            throw new ArgumentOutOfRangeException(nameof(stateSnapshot), stateSnapshot.CurrentInventoryWeight, "현재 인벤토리 무게가 음수임");

        if (stateSnapshot.NormalInventoryWeight < 0)
            throw new ArgumentOutOfRangeException(nameof(stateSnapshot), stateSnapshot.NormalInventoryWeight, "인벤토리 기본 무게가 음수임");

        if (stateSnapshot.MaximumInventoryWeight < stateSnapshot.NormalInventoryWeight)
            throw new ArgumentOutOfRangeException(nameof(stateSnapshot), stateSnapshot.MaximumInventoryWeight, "인벤토리 최대 무게가 일반 무게보다 적음");

        int slotCount = stateSnapshot.Slots.Count;
        bool[] assignedIndices = new bool[slotCount];

        for(int i = 0; i < slotCount; i++)
        {
            InventorySlotSnapshot slotSnapshot = stateSnapshot.Slots[i];

            int index = slotSnapshot.Index;

            if (index < 0 || index >= stateSnapshot.Slots.Count)
                throw new ArgumentOutOfRangeException(nameof(stateSnapshot), index, "인벤토리 범위를 벗어남");

            if (assignedIndices[index])
                throw new ArgumentException($"전체 Snapshot에 {index}번 슬롯이 중복되어 있음", nameof(stateSnapshot));

            assignedIndices[index] = true;
        }
    }

    private void ValidateChangeSet(InventoryChangeSet changeSet)
    {
        if (!isInitialized)
            throw new InvalidOperationException("전체 Snapshot 적용 전에 Delta를 적용 할 수 없음");

        if(changeSet.Snapshots == null)
            throw new ArgumentException("Inventory Change Set에 슬롯 목록이 없음", nameof(changeSet));

        if (changeSet.CurrentInventoryWeight < 0)
            throw new ArgumentOutOfRangeException(nameof(changeSet), changeSet.CurrentInventoryWeight, "현재 인벤토리 무게가 음수임");

        bool[] changedIndices = new bool[slotSnapshots.Length];

        for(int i = 0; i < changeSet.Snapshots.Count; i++)
        {             
            int index = changeSet.Snapshots[i].Index;

            if (index < 0 || index >= slotSnapshots.Length)
                throw new ArgumentOutOfRangeException(nameof(changeSet), index, "인덱스 범위 초과");

            if (changedIndices[index])
                throw new ArgumentException(nameof(changeSet), "슬롯 중복");

            changedIndices[index] = true;
        }
    }

    public InventorySlotSnapshot[] GetAllSnapshot()
    {
        return (InventorySlotSnapshot[])slotSnapshots.Clone();
    }

    public void ApplyFullSnapshot(InventoryStateSnapshot stateSnapshot)
    {
        ValidateFullSnapshot(stateSnapshot);

        IReadOnlyList<InventorySlotSnapshot> invenSlots = stateSnapshot.Slots;

        InventorySlotSnapshot[] nextSlots = new InventorySlotSnapshot[invenSlots.Count];

        for(int i = 0; i <  invenSlots.Count; i++)
        {
            InventorySlotSnapshot slotSnapshot = invenSlots[i];
            nextSlots[slotSnapshot.Index] = slotSnapshot;
        }

        slotSnapshots = nextSlots;

        currentInventoryWeight = stateSnapshot.CurrentInventoryWeight;
        normalInventoryWeight = stateSnapshot.NormalInventoryWeight;
        maximumInventoryWeight = stateSnapshot.MaximumInventoryWeight;

        isInitialized = true;

        InventorySlotSnapshot[] eventSnapshots = (InventorySlotSnapshot[])slotSnapshots.Clone();

        InventoryChangeSet changeSet = new InventoryChangeSet(eventSnapshots, currentInventoryWeight);

        OnInventoryChange?.Invoke(changeSet);
    }

    public void ApplyChangeSet(InventoryChangeSet changeSet)
    {
        ValidateChangeSet(changeSet);

        InventorySlotSnapshot[] nextSlots = (InventorySlotSnapshot[])slotSnapshots.Clone();

        InventorySlotSnapshot[] eventSnapshots = new InventorySlotSnapshot[changeSet.Snapshots.Count];

        for(int i = 0; i <  changeSet.Snapshots.Count; i++)
        {
            InventorySlotSnapshot changeSlot = changeSet.Snapshots[i];

            nextSlots[changeSlot.Index] = changeSlot;

            eventSnapshots[i] = changeSlot;
        }

        slotSnapshots = nextSlots;
        currentInventoryWeight = changeSet.CurrentInventoryWeight;

        InventoryChangeSet applyChangeSet = new InventoryChangeSet(eventSnapshots, currentInventoryWeight);

        OnInventoryChange?.Invoke(applyChangeSet);
    }
}
