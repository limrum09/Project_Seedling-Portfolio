using UnityEngine;

public class BuildingRemoveService : MonoBehaviour
{
    [SerializeField]
    private BuildingConnectionService connectionService;
    [SerializeField]
    private GridRegistry gridRegistery;

    public bool CanRemove(BuildingRuntime runtime)
    {
        if (runtime == null)
            return false;
        if (runtime.Define == null)
            return false;

        if(!gridRegistery.TryGetOccupiedCells(runtime, out _))
            return false;

        if(!connectionService.ValidateConnections(runtime))
            return false;

        return true;
    }

    public bool TryRemove(BuildingRuntime runtime)
    {
        if (runtime == null)
            return false;

        if (!gridRegistery.TryGetOccupiedCells(runtime, out _))
            return false;

        if (!connectionService.ValidateConnections(runtime))
            return false;

        if(!connectionService.TryDisconnectAll(runtime)) 
            return false;

        gridRegistery.UnRegister(runtime);

        Destroy(runtime.gameObject);

        return true;
    }
}
