using System;
using UnityEngine;

public enum WeaponCategory
{
    All,
    Pistol,
    Rifle,
    Raser
}

[Flags]
public enum HoldItemCategory
{
    None = 0,
    Combat = 1 << 0,
    Mining = 1 << 1
}

[CreateAssetMenu(fileName ="_HoldItemDefine", menuName = "Project/Combat/Hold Item Defination")]
public sealed class HoldItemDefine : ItemDefine
{
    [SerializeField]
    private WeaponCategory weaponCategory;
    [SerializeField]
    private HoldItemCategory holdCategory;
    [SerializeField, Min(1f)]
    private float itemDamage;
    [SerializeField, Min(0f)]
    private float attackCooldown;
    [SerializeField, Min(5f)]
    private float attackRange;
    [SerializeField, Min(0.1f)]
    private float energyCost;

    public WeaponCategory WeaponCategory => weaponCategory;
    public HoldItemCategory HoldCategory => holdCategory;
    public string ItemID => ItemUID;
    public float ItemDamage => itemDamage;
    public float AttackCooldown => attackCooldown;
    public float AttackRange => attackRange;
    public float EnergyCost => energyCost;

    public bool CanMine => (holdCategory & HoldItemCategory.Mining) != 0;
}
