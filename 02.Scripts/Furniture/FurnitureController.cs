using System;
using UnityEngine;



/// <summary>
/// Furniture UI와 Mode Controller에서 사용하는 가구 배치 접근점
/// </summary>
public interface IFurniturePlacementAccess
{
    event Action<FurnitureState> OnChangedState;

    FurnitureState CurrentState { get; }

    void StartPlaceMode();
    void CancelPlaceMode();
    bool RequestFurnitureReplace();
    bool RequestFurnitureRemove();
    bool ConfirmFurnitureRemove();
    void CancelCurrentAction();
    bool Begin(FurnitureDefine define);
}

/// <summary>
/// 가구 설치와 편집 작업의 현재 상태를 정의
/// </summary>
public enum FurnitureState
{
    None,
    SelectingFurniture,
    SelectedFurniture,
    PreviewPlacement,
    PreviewRelocation,
    ConfirmRemove
}

/// <summary>
/// 선택한 FurnitureDefine과 포인터 표면을 이용해 가구 배치 상태와 Preview를 관리
/// 입력을 직접 읽거나 실제 배치 규칙을 판단하지 않음
/// </summary>
public class FurnitureController : MonoBehaviour, IFurniturePlacementAccess
{
    [SerializeField]
    private FurnitureService placementService;
    [SerializeField]
    private FurniturePreviewController previewController;

    private FurnitureState currentState;
    private FurnitureDefine currentDefine;
    private FurnitureRuntime selectedFurniture;
    private FurniturePlacementCandidate currentCandidate;
    private IInventoryConsumption inventoryConsumption;
    private RaycastHit currentSurfaceHit;
    private int currentRotationStep;
    private bool hasSurfaceHit;
    private bool currentCanPlace;

    public FurnitureState CurrentState => currentState;
    public FurnitureRuntime SelectedFurniture => selectedFurniture;
    public FurnitureDefine SelectedFurnitureDefine => selectedFurniture != null ? selectedFurniture.Define : null;
    public bool IsActive => currentState != FurnitureState.None;
    public bool CurrentCanPlace => currentCanPlace;
    public bool IsPlacing => currentState == FurnitureState.PreviewPlacement;
    public bool IsPreviewing => currentState == FurnitureState.PreviewPlacement || currentState == FurnitureState.PreviewRelocation;


    public event Action<FurnitureState> OnChangedState;
    public event Action<FurnitureDefine> OnChangedSelectedFurniture;

    /// <summary>
    /// 현재 가구 상태를 변경하고 변경 결과를 이벤트로 전달
    /// </summary>
    /// <param name="nextState">변경할 가구 상태</param>
    private void ChangeState(FurnitureState nextState)
    {
        if (currentState == nextState)
            return;

        currentState = nextState;

        OnChangedState?.Invoke(currentState);
    }

    /// <summary>
    /// 현재 표면과 회전 단계로 가구 배치 후보와 Preview를 갱신
    /// 후보를 생성할 수 없으면 Preview를 숨김
    /// </summary>
    private void RefreshCandidate()
    {
        currentCandidate = null;
        currentCanPlace = false;

        if (!hasSurfaceHit)
        {
            previewController.Hide();
            return;
        }

        if (!FurniturePlacementCalculator.TryCreateCandidate(currentDefine, currentSurfaceHit, currentRotationStep, placementService.CellSize, out FurniturePlacementCandidate candidate))
        {
            previewController.Hide();
            return;
        }

        currentCandidate = candidate;

        previewController.Show(currentDefine);
        previewController.SetPose(currentCandidate.Pos, currentCandidate.Rotation);

        if (currentState == FurnitureState.PreviewRelocation)
            currentCanPlace = placementService.CanReplace(selectedFurniture, currentCandidate);
        else
            currentCanPlace = placementService.CanPlace(currentDefine, currentCandidate, inventoryConsumption);

        previewController.SetValid(currentCanPlace);
    }

    private void ClearSelectedFurniture()
    {
        selectedFurniture = null;
        OnChangedSelectedFurniture?.Invoke(null);
    }

    private void ClearPlacement()
    {
        currentDefine = null;
        currentCandidate = null;
        currentSurfaceHit = default;
        currentRotationStep = 0;
        hasSurfaceHit = false;
        currentCanPlace = false;

        previewController.Hide();
    }

    public void StartPlaceMode()
    {
        ClearPlacement();
        ChangeState(FurnitureState.SelectingFurniture);
    }

    /// <summary>
    /// 가구 설치에 사용할 플레이어 인벤토리 소비 기능을 연결
    /// </summary>
    /// <param name="getInventoryConsumption">재료 검사와 소비를 처리할 인벤토리 기능</param>
    public void Bind(IInventoryConsumption getInventoryConsumption)
    {
        inventoryConsumption = getInventoryConsumption;
    }

    /// <summary>
    /// 지정한 가구의 배치 상태를 시작
    /// 진행 중인 배치 상태가 있으면 기존 Preview와 후보를 초기화
    /// </summary>
    /// <param name="define">배치할 가구의 정적 데이터</param>
    /// <returns>가구 배치 상태를 시작하면 true 반환</returns>
    public bool Begin(FurnitureDefine define)
    {
        if (define == null)
            return false;

        if (!IsActive)
            return false;

        if (!placementService.CheckEnoughConsume(define, inventoryConsumption))
            return false;

        ClearPlacement();

        currentDefine = define;
        currentRotationStep = 0;

        ChangeState(FurnitureState.PreviewPlacement);

        return true;
    }

    public bool SelectFurniture(FurnitureRuntime runtime)
    {
        if (currentState != FurnitureState.SelectingFurniture && currentState != FurnitureState.SelectedFurniture)
            return false;

        if (runtime == null)
            return false;

        selectedFurniture = runtime;

        OnChangedSelectedFurniture?.Invoke(selectedFurniture.Define);
        ChangeState(FurnitureState.SelectedFurniture);

        return true;
    }

    public bool RequestFurnitureReplace()
    {
        if (currentState != FurnitureState.SelectedFurniture)
            return false;

        ClearPlacement();

        currentDefine = selectedFurniture.Define;
        currentRotationStep = selectedFurniture.Location.RotationStep;

        ChangeState(FurnitureState.PreviewRelocation);

        return true;
    }

    public bool RequestFurnitureRemove()
    {
        if(currentState != FurnitureState.SelectedFurniture)
            return false;

        if(!placementService.CanRemove(selectedFurniture))
            return false;

        ChangeState(FurnitureState.ConfirmRemove);

        return true;
    }

    /// <summary>
    /// 포인터가 확인한 설치 표면을 저장하고 배치 후보를 갱신
    /// </summary>
    /// <param name="surfaceHit">설치 표면의 Raycast 결과</param>
    public void SetSurfacePointer(RaycastHit surfaceHit)
    {
        if (!IsActive)
            return;

        currentSurfaceHit = surfaceHit;
        hasSurfaceHit = true;

        RefreshCandidate();
    }

    public bool ConfirmFurnitureRemove()
    {
        if (currentState != FurnitureState.ConfirmRemove)
            return false;

        if (selectedFurniture == null)
            return false;

        if(!placementService.TryRemove(selectedFurniture))
            return false;

        ClearSelectedFurniture();
        ClearPlacement();

        ChangeState(FurnitureState.SelectingFurniture);

        return true;
    }

    
    /// <summary>
    /// 현재 가구의 90도 단위 회전 단계를 변경하고 배치 후보를 갱신
    /// </summary>
    /// <param name="step">현재 회전에 더할 회전 단계</param>
    public void Rotate(int step)
    {
        if (!IsActive)
            return;

        currentRotationStep =
            (currentRotationStep + step + 4) % 4;

        if (hasSurfaceHit)
            RefreshCandidate();
    }

    /// <summary>
    /// 현재 가구 배치 후보를 이용해 실제 가구 설치를 요청
    /// 설치에 성공하면 배치 상태를 종료
    /// </summary>
    /// <returns>가구 설치에 성공하면 true 반환</returns>
    public bool TryPlace()
    {
        if (!IsPreviewing)
            return false;

        if (!currentCanPlace || currentCandidate == null)
            return false;

        if(currentState == FurnitureState.PreviewPlacement)
        {
            if (!placementService.TryPlace(currentDefine, currentCandidate, inventoryConsumption, out FurnitureRuntime placedFurniture))
            {
                currentCanPlace = false;
                previewController.SetValid(false);

                return false;
            }

            CancelPlacement();
            ChangeState(FurnitureState.SelectingFurniture);

            return true;
        }

        if(!placementService.TryReplace(selectedFurniture, currentCandidate))
        {
            currentCanPlace = false;
            previewController.SetValid(false);

            return false;
        }

        ClearPlacement();
        ChangeState(FurnitureState.SelectedFurniture);

        return true;
    }

    public void CancelCurrentAction()
    {
        switch (currentState)
        {
            case FurnitureState.PreviewPlacement:
                CancelPlacement();
                break;
            case FurnitureState.PreviewRelocation:
                ClearPlacement();
                ChangeState(FurnitureState.SelectedFurniture);
                break;
            case FurnitureState.ConfirmRemove:
                ChangeState(FurnitureState.SelectedFurniture);
                break;
            case FurnitureState.SelectedFurniture:
                ClearSelectedFurniture();
                ChangeState(FurnitureState.SelectingFurniture);
                break;
            case FurnitureState.SelectingFurniture:
                CancelPlaceMode();
                break;
            case FurnitureState.None:
                break;
        }
    }

    /// <summary>
    /// 현재 설치 표면 정보를 제거하고 Preview를 숨김
    /// </summary>
    public void ClearSurfacePointer()
    {
        if (!IsActive)
            return;

        currentSurfaceHit = default;
        currentCandidate = null;
        hasSurfaceHit = false;
        currentCanPlace = false;

        previewController.Hide();
    }


    public void CancelPlacement()
    {
        if (!IsActive)
            return;

        ClearPlacement();
        ChangeState(FurnitureState.SelectingFurniture);
    }

    /// <summary>
    /// 현재 가구 배치 후보와 Preview를 제거하고 배치 상태를 종료
    /// </summary>
    public void CancelPlaceMode()
    {
        ClearSelectedFurniture();
        ClearPlacement();
        ChangeState(FurnitureState.None);
    }
}
