using UnityEngine;

public enum FurnitureSpaceType
{
    Indoor,
    Outdoor
}

/// <summary>
/// 설치된 가구 정보를 보관
/// </summary>
public readonly struct FurnitureLocation 
{
    public FurnitureSpaceType SpaceType { get; }
    public BuildingRuntime OwnerBuilding { get; }
    public Vector2Int StartCell { get; }
    public int RotationStep { get; }

    public FurnitureLocation(FurnitureSpaceType spaceType, BuildingRuntime owner, Vector2Int startCell, int rotationStep)
    {
        SpaceType = spaceType;
        OwnerBuilding = owner;
        StartCell = startCell;
        RotationStep = (rotationStep % 4 + 4) % 4;
    }
}
