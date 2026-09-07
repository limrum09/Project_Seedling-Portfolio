using System;
using UnityEngine;

[Serializable]
public class InventorySlot
{
    [SerializeField]
    private ItemDefine currentItemDefine;
    [SerializeField, Min(0)]
    private int currentItemAmount;

    public ItemDefine CurrentItemDefine => currentItemDefine;
    public int CurrentItemAmount => currentItemAmount;
    public bool IsEmpty => currentItemDefine == null;
    public float SlotWeight => currentItemDefine == null ? 0 : currentItemDefine.UnitWeight * currentItemAmount;

    public bool IsSameItem(ItemDefine itemDefine)
    {
        return currentItemDefine == itemDefine;
    }

    public void SetItemSlot(ItemDefine itemDefine, int itemAmount)
    {
        currentItemDefine = itemDefine;
        currentItemAmount = itemAmount;
    }

    public void Clear()
    {
        currentItemDefine = null;
        currentItemAmount = 0;
    }
}
