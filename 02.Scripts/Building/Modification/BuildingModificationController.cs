using UnityEngine;

public class BuildingModificationController : MonoBehaviour
{
    [SerializeField]
    private BuildPreviewController previewCtr;
    [SerializeField]
    private BuildingGrid grid;
    [SerializeField]
    private BuildingModificationService modifyService;

    private BuildingRuntime selectedRuntime;
    private BuildingPlacementCandidate currentCandidate;

    private Vector3 currentPointerPoint;
    private int currentRotationStep;

    private bool hasPointerPoint;
    private bool currentCanPlace;

    public bool IsActive => selectedRuntime != null;
    public bool CanPlace => currentCanPlace;

    private void RefreshCandidata()
    {
        currentCandidate = null;
        currentCanPlace = false;

        if (!IsActive || !hasPointerPoint)
        {
            previewCtr.SetValid(false);
            return;
        }

        if (!BuildingPlacementCalculator.TryCreateCandidate(selectedRuntime.Define, grid, currentPointerPoint, currentRotationStep, out currentCandidate))
        {
            previewCtr.SetValid(false);
            return;
        }

        currentCanPlace = modifyService.CanPlace(selectedRuntime, currentCandidate.OccupiedCells);

        previewCtr.SetPos(currentCandidate.Pos, currentCandidate.Rotation);
        previewCtr.SetValid(currentCanPlace);
    }

    public bool Begin(BuildingRuntime runtime)
    {
        if (runtime == null || runtime.Define == null)
            return false;

        if (!modifyService.CanBegin(runtime))
            return false;

        Cancel();

        selectedRuntime = runtime;

        currentRotationStep = BuildingPlacementCalculator.GetRotationStep(runtime.transform.rotation);

        currentPointerPoint = runtime.transform.position;

        hasPointerPoint = true;
        currentCandidate = null;
        currentCanPlace = false;

        previewCtr.Show(runtime.Define);

        RefreshCandidata();

        return true;
    }

    public void SetGroundPointer(Vector3 worldPointer)
    {
        if (!IsActive)
            return;

        currentPointerPoint = worldPointer;
        hasPointerPoint = true;

        RefreshCandidata();
    }

    public void Rotate(int step)
    {
        if (!IsActive)
            return;

        currentRotationStep = (currentRotationStep + step + 4) % 4;

        currentCandidate = null;
        currentCanPlace = false;

        if (hasPointerPoint)
            RefreshCandidata();
        else
            previewCtr.SetValid(false);
    }

    public bool TryApply()
    {
        if (!IsActive)
            return false;

        if (!currentCanPlace || currentCandidate == null)
            return false;

        return modifyService.TryApply(selectedRuntime, currentCandidate.OccupiedCells, currentCandidate.Pos, currentCandidate.Rotation);
    }

    public void Cancel()
    {
        selectedRuntime = null;
        currentCandidate = null;

        currentPointerPoint = Vector3.zero;
        currentRotationStep = 0;

        hasPointerPoint = false;
        currentCanPlace = false;

        previewCtr.Hide();
    }
}
