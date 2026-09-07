using UnityEngine;

public class TempFurnitureButton : MonoBehaviour
{
    [SerializeField]
    private FurnitureController ctr;
    [SerializeField]
    private GameObject furnitureInteractionButtons;
    [SerializeField]
    private GameObject furnitureRemovePanel;

    private void OnEnable()
    {
        ctr.OnChangedState += FurnitureStateChaned;
        FurnitureStateChaned(ctr.CurrentState);
    }

    private void OnDisable()
    {
        ctr.OnChangedState -= FurnitureStateChaned;
    }

    private void FurnitureStateChaned(FurnitureState state)
    {
        furnitureInteractionButtons.SetActive(state == FurnitureState.SelectedFurniture);
        furnitureRemovePanel.SetActive(state == FurnitureState.ConfirmRemove);
    }

    public void StartFurnitureMode()
    {
        ctr.StartPlaceMode();
    }

    public void StartFurnitureReplace()
    {
        ctr.RequestFurnitureReplace();
    }

    public void CancelFurnitureAction()
    {
        ctr.CancelCurrentAction();
    }

    public void HandleFurnitureRemove()
    {
        if (ctr.CurrentState == FurnitureState.SelectedFurniture)
        {
            ctr.RequestFurnitureRemove();
            return;
        }

        if (ctr.CurrentState == FurnitureState.ConfirmRemove)
            ctr.ConfirmFurnitureRemove();
    }

    public void StartPlacement(FurnitureDefine define)
    {
        ctr.Begin(define);
    }

    public void CanclePlacement()
    {
        ctr.CancelPlaceMode();
    }
}
