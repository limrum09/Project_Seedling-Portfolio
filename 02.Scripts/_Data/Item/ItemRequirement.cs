using System;
using UnityEngine;

[Serializable]
public sealed class ItemRequirement
{
    [SerializeField]
    private ItemDefine item;
    [SerializeField, Min(1)]
    private int amount = 1;

    public ItemDefine Item => item;
    public int Amount => amount;
}
