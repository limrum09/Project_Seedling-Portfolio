using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 권한 영역에서 요구 아이템 소비 가능 여부와 실제 소비 기능 제공
/// </summary>
public interface IInventoryConsumption
{
    /// <summary>
    /// 현재 Inventory로 모든 요구 아이템을 소비할 수 있는지 검사
    /// </summary>
    /// <param name="requirements">요구 아이템 목록</param>
    /// <returns>모든 요구량을 충족하면 true</returns>
    bool CanConsume(IReadOnlyList<ItemRequirement> requirements);

    /// <summary>
    /// 현재 Inventory에서 모든 요구 아이템 소비
    /// </summary>
    /// <param name="requirements">소비할 아이템 목록</param>
    /// <returns>모든 요구량을 소비했으면 true</returns>
    bool TryConsume(IReadOnlyList<ItemRequirement> requirements);
}

/// <summary>
/// 권한 영역에서 Inventory에 아이템을 추가하는 기능 제공
/// </summary>
public interface IInventoryItemReceiver
{
    /// <summary>
    /// 지정한 아이템을 요청 수량만큼 Inventory에 추가
    /// </summary>
    /// <param name="itemDefine">추가할 아이템 정의</param>
    /// <param name="requestAmount">추가할 수량</param>
    /// <returns>Inventory 추가 결과</returns>
    InventoryAddResult TryAdd(ItemDefine itemDefine, int requestAmount);
}

/// <summary>
/// 권한 영역에서 Inventory 아이템을 제거하는 기능 제공
/// </summary>
public interface IInventoryItemRemover
{
    /// <summary>
    /// 지정한 UID의 아이템을 요청 수량만큼 제거
    /// </summary>
    /// <param name="itemUID">제거할 아이템 UID</param>
    /// <param name="amount">제거할 수량</param>
    /// <returns>요청 수량 전체를 제거했으면 true</returns>
    bool TryRemoveItem(string itemUID, int amount);
}

/// <summary>
/// 권한 Inventory에서 변경된 슬롯 상태와 현재 무게 전달
/// </summary>
public readonly struct InventoryChangeSet
{
    public IReadOnlyList<InventorySlotSnapshot> Snapshots { get; }
    public float CurrentInventoryWeight { get; }

    /// <summary>
    /// Inventory 변경 상태 생성
    /// </summary>
    /// <param name="snapshots">변경된 슬롯 상태</param>
    /// <param name="currentInventoryWeight">변경 후 현재 무게</param>
    public InventoryChangeSet(IReadOnlyList<InventorySlotSnapshot> snapshots, float currentInventoryWeight)
    {
        Snapshots = snapshots;
        CurrentInventoryWeight = currentInventoryWeight;
    }
}

/// <summary>
/// 권한 영역에서 Inventory 규칙을 검사하고 실제 PlayerInventory 상태 변경
/// </summary>
public class InventoryService : MonoBehaviour, IInventoryConsumption, IInventoryItemReceiver, IInventoryItemRemover
{
    [SerializeField]
    private PlayerInventory inventory;
    [SerializeField]
    private BagDefine startBag;
    [SerializeField]
    private float maxBagMultiplier = 1.4f;

    public event Action<InventoryChangeSet> OnInventoryChange;

    public int InvenSlotCount => inventory.SlotCount;
    public float NormalInventoryWeight => inventory.CurrentBag.MaxCarryWeight;
    public float MaximumInventoryWeight => inventory.CurrentBag.MaxCarryWeight * maxBagMultiplier;

    /// <summary>
    /// 계산이 끝난 추가 수량을 기존 Stack과 빈 Slot에 적용
    /// </summary>
    /// <param name="itemDefine">추가할 아이템 정의</param>
    /// <param name="addAmount">실제로 추가할 수량</param>
    /// <returns>변경된 슬롯 인덱스 목록</returns>
    private List<int> ApplyAdd(ItemDefine itemDefine, int addAmount)
    {
        int remainAmount = addAmount;
        List<int> changedIndex = new List<int>();

        for(int i = 0; i < inventory.SlotCount && remainAmount > 0; i++)
        {
            InventorySlot slot = inventory.GetSlot(i);

            if (!slot.IsSameItem(itemDefine))
                continue;

            int canAmount = itemDefine.MaxStack - slot.CurrentItemAmount;

            if (canAmount <= 0)
                continue;

            int moveAmount = Mathf.Min(canAmount, remainAmount);

            slot.SetItemSlot(slot.CurrentItemDefine, slot.CurrentItemAmount + moveAmount);

            changedIndex.Add(i);

            remainAmount -=moveAmount;
        }

        for (int i = 0; i < inventory.SlotCount && remainAmount > 0; i++)
        {
            InventorySlot slot = inventory.GetSlot(i);

            if (!slot.IsEmpty)
                continue;

            int moveAmount = Mathf.Min(itemDefine.MaxStack, remainAmount);

            slot.SetItemSlot(itemDefine, moveAmount);

            changedIndex.Add(i);

            remainAmount -=moveAmount;
        }

        if (remainAmount > 0)
            Debug.LogError("아이템이 남음");

        return changedIndex;
    }

    /// <summary>
    /// 현재 Slot 공간을 기준으로 추가할 수 있는 아이템 수량 계산
    /// </summary>
    /// <param name="item">추가할 아이템 정의</param>
    /// <param name="requestAmount">요청 수량</param>
    /// <returns>Slot 공간에 추가할 수 있는 수량</returns>
    private int CalculateSlotAddAmount(ItemDefine item, int requestAmount)
    {
        int addAmount = 0;

        for(int i = 0; i < inventory.SlotCount; i++)
        {
            InventorySlot slot = inventory.GetSlot(i);
            int canAmount;

            if (slot.IsEmpty)
                canAmount = item.MaxStack;
            else if (slot.IsSameItem(item))
                canAmount = item.MaxStack - slot.CurrentItemAmount;
            else
                continue;

            if (canAmount < 0)
                continue;

            int requiredAmount = requestAmount - addAmount;

            addAmount += Mathf.Min(canAmount, requiredAmount);

            if (addAmount >= requestAmount)
                return requestAmount;
        }

        return addAmount;
    }

    /// <summary>
    /// 현재 최대 무게를 기준으로 추가할 수 있는 아이템 수량 계산
    /// </summary>
    /// <param name="item">추가할 아이템 정의</param>
    /// <param name="requestAmount">요청 수량</param>
    /// <returns>무게 제한 안에서 추가할 수 있는 수량</returns>
    private int CalculateWeightAddAmount(ItemDefine item, int requestAmount)
    {
        if (item.UnitWeight <= 0)
            return requestAmount;

        float canWeight = MaximumInventoryWeight - inventory.CurrentWeight;

        if (canWeight <= 0)
            return 0;

        float calculatonTolerance = 0.001f;

        int addAmount = Mathf.FloorToInt((canWeight + calculatonTolerance) / item.UnitWeight);

        return Mathf.Clamp(addAmount, 0, requestAmount);
    }

    /// <summary>
    /// 모든 슬롯의 상태를 인벤토리 변경 이벤트로 전달
    /// </summary>
    private void InventoryChanged()
    {
        InventoryChangeSet changeSet = new InventoryChangeSet(inventory.CreateAllSlotSnapshot(), inventory.CurrentWeight);

        OnInventoryChange?.Invoke(changeSet);
    }

    /// <summary>
    /// 지정된 슬롯의 현재 상태를 인벤토리 변경 이벤트로 전달
    /// </summary>
    /// <param name="index">지정된 슬롯의 인덱스</param>
    private void InventoryChanged(int index)
    {
        InventorySlotSnapshot[] snapshot = new InventorySlotSnapshot[1];

        inventory.TryCreateSlotSnapshot(index, out snapshot[0]);

        InventoryChangeSet changeSet = new InventoryChangeSet(snapshot, inventory.CurrentWeight);

        OnInventoryChange?.Invoke(changeSet);
    }

    /// <summary>
    /// 2개 슬롯의 현재 상태를 인벤토리 변경 이벤트로 전달
    /// </summary>
    /// <param name="firstIndex">지정된 첫번재 슬롯의 인덱스</param>
    /// <param name="secondIndex">지정된 두번째 슬롯의 인덱스</param>
    private void InventoryChanged(int firstIndex, int secondIndex)
    {
        InventorySlotSnapshot[] snapshot = new InventorySlotSnapshot[2];

        inventory.TryCreateSlotSnapshot(firstIndex, out snapshot[0]);
        inventory.TryCreateSlotSnapshot(secondIndex, out snapshot[1]);

        InventoryChangeSet changeSet = new InventoryChangeSet(snapshot, inventory.CurrentWeight);

        OnInventoryChange?.Invoke(changeSet);
    }

    /// <summary>
    /// 지정된 여러 슬롯의 현재 상태를 이벤트 변경 이벤트로 전달
    /// </summary>
    /// <param name="index">변경된 슬롯 인덱스들의 목록</param>
    private void InventoryChanged(IReadOnlyList<int> index)
    {
        InventorySlotSnapshot[] snapshots = new InventorySlotSnapshot[index.Count];

        for(int i =0; i < index.Count; i++)
        {
            inventory.TryCreateSlotSnapshot(index[i], out snapshots[i]);
        }

        InventoryChangeSet changeSet =new InventoryChangeSet(snapshots, inventory.CurrentWeight);

        OnInventoryChange?.Invoke(changeSet);
    }


    /// <summary>
    /// 요구 목록을 ItemDefine별 전체 요구량으로 합산
    /// </summary>
    /// <param name="requirements">합산할 아이템 요구 목록</param>
    /// <returns>ItemDefine별 전체 요구량</returns>
    private Dictionary<ItemDefine, int> CreateRequiredAmounts(IReadOnlyList<ItemRequirement> requirements)
    {
        Dictionary<ItemDefine, int> requiredAmounts = new Dictionary<ItemDefine, int>();

        for (int i = 0; i < requirements.Count; i++)
        {
            ItemRequirement requirement = requirements[i];

            if (!requiredAmounts.TryAdd(requirement.Item, requirement.Amount))
            {
                requiredAmounts[requirement.Item] += requirement.Amount;
            }
        }

        return requiredAmounts;
    }

    /// <summary>
    /// ItemDefine별 요구량을 현재 인벤토리 전체 슬롯으로 충족할 수 있는지 검사
    /// </summary>
    /// <param name="requiredAmounts">ItemDefine별 전체 요구량</param>
    /// <returns>모든 요구량이 충족되면 true</returns>
    private bool HasRequiredAmounts(IReadOnlyDictionary<ItemDefine, int> requiredAmounts)
    {
        Dictionary<ItemDefine, int> remainAmounts = new Dictionary<ItemDefine, int>(requiredAmounts);

        for (int i = 0; i < inventory.SlotCount && remainAmounts.Count > 0; i++)
        {
            InventorySlot slot = inventory.GetSlot(i);

            if (slot.IsEmpty)
                continue;

            ItemDefine item = slot.CurrentItemDefine;

            if (!remainAmounts.TryGetValue(
                    item,
                    out int remainAmount))
            {
                continue;
            }

            remainAmount -= slot.CurrentItemAmount;

            if (remainAmount <= 0)
                remainAmounts.Remove(item);
            else
                remainAmounts[item] = remainAmount;
        }

        return remainAmounts.Count == 0;
    }

    public void Init()
    {
        inventory.Init(startBag);
    }

    /// <summary>
    /// 현재 Inventory의 Bag 교체를 시도
    /// </summary>
    /// <param name="newBag">교체할 Bag 정의</param>
    /// <returns>Bag을 교체했으면 true</returns>
    public bool TryChangedBag(BagDefine newBag)
    {
        if (newBag == null)
            return false;



        return true;
    }

    /// <summary>
    /// 지정한 슬롯의 현재 상태 생성을 시도
    /// </summary>
    /// <param name="index">확인할 슬롯 인덱스</param>
    /// <param name="snapshot">생성된 슬롯 상태</param>
    /// <returns>유효한 슬롯 상태를 생성했으면 true</returns>
    public bool GetSnapshot(int index, out InventorySlotSnapshot snapshot)
    {
        return inventory.TryCreateSlotSnapshot(index, out snapshot);
    }

    /// <summary>
    /// Slot과 무게 제한 안에서 지정한 아이템 추가를 시도
    /// </summary>
    /// <param name="itemDefine">추가할 아이템 정의</param>
    /// <param name="requestAmount">추가할 요청 수량</param>
    /// <returns>실제로 추가된 수량과 중단 사유</returns>
    public InventoryAddResult TryAdd(ItemDefine itemDefine, int requestAmount)
    {
        if (requestAmount <= 0)
            return InventoryAddResult.Fail(InventoryStopReason.InvalidAmount, requestAmount);

        if (itemDefine == null)
            return InventoryAddResult.Fail(InventoryStopReason.InvalidItem, requestAmount);

        if (itemDefine.Category == ItemCategory.Weapon)
            return InventoryAddResult.Fail(InventoryStopReason.ItemNotAllowed, requestAmount);

        int slotAddAmount = CalculateSlotAddAmount(itemDefine, requestAmount);

        int weightAddAmount = CalculateWeightAddAmount(itemDefine, requestAmount);

        int addAmount = Mathf.Min(slotAddAmount, weightAddAmount);

        if(addAmount <= 0)
        {
            InventoryStopReason stopReason = weightAddAmount <= slotAddAmount ? InventoryStopReason.WeightLimit : InventoryStopReason.NoEmptySlots;

            return InventoryAddResult.Fail(stopReason, requestAmount);
        }

        List<int> changedIndex = ApplyAdd(itemDefine, addAmount);

        inventory.RecalculateWeight();

        InventoryChanged(changedIndex);

        if (addAmount == requestAmount)
            return InventoryAddResult.Success(requestAmount);

        InventoryStopReason partialStopReason = weightAddAmount <= slotAddAmount ? InventoryStopReason.WeightLimit : InventoryStopReason.NoEmptySlots;

        return InventoryAddResult.PartialSuccess(partialStopReason, requestAmount, addAmount);
    }

    /// <summary>
    /// 현재 권한 Inventory의 전체 상태를 생성
    /// </summary>
    /// <returns>현재 Inventory 전체 상태</returns>
    public InventoryStateSnapshot CreateStateSnapshot()
    {
        return new InventoryStateSnapshot(inventory.CreateAllSlotSnapshot(), inventory.CurrentWeight, NormalInventoryWeight, MaximumInventoryWeight);
    }

    /// <summary>
    /// 첫 번째 슬롯의 아이템을 두 번째 슬롯으로 이동하거나 두 슬롯을 교환
    /// </summary>
    /// <param name="firstIndex">이동을 시작할 슬롯 인덱스</param>
    /// <param name="secondIndex">도착할 슬롯 인덱스</param>
    /// <returns>슬롯 이동 처리 결과</returns>
    public InventoryOperationResult TryMoveSlot(int firstIndex, int secondIndex)
    {
        if (firstIndex == secondIndex)
            return InventoryOperationResult.Failed(InventoryStopReason.SameSlot);

        if (!inventory.IsValidIndex(firstIndex))
            return InventoryOperationResult.Failed(InventoryStopReason.InvalidSlot);

        if (!inventory.IsValidIndex(secondIndex))
            return InventoryOperationResult.Failed(InventoryStopReason.InvalidSlot);

        InventorySlot firstSlot = inventory.GetSlot(firstIndex);
        InventorySlot secondSlot = inventory.GetSlot(secondIndex);

        if (firstSlot.IsEmpty)
            return InventoryOperationResult.Failed(InventoryStopReason.SourceSlotEmpty);


        ItemDefine firstItem = firstSlot.CurrentItemDefine;
        int firstItemAmount = firstSlot.CurrentItemAmount;

        if (secondSlot.IsEmpty)
        {
            secondSlot.SetItemSlot(firstItem, firstItemAmount);
            firstSlot.Clear();
        }
        else if (secondSlot.IsSameItem(firstItem))
        {
            int empytAmount = firstItem.MaxStack - secondSlot.CurrentItemAmount;

            if (empytAmount <= 0)
                return InventoryOperationResult.Failed(InventoryStopReason.DestinationStackFull);

            int moveAmount = Mathf.Min(firstItemAmount, empytAmount);

            secondSlot.SetItemSlot(firstItem, secondSlot.CurrentItemAmount + moveAmount);

            int remainAmount = firstItemAmount - moveAmount;

            if (remainAmount <= 0)
                firstSlot.Clear();
            else
                firstSlot.SetItemSlot(firstItem, remainAmount);
        }
        else
        {
            ItemDefine secondItem = secondSlot.CurrentItemDefine;
            int secondItemAmount = secondSlot.CurrentItemAmount;

            firstSlot.SetItemSlot(secondItem, secondItemAmount);
            secondSlot.SetItemSlot(firstItem, firstItemAmount);
        }

        InventoryChanged(firstIndex, secondIndex);

        return InventoryOperationResult.Succeeded();
    }

    /// <summary>
    /// 지정한 ItemUID의 아이템을 여러 슬롯에 걸쳐 요청 수량 만큼 제거
    /// 전체 보유량이 부족하면 어떤 슬롯도 변경하지 않음
    /// </summary>
    /// <param name="itemUID">제거할 아이템의 UID</param>
    /// <param name="amount">제거할 수량</param>
    /// <returns>요청 수량 전체를 제거했으면 true</returns>
    public bool TryRemoveItem(string itemUID, int amount)
    {
        if(string.IsNullOrEmpty(itemUID))
            return false;

        if(amount <= 0) 
            return false;

        int totalAmount = 0;

        for(int i = 0; i < InvenSlotCount; i++)
        {
            InventorySlot slot = inventory.GetSlot(i);

            if(slot.IsEmpty)
                continue;

            if (!string.Equals(slot.CurrentItemDefine.ItemUID, itemUID, StringComparison.Ordinal))
                continue;

            totalAmount += slot.CurrentItemAmount;
        }

        if (totalAmount < amount)
            return false;

        int remainAmount = amount;
        List<int> changedIndex = new List<int>();

        for(int i = 0; i < InvenSlotCount && remainAmount > 0; i++)
        {
            InventorySlot slot = inventory.GetSlot(i);

            if (slot.IsEmpty)
                continue;

            if (!string.Equals(slot.CurrentItemDefine.ItemUID, itemUID, StringComparison.Ordinal))
                continue;

            int removeAmount = Mathf.Min(slot.CurrentItemAmount, remainAmount);
            int newAmount = slot.CurrentItemAmount - removeAmount;

            if (newAmount <= 0)
                slot.Clear();
            else
                slot.SetItemSlot(slot.CurrentItemDefine, newAmount);

            remainAmount -= removeAmount;
            changedIndex.Add(i);
        }

        inventory.RecalculateWeight();
        InventoryChanged(changedIndex);

        return true;
    }

    /// <summary>
    /// 지정한 슬롯의 아이템을 제거
    /// </summary>
    /// <param name="index">버릴 슬롯 인덱스</param>
    /// <returns>슬롯 버리기 처리 결과</returns>
    public InventoryOperationResult TryDiscardSlot(int index)
    {
        if (!inventory.IsValidIndex(index))
            return InventoryOperationResult.Failed(InventoryStopReason.InvalidSlot);

        InventorySlot slot = inventory.GetSlot(index);

        if (slot.IsEmpty)
            return InventoryOperationResult.Failed(InventoryStopReason.SourceSlotEmpty);

        if (slot.CurrentItemDefine.CannotDiscard)
            return InventoryOperationResult.Failed(InventoryStopReason.NotDiscardable);

        slot.Clear();

        inventory.RecalculateWeight();

        InventoryChanged(index);

        return InventoryOperationResult.Succeeded();
    }

    /// <summary>
    /// 현재 인벤토리로 모든 요구 재료를 소비할 수 있는지 검가
    /// </summary>
    /// <param name="requirements">요구 아이템 목록</param>
    /// <returns>재료가 충분하면 true</returns>
    public bool CanConsume(IReadOnlyList<ItemRequirement> requirements)
    {
        Dictionary<ItemDefine, int> requiredAmounts = CreateRequiredAmounts(requirements);

        return HasRequiredAmounts(requiredAmounts);
    }

    /// <summary>
    /// 모든 요구량을 충족할 수 있을 때만 Inventory에서 요구 아이템을 소비
    /// </summary>
    /// <param name="requirements">소비할 아이템 목록</param>
    /// <returns>모든 요구량을 소비했으면 true</returns>
    public bool TryConsume(IReadOnlyList<ItemRequirement> requirements)
    {
        Dictionary<ItemDefine, int> requiredAmounts = CreateRequiredAmounts(requirements);

        if(!HasRequiredAmounts(requiredAmounts))
            return false;

        if (requiredAmounts.Count == 0)
            return true;

        List<int> changedIndex = new List<int>();

        for (int i = 0; i < inventory.SlotCount && requiredAmounts.Count > 0; i++)
        {
            InventorySlot slot = inventory.GetSlot(i);

            if (slot.IsEmpty)
                continue;

            ItemDefine item = slot.CurrentItemDefine;

            if (!requiredAmounts.TryGetValue(item, out int remainAmount))
            {
                continue;
            }

            int consumeAmount = Mathf.Min(slot.CurrentItemAmount, remainAmount);

            int newSlotAmount =slot.CurrentItemAmount - consumeAmount;

            if (newSlotAmount <= 0)
                slot.Clear();
            else
                slot.SetItemSlot(item, newSlotAmount);

            remainAmount -= consumeAmount;

            if (remainAmount <= 0)
                requiredAmounts.Remove(item);
            else
                requiredAmounts[item] = remainAmount;

            changedIndex.Add(i);
        }

        inventory.RecalculateWeight();
        InventoryChanged(changedIndex);

        return true;
    }
}
