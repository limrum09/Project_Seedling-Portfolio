using UnityEngine;
using UnityEngine.UI;

public class TempBuildButton : MonoBehaviour
{
    [SerializeField]
    private Building building;
    [SerializeField]
    private GameObject statePanel;
    [SerializeField]
    private Image confirmRemovePanel;

    private void OnEnable()
    {
        building.OnChangedState += SetBuildingStateButtons;
        statePanel.SetActive(false);
    }

    private void OnDisable()
    {
        building.OnChangedState -= SetBuildingStateButtons;
    }

    private void SetBuildingStateButtons(BuildState state)
    {
        if(state == BuildState.BuildingSelected || state == BuildState.PreviewModifyBuilding || state == BuildState.ConfirmRemove)
            statePanel.SetActive(true);
        else 
            statePanel.SetActive(false);
    }

    public void StartBuildMode()
    {
        building.StartBuildMode();
    }

    public void StartBuild(BuildingDefine define)
    {
        building.StartSingleBuild(define);
    }

    public void StartCorridor()
    {
        building.RequestCorridorBuild();
    }

    public void StartReplace()
    {
        building.RequestBuildingReplace();
    }

    public void StartRemoveBuiliding()
    {
        if (!building.RequestBuildingRemove())
            return;

        confirmRemovePanel.gameObject.SetActive(true);
    }

    public void ConfromRemoveBuilding()
    {
        if (!building.ConfirmBuildingRemove())
            return;

        CancelRemove();
    }

    public void CancelRemove()
    {
        building.CancelCurrentAction();
        confirmRemovePanel.gameObject.SetActive(false);
    }

    public void CancelBuld()
    {
        building.CancelCurrentBuild();
    }
}
