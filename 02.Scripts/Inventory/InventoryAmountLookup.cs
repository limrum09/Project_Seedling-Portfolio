using System;
using System.Collections.Generic;

/// <summary>
/// 요구 아이템 하나의 현재 보유량과 전체 필요 수량
/// </summary>
public readonly struct ItemRequirementState
{
    public ItemDefine Item { get; }
    public int InventoryAmount { get; }
    public int RequiredAmount { get; }
    public bool HasEnough => InventoryAmount >= RequiredAmount;

    public ItemRequirementState(ItemDefine item, int inventoryAmount, int requiredAmount)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        if (inventoryAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(inventoryAmount));

        if (requiredAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(requiredAmount));

        Item = item;
        InventoryAmount = inventoryAmount;
        RequiredAmount = requiredAmount;
    }
}

/// <summary>
/// Inventory Snapshot을 아이템별 보유량으로 변환하고 요구 재료 충족 상태를 계산
/// Inventory 상태를 직접 변경하지 않음
/// </summary>
public sealed class InventoryAmountLookup
{
    private readonly Dictionary<ItemDefine, int> inventoryAmounts = new Dictionary<ItemDefine, int>();

    /// <summary>
    /// Inventory Snapshot을 이용해 아이템별 전체 보유량 생성
    /// </summary>
    /// <param name="snapshots">현재 Inventory 슬롯 상태</param>
    public InventoryAmountLookup(IReadOnlyList<InventorySlotSnapshot> snapshots)
    {
        if (snapshots == null)
            throw new ArgumentNullException(nameof(snapshots));

        for (int i = 0; i < snapshots.Count; i++)
        {
            InventorySlotSnapshot snapshot = snapshots[i];

            if (snapshot.Item == null)
            {
                if (snapshot.ItemAmount != 0)
                    throw new InvalidOperationException("아이템이 없는 Inventory Snapshot에 수량이 존재함");

                continue;
            }

            if (snapshot.ItemAmount <= 0)
                throw new InvalidOperationException("아이템이 있는 Inventory Snapshot의 수량이 0 이하임");

            if (inventoryAmounts.TryGetValue(snapshot.Item, out int currentAmount))
            {
                inventoryAmounts[snapshot.Item] = currentAmount + snapshot.ItemAmount;
            }
            else
            {
                inventoryAmounts.Add(snapshot.Item, snapshot.ItemAmount);
            }
        }
    }

    /// <summary>
    /// 중복된 ItemRequirement를 아이템별 전체 필요 수량으로 합산
    /// </summary>
    /// <param name="requirements">합산할 아이템 요구 목록</param>
    /// <param name="orderedItems">요구 목록에 처음 등장한 아이템 순서</param>
    /// <returns>아이템별 전체 필요 수량</returns>
    private static Dictionary<ItemDefine, int> CreateRequiredAmounts(IReadOnlyList<ItemRequirement> requirements, out List<ItemDefine> orderedItems)
    {
        if (requirements == null)
            throw new ArgumentNullException(nameof(requirements));

        Dictionary<ItemDefine, int> requiredAmounts = new Dictionary<ItemDefine, int>();

        orderedItems = new List<ItemDefine>();

        for (int i = 0; i < requirements.Count; i++)
        {
            ItemRequirement requirement = requirements[i];

            if (requirement == null || requirement.Item == null || requirement.Amount <= 0)
                throw new InvalidOperationException($"{i}번 Item Requirement 설정이 올바르지 않음");

            if (requiredAmounts.TryGetValue(requirement.Item, out int requiredAmount))
            {
                requiredAmounts[requirement.Item] = requiredAmount + requirement.Amount;
            }
            else
            {
                requiredAmounts.Add(requirement.Item, requirement.Amount);

                orderedItems.Add(requirement.Item);
            }
        }

        return requiredAmounts;
    }

    /// <summary>
    /// 지정한 아이템의 현재 전체 보유량 반환
    /// </summary>
    /// <param name="item">조회할 아이템</param>
    /// <returns>현재 Inventory 전체 보유량</returns>
    public int GetAmount(ItemDefine item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        inventoryAmounts.TryGetValue(item, out int amount);

        return amount;
    }

    /// <summary>
    /// 요구 목록의 아이템별 현재 보유량과 전체 필요 수량 생성
    /// 중복된 요구 아이템은 하나의 상태로 합산
    /// </summary>
    /// <param name="requirements">계산할 요구 아이템 목록</param>
    /// <returns>요구 아이템별 현재 상태</returns>
    public IReadOnlyList<ItemRequirementState> CreateRequirementStates(IReadOnlyList<ItemRequirement> requirements)
    {
        Dictionary<ItemDefine, int> requiredAmounts = CreateRequiredAmounts(requirements, out List<ItemDefine> orderedItems);

        ItemRequirementState[] states = new ItemRequirementState[orderedItems.Count];

        for (int i = 0; i < orderedItems.Count; i++)
        {
            ItemDefine item = orderedItems[i];

            states[i] = new ItemRequirementState(item, GetAmount(item), requiredAmounts[item]);
        }

        return Array.AsReadOnly(states);
    }

    /// <summary>
    /// 현재 Inventory가 모든 요구 아이템을 충족하는지 확인
    /// </summary>
    /// <param name="requirements">확인할 요구 아이템 목록</param>
    /// <returns>모든 요구량을 충족하면 true</returns>
    public bool HasEnough(IReadOnlyList<ItemRequirement> requirements)
    {
        Dictionary<ItemDefine, int> requiredAmounts = CreateRequiredAmounts(requirements, out _);

        foreach (KeyValuePair<ItemDefine, int> pair in requiredAmounts)
        {
            if (GetAmount(pair.Key) < pair.Value)
                return false;
        }

        return true;
    }
}