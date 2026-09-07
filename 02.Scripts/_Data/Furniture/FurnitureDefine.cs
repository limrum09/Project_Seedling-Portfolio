using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 가구를 설치 가능한 범위를 정의
/// </summary>
public enum FurniturePlacementScope
{
    IndoorOnly,
    IndoorAndOutdoor
}

public enum FurnitureCategory
{
    All,
    Argo,
    Box,
    Station
}

/// <summary>
/// 가구의 식별 정보, prefab, grid 점유 크기와 설치 범위를 정의
/// </summary>
[CreateAssetMenu(fileName = "Furniture_", menuName = "Project/Furniture/Furniture Define")]
public  class FurnitureDefine : ScriptableObject
{
    [SerializeField]
    private Sprite icon;
    [SerializeField]
    private string furnitureUID;
    [SerializeField]
    private string displayName;
    [SerializeField]
    private string description;
    [SerializeField]
    private List<ItemRequirement> materialItemRequirements = new();
    [SerializeField]
    private FurnitureCategory category;
    [SerializeField]
    private FurnitureRuntime prefab;
    [SerializeField]
    private ProductionDefine productionDefine;
    [SerializeField]
    Vector2Int gridFootprint = Vector2Int.one;
    [SerializeField]
    private FurniturePlacementScope scope;

    public Sprite Icon => icon;
    public string FurnitureUID => furnitureUID;
    public string DisplayName => displayName;
    public string Description => description;
    public IReadOnlyList<ItemRequirement> MaterialItemRequirements => materialItemRequirements;
    public FurnitureCategory Category => category;
    public FurnitureRuntime Prefab => prefab;
    public ProductionDefine ProductionDefine => productionDefine;
    public Vector2Int GridFootprint => gridFootprint;
    public FurniturePlacementScope Scope => scope;
}
