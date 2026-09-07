using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct InventorySlotSnapshot
{
    public int Index { get; }
    public ItemDefine Item { get; }
    public int ItemAmount { get; }

    public InventorySlotSnapshot(int index, ItemDefine item, int itemAmount)
    {
        Index = index;
        Item = item;
        ItemAmount = itemAmount;
    }
}

public class PlayerInventory : MonoBehaviour
{
    [SerializeField]
    private List<InventorySlot> inventorySlots = new List<InventorySlot>();
    [SerializeField]
    private BagDefine bag;
    [SerializeField]
    private float inventoryWeight;

    public BagDefine CurrentBag => bag;
    public int SlotCount => inventorySlots.Count;
    public float CurrentWeight => inventoryWeight;

    public void Init(BagDefine initBag)
    {
        bag = initBag;
        int slotCount = bag.SlotCount;

        if (slotCount < 0)
            return;

        inventorySlots.Clear();

        for(int i = 0; i < slotCount; i++)
        {
            inventorySlots.Add(new InventorySlot());
        }

        inventoryWeight = 0;
    }

    public void ChanedBagDefine(BagDefine bagDefine)
    {
        bag = bagDefine;
    }

    public bool TryCreateSlotSnapshot(int index, out InventorySlotSnapshot snapshot)
    {
        if (!IsValidIndex(index))
        {
            snapshot = default;
            return false;
        }

        InventorySlot slot = inventorySlots[index];

        snapshot = new InventorySlotSnapshot(index, slot.CurrentItemDefine, slot.CurrentItemAmount);

        return true;
    }

    public InventorySlotSnapshot[] CreateAllSlotSnapshot()
    {
        InventorySlotSnapshot[] snapshots = new InventorySlotSnapshot[inventorySlots.Count];

        for(int i = 0; i <  inventorySlots.Count; i++)
        {
            InventorySlot slot = inventorySlots[i];

            snapshots[i] = new InventorySlotSnapshot(i, slot.CurrentItemDefine, slot.CurrentItemAmount);
        }

        return snapshots;
    }

    public InventorySlot GetSlot(int index)
    {
        if (!IsValidIndex(index))
            return null;

        return inventorySlots[index];
    }

    public void RecalculateWeight()
    {
        inventoryWeight = 0;
        foreach (InventorySlot slot in inventorySlots)
        {
            if (slot.IsEmpty)
                continue;

            inventoryWeight += slot.SlotWeight;
        }
    }

    public bool IsValidIndex(int index) => index >= 0 && index<inventorySlots.Count;
}
