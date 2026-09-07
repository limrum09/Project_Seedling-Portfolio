using System.Collections.Generic;
using UnityEngine;

public class FurniturePlacementCandidate
{
    public FurnitureLocation Location { get; }
    public IReadOnlyList<Vector2Int> OccupiedCells { get; }
    public Vector3 Pos { get; }
    public Quaternion Rotation { get; }
    public Collider PlacementSurface { get; }

    public FurniturePlacementCandidate(FurnitureLocation location, IReadOnlyList<Vector2Int> occupiedCells, Vector3 pos, Quaternion roatation, Collider placementSurface)
    {
        Location = location;
        OccupiedCells = occupiedCells;
        Pos = pos;
        Rotation = roatation;
        PlacementSurface = placementSurface;
    }
}
