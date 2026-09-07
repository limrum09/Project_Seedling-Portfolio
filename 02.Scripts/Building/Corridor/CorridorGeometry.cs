using System.Collections.Generic;
using UnityEngine;

public static class CorridorGeometry
{
    private static bool TryGetConnectorLcoalPos(BuildingRuntime runtime, ConnectorDirection dir, out Vector3 localPos, out Quaternion localRotation)
    {
        localPos = Vector3.zero;
        localRotation = Quaternion.identity;

        if (runtime == null)
            return false;

        BuildingConnector connector = runtime.GetConnector(dir);

        if(connector == null) 
            return false;

        Transform root = runtime.transform;

        Vector3 worldOffset = connector.transform.position - root.position;
        localPos = Quaternion.Inverse(root.rotation) * worldOffset;
        localRotation = Quaternion.Inverse(root.rotation) * connector.transform.rotation;

        return true;
    }

    public static ConnectorDirection GetGridDir(Vector3 forward)
    {
        if (Mathf.Abs(forward.x) > Mathf.Abs(forward.z))
            return forward.x >= 0f ? ConnectorDirection.Right : ConnectorDirection.Left;

        return forward.z >= 0f ? ConnectorDirection.Forward : ConnectorDirection.Back;
    }

    public static Vector2Int GetGridVector(ConnectorDirection dir)
    {
        switch (dir)
        {
            case ConnectorDirection.Forward:
                return Vector2Int.up;
            case ConnectorDirection.Back:
                return Vector2Int.down;
            case ConnectorDirection.Right:
                return Vector2Int.right;
            case ConnectorDirection.Left:
                return Vector2Int.left;
            default:
                return Vector2Int.zero;
        }
    }

    public static List<Vector2Int> GetOccupiedCells(BuildingDefine define, Vector3 modulePos, Quaternion moduleRotation, BuildingGrid grid)
    {
        Vector3 occupiedCenter = modulePos + moduleRotation * define.BuildingOffset;
        Vector3 moduleForward = moduleRotation * Vector3.forward;

        ConnectorDirection gridDir = GetGridDir(moduleForward);

        Vector2Int rotatedFootprint = define.GridFootPrint;

        if (gridDir == ConnectorDirection.Right || gridDir == ConnectorDirection.Left)
            rotatedFootprint = new Vector2Int(define.GridFootPrint.y, define.GridFootPrint.x);

        Vector2Int startCell = grid.WorldToStartCell(occupiedCenter, rotatedFootprint);

        return GridFootprintCalculator.GetOccupiedCells(startCell, define.GridFootPrint, gridDir);
    }

    public static bool TryCalculateSnapPos(BuildingRuntime module, ConnectorDirection entryDir, Vector3 targetPos,
        Quaternion targetRotation, out Vector3 modulePos, out Quaternion moduleRotation)
    {
        modulePos = Vector2.zero;
        moduleRotation = Quaternion.identity;

        if (module == null)
            return false;

        if (!TryGetConnectorLcoalPos(module, entryDir, out Vector3 localPos, out Quaternion localRotation))
            return false;

        Vector3 entryLocalPos = localPos;
        Quaternion entryLocalRotation = localRotation;

        Quaternion desiredEntryRotation = targetRotation * Quaternion.Euler(0f, 180f, 0f);

        moduleRotation = desiredEntryRotation * Quaternion.Inverse(entryLocalRotation);
        modulePos = targetPos - moduleRotation * entryLocalPos;

        return true;
    }

    public static bool TryGetModulePos(BuildingDefine define, ConnectorDirection entryDir, ConnectorDirection exitDir,
        Vector3 openPos, Quaternion openRotation, out Vector3 modulePos, out Quaternion moduleRotation, out Vector3 nextOpenPos, out Quaternion nextOpenRotation)
    {
        modulePos = Vector3.zero;
        moduleRotation = Quaternion.identity;
        nextOpenPos = Vector3.zero;
        nextOpenRotation = Quaternion.identity;

        if (define == null || define.BuildingPrefab == null)
            return false;

        BuildingRuntime runtime = define.BuildingPrefab;

        if (!TryCalculateSnapPos(runtime, entryDir, openPos, openRotation, out modulePos, out moduleRotation))
            return false;

        if (!TryGetConnectorAnchor(define, modulePos, moduleRotation, exitDir, out CorridorRouteAnchor nextAnchor))
            return false;

        nextOpenPos = nextAnchor.Position;
        nextOpenRotation = nextAnchor.Rotation;

        return true;
    }

    public static bool TryGetConnectorAnchor(BuildingDefine define, Vector3 modulePos, Quaternion moduleRotation, ConnectorDirection dir, out CorridorRouteAnchor anchor)
    {
        anchor = null;

        BuildingRuntime runtime = define.BuildingPrefab;

        if(runtime == null) 
            return false;

        if (!TryGetConnectorLcoalPos(runtime, dir, out Vector3 localPos, out Quaternion localRotation))
            return false;

        Vector3 worldPos = modulePos + moduleRotation * localPos;
        Quaternion worldRotation = moduleRotation * localRotation;

        anchor = new CorridorRouteAnchor(worldPos, worldRotation);
        return true;
    }
}
