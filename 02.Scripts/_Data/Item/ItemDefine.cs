using System.Collections.Generic;
using UnityEngine;

public enum ItemCategory
{
    Weapon,
    Consumable,
    Material
}

public enum ItemConsumableType
{
    None,
    Energy,
    Food,
    Oxygen
}

public enum ItemRarity
{
    Normal,
    Rare,
    Epic,
    Unique
}



[CreateAssetMenu(fileName = "_ItemDefine", menuName = "Project/Inventory/Item Defination")]
public class ItemDefine : ScriptableObject
{
    [SerializeField]
    private string itemUID;
    [SerializeField]
    private string displayName;
    [SerializeField]
    private string description;
    [SerializeField]
    private Sprite icon;
    [SerializeField]
    private ItemCategory category;
    [SerializeField]
    private ItemConsumableType consumableType;
    [SerializeField, Min(5)]
    private int consumbaleAmount;
    [SerializeField]
    private ItemRarity rarity;
    [SerializeField, Min(0f)]
    private float unitWeight;
    [SerializeField, Min(1)]
    private int maxStack = 1;
    [SerializeField]
    private bool cannotDiscard;

    public string ItemUID => itemUID;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public ItemCategory Category => category;
    public ItemConsumableType ConsumableType => consumableType;
    public int ConsumableAmount => consumbaleAmount;
    public ItemRarity Rarity => rarity;
    public float UnitWeight => unitWeight;
    public int MaxStack => maxStack;
    public bool CannotDiscard => cannotDiscard;
}