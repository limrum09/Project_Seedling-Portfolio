using System.Collections.Generic;
using UnityEngine;

public class BuildingModificationService : MonoBehaviour
{
    [SerializeField]
    private GridRegistry gridRegister;
    [SerializeField]
    private BuildValidator validator;
    [SerializeField]
    private BuildingConnectionService connectionService;

    public bool CanBegin(BuildingRuntime runtime)
    {
        if (runtime == null)
            return false;

        if (runtime.Define == null)
            return false;

        if (!gridRegister.TryGetOccupiedCells(runtime, out _))
            return false;

        return connectionService.ValidateConnections(runtime);
    }

    public bool CanPlace(BuildingRuntime runtime, IReadOnlyList<Vector2Int> cells)
    {
        if (!CanBegin(runtime))
            return false;

        return gridRegister.CanOccupy(cells, runtime);
    }

    public bool TryApply(BuildingRuntime runtime, IReadOnlyList<Vector2Int> newCells, Vector3 newPos, Quaternion newRotation)
    {
        if (!CanBegin(runtime))
            return false;

        if (newCells == null)
            return false;

        if (newCells.Count == 0)
            return false;

        if (!connectionService.ValidateConnections(runtime))
            return false;

        if (!gridRegister.CanUpdateRegist(runtime, newCells))
            return false;

        if(!connectionService.TryDisconnectAll(runtime)) 
            return false;

        if(!gridRegister.TryUpdateRegist(runtime, newCells))
            return false;

        runtime.transform.SetPositionAndRotation(newPos, newRotation);

        if(!connectionService.TryConnectNewBuilding(runtime, gridRegister.RegisteredBuildings))
            return false;

        return true;
    }
}
