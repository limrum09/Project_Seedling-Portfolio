using UnityEngine;

/// <summary>
/// 선택한 BuildingDefine과 포인터 위치를 이용해 단일 건물의 배치 Preview를 관리
/// 배치 후보 계산과 실제 건물 생성은 Calculator과 BuildingService에 요청
/// </summary>
public class BuildingPlacementController : MonoBehaviour
{
    [Header("Previews")]
    [SerializeField]
    private BuildPreviewController previewCtr;
    [SerializeField]
    private BuildingGrid grid;
    [SerializeField]
    private BuildingService buildingService;

    private BuildingDefine selectedBuildingDefine;
    private BuildingPlacementCandidate currentCandidate;

    private Vector3 currentPointerPoint;
    private int currentRotationStep;

    private bool hasPointerPoint;
    private bool currentCanPlace;

    public bool IsActive => selectedBuildingDefine != null;
    public bool CanPlace => currentCanPlace;

    /// <summary>
    /// 현재 포인터 위치와 회전 단계로 배치 후보를 다시 계산하고 preview 상태를 갱신
    /// 포인터 또는 배치 후보가 유효하지 않으면 배치를 불가능한 상태로 설정
    /// </summary>
    private void RefreshCandidata()
    {
        currentCandidate = null;
        currentCanPlace = false;

        if (!hasPointerPoint)
        {
            previewCtr.SetValid(false);
            return;
        }

        if(!BuildingPlacementCalculator.TryCreateCandidate(selectedBuildingDefine, grid, currentPointerPoint, currentRotationStep, out currentCandidate))
        {
            previewCtr.SetValid(false);
            return;
        }

        currentCanPlace = buildingService.CanPlace(currentCandidate.OccupiedCells);

        previewCtr.SetPos(currentCandidate.Pos, currentCandidate.Rotation);
        previewCtr.SetValid(currentCanPlace);
    }

    /// <summary>
    /// 지정한 건물의 Define으로 단일 건물 배치 Preview를 시작
    /// 기존 Preview 상태를 정리하고 포인터 입력을 기다리는 상태로 초기화
    /// </summary>
    /// <param name="define">배치할 건물의 정적 데이터</param>
    /// <returns>Preview 시작 성공 여부</returns>
    public bool Begin(BuildingDefine define)
    {
        if (define == null)
            return false;

        Cancel();
        
        selectedBuildingDefine = define;
        currentCandidate = null;

        currentPointerPoint = Vector3.zero;
        currentRotationStep = 0;

        hasPointerPoint = false;
        currentCanPlace = false;

        previewCtr.Show(selectedBuildingDefine);
        previewCtr.SetValid(false);

        return true;
    }

    /// <summary>
    /// 지면 포인터 위치를 저장하고 현재 건물의 배치 후보를 갱신
    /// </summary>
    /// <param name="worldPoint">포인터가 가리키는 위치</param>
    public void SetGroundPointer(Vector3 worldPoint)
    {
        if (!IsActive)
            return;

        currentPointerPoint = worldPoint; ;
        hasPointerPoint = true;

        RefreshCandidata();
    }

    /// <summary>
    /// 현재 건물의 회전 단계를 변경하고 배치 후보를 다시 게산
    /// </summary>
    /// <param name="step">회전 단계</param>
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

    /// <summary>
    /// 현재 배치 후보를 이용해 BuildingService에 건물 배치를 요청
    /// 배치에 실패하면 현재 Preview를 배치 불가능한 상태로 갱신
    /// </summary>
    /// <returns>건물 배치 성공 여부</returns>
    public bool TryPlace()
    {
        if (!IsActive)
            return false;

        if (!currentCanPlace)
            return false;

        bool placed = buildingService.TryPlace(selectedBuildingDefine, currentCandidate.StartCell, currentCandidate.Dir, currentCandidate.Pos, currentCandidate.Rotation, out _);

        if (!placed)
        {
            currentCanPlace = false;

            previewCtr.SetValid(false);

            return false;
        }

        return true;
    }

    /// <summary>
    /// 현재 단일 건물 배치 상태를 초기화 하고 preview를 숨김
    /// </summary>
    public void Cancel()
    {
        selectedBuildingDefine = null;
        currentCandidate = null;

        currentPointerPoint = Vector3.zero;
        currentRotationStep = 0;

        hasPointerPoint = false;
        currentCanPlace = false;

        previewCtr.Hide();
    }
}
