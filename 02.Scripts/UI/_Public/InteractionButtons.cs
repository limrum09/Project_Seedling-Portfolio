using UnityEditor.Search;
using UnityEngine;

public class InteractionButtons : MonoBehaviour
{
    [Header("Build")]
    [SerializeField]
    private GameObject buildingStatePanel;
    [SerializeField]
    private GameObject buildingConfirmRemovePanel;

    [Header("Furniture")]
    [SerializeField]
    private GameObject furnitureStatePanel;
    [SerializeField]
    private GameObject furnitureConfirmRemovePanel;

    private IBuildingPlacementAccess building;
    private IFurniturePlacementAccess furniture;

    private void OnDisable()
    {
        building.OnChangedState -= BuildStateChanged;
        furniture.OnChangedState -= FurnitureStateChanged;
    }

    private void BuildStateChanged(BuildState state)
    {
        bool showBuildingPanel = state == BuildState.BuildingSelected || state == BuildState.PreviewModifyBuilding || state == BuildState.ConfirmRemove;

        buildingStatePanel.SetActive(showBuildingPanel);
        buildingConfirmRemovePanel.SetActive(state == BuildState.ConfirmRemove);
    }

    private void FurnitureStateChanged(FurnitureState state)
    {
        furnitureStatePanel.SetActive(state == FurnitureState.SelectedFurniture);
        furnitureConfirmRemovePanel.SetActive(state == FurnitureState.ConfirmRemove);
    }

    public void BuildingCorridorStart()
    {
        building.RequestCorridorBuild();
    }

    public void BuildingReplaceStart()
    {
        building.RequestBuildingReplace();
    }

    public void BuilidingRemoveStart()
    {
        building.RequestBuildingRemove();
    }

    public void BuildingConfrimRemove()
    {
        building.ConfirmBuildingRemove();
    }

    public void BuildingRemoveCancel()
    {
        building.CancelCurrentAction();
    }

    public void FurnitureReplaceStart()
    {
        furniture.RequestFurnitureReplace();
    }

    public void FurnitureRemoveStart()
    {
        if (furniture.CurrentState != FurnitureState.SelectedFurniture)
            return;

        furniture.RequestFurnitureRemove();
    }

    public void FurnitureConfirmRemove()
    {
        if (furniture.CurrentState != FurnitureState.ConfirmRemove)
            return;

        furniture.ConfirmFurnitureRemove();
    }

    public void FurnitureRemoveCancel()
    {
        furniture.CancelCurrentAction();
    }

    public void Bind(IBuildingPlacementAccess getBuilding, IFurniturePlacementAccess getFurniture)
    {
        building = getBuilding;
        furniture = getFurniture;

        building.OnChangedState += BuildStateChanged;
        furniture.OnChangedState += FurnitureStateChanged;        

        BuildStateChanged(building.CurrentState);
        FurnitureStateChanged(furniture.CurrentState);
    }
}
