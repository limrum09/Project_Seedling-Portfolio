using System;
using UnityEngine;

/// <summary>
/// Building UI와 Mode Controller에서 사용하는 건설 접근점
/// </summary>
public interface IBuildingPlacementAccess
{
    event Action<BuildState> OnChangedState;

    bool RequestCorridorBuild();
    bool RequestBuildingReplace();
    bool RequestBuildingRemove();
    bool ConfirmBuildingRemove();
    void StartBuildMode();
    void StartSingleBuild(BuildingDefine define);
    void ExitBuildMode();

    /// <summary>
    /// 현재 진행 중인 건설 작업 취소
    /// </summary>
    void CancelCurrentAction();

    BuildState CurrentState { get; }
}

/// <summary>
/// 건설 시스템의 현재 작업 상태를 정의
/// 각 상태는 입력을 전달할 대상과 실행할 수 있는 건설 행동을 결정
/// </summary>
public enum BuildState
{
    None,
    Idle,
    BuildingSelected,
    PreviewNewBuilding,
    PreviewCorridor,
    PreviewModifyBuilding,
    ConfirmRemove
}

/// <summary>
/// 건물의 기능과 통로 구성 방식에 따른 분류 정의
/// </summary>
public enum BuildingCategory
{
    All,
    Facility,
    Installation,
    Corridor,
}

/// <summary>
/// 건설 모드의 상태와 선택된 Runtime 건물을 관리하고 건설 요청을 알맞은 Controller와 Service에 전달
/// 실제 배치할 위치 계산, 건물 생성 및 연결, 수정과 철거 규칙은 직접 실행하지 않음
/// </summary>
public class Building : MonoBehaviour, IBuildingPlacementAccess
{
    [Header("Components")]
    [SerializeField]
    private BuildingPlacementController placementCtr;
    [SerializeField]
    private BuildingModificationController modifyCtr;
    [SerializeField]
    private CorridorConstructionController corridorCtr;
    [SerializeField]
    private BuildingRemoveService removeService;

    private BuildState currentState;
    private BuildingRuntime selectedRuntime;
    private BuildingAction currentBuildingAction = BuildingAction.None;
    private IInventoryConsumption inventoryConsumption;

    public bool IsBuildMode => currentState != BuildState.None;
    public BuildState CurrentState => currentState;
    public BuildingRuntime SelectedRuntime => selectedRuntime;
    public BuildingAction CurrentBuildingAction => currentBuildingAction;

    public event Action<BuildState> OnChangedState;


    private void ChangedState(BuildState nextState)
    {
        if (currentState == nextState)
            return;

        ExitState(currentState);
        currentState = nextState;
        EnterState(currentState);

        OnChangedState?.Invoke(currentState);
    }

    private void RefreshSelectAction()
    {
        if (selectedRuntime == null || selectedRuntime.Define == null)
        {
            currentBuildingAction = BuildingAction.None;
            return;
        }

        currentBuildingAction = BuildingActionPolicy.Evaluate(selectedRuntime);
    }

    private void EnterState(BuildState getState)
    {
        switch (getState)
        {
            case BuildState.BuildingSelected:
                RefreshSelectAction();
                break;
            case BuildState.Idle:
            case BuildState.None:
                ClearSelection();
                break;
        }
    }

    private void ExitState(BuildState getState)
    {
        switch (getState)
        {
            case BuildState.PreviewNewBuilding:
                placementCtr.Cancel();
                break;
            case BuildState.PreviewCorridor:
                corridorCtr.Cancel();
                break;
            case BuildState.PreviewModifyBuilding:
                modifyCtr.Cancel();
                break;
        }
    }

    private void ClearSelection()
    {
        if(selectedRuntime != null)
            selectedRuntime.SetSelected(false);

        selectedRuntime = null;
        currentBuildingAction = BuildingAction.None;
    }

    /// <summary>
    /// 건물 설치에 사용할 플레이어 인벤토리 소비 기능을 연결
    /// </summary>
    /// <param name="getInventoryConsumption">재료 검사와 소비를 처리할 인벤토리 기능</param>
    public void Bind(IInventoryConsumption getInventoryConsumption)
    {
        inventoryConsumption = getInventoryConsumption;
    }

    /// <summary>
    /// 건설 대기 또는 건물 선택 상태에서 Runtime 건물 선택
    /// 선택에 성공하면 이전 선택 표시를 해제하고 현재 건물에서 실행할 수 있는 행동을 갱신
    /// </summary>
    /// <param name="runtime">선택할 Runtime 건물</param>
    /// <returns>건물 선택 성공 여부</returns>
    public bool SelectBuilding(BuildingRuntime runtime)
    {
        if (currentState != BuildState.Idle && currentState != BuildState.BuildingSelected)
            return false;

        if (runtime == null)
            return false;

        if (runtime.Define == null)
            return false;

        if (selectedRuntime != runtime)
        {
            if (selectedRuntime != null)
                selectedRuntime.SetSelected(false);

            selectedRuntime = runtime;
            selectedRuntime.SetSelected(true);
        }

        currentBuildingAction = BuildingActionPolicy.Evaluate(runtime);

        ChangedState(BuildState.BuildingSelected);

        return true;
    }

    /// <summary>
    /// 선택된 건물에서 통로 건설 시작을 요청
    /// 통로 건설이 허용되고 Controller가 시작에 성공하면 통로 Preview 상태로 전환
    /// </summary>
    /// <returns>통로 건설 시작 성공 여부</returns>
    public bool RequestCorridorBuild()
    {
        if (currentState != BuildState.BuildingSelected)
            return false;

        if (!currentBuildingAction.CanBuildCorridor)
            return false;

        if(!corridorCtr.Begin(selectedRuntime))
            return  false;

        ChangedState(BuildState.PreviewCorridor);

        return true;
    }

    /// <summary>
    /// 선택된 건물의 위치 또는 회전 수정을 요청
    /// 수정이 허용되고 Controller가 시작에 성공하면 건물 수정 Preview상태로 전환
    /// </summary>
    /// <returns>건물 수정 시작 성공 여부</returns>
    public bool RequestBuildingReplace()
    {
        if (currentState != BuildState.BuildingSelected)
            return false;

        if (!currentBuildingAction.CanModify)
            return false;

        if(!modifyCtr.Begin(selectedRuntime))
            return false;

        ChangedState(BuildState.PreviewModifyBuilding);

        return true;
    }

    /// <summary>
    /// 선택된 건물의 처거 가능 여부를 확인, 철거 확인 상태로 전환
    /// 실제 건물 철거는 수행하지 않음
    /// </summary>
    /// <returns>철거 요청 여부</returns>
    public bool RequestBuildingRemove()
    {
        if (currentState != BuildState.BuildingSelected)
            return false;

        if(!currentBuildingAction.CanRemove)
            return false;

        if(!removeService.CanRemove(selectedRuntime))
            return false;

        ChangedState(BuildState.ConfirmRemove);

        return true;
    }

    /// <summary>
    /// 철거 확인 상태에서 선택된 건물의 철거를 요청
    /// 철거에 성공하면 건설 대기 상태로 전환
    /// </summary>
    /// <returns>건물 철거 성공 여부</returns>
    public bool ConfirmBuildingRemove()
    {
        if (currentState != BuildState.ConfirmRemove)
            return false;

        if (!removeService.TryRemove(selectedRuntime))
            return false;

        ChangedState(BuildState.Idle);

        return true;
    }

    /// <summary>
    /// 지정한 건물 정의를 이용해 단일 건물 배치를 시작
    /// 진행 중인 건설 행동을 정리하고 새 건물 Preivew 상태로 전환
    /// </summary>
    /// <param name="getDefine">배치할 건물의 정적 데이터</param>
    public void StartSingleBuild(BuildingDefine getDefine)
    {
        if (currentState == BuildState.None)
            return;

        if (getDefine == null)
            return;

        if (currentState != BuildState.Idle)
            CancelCurrentBuild();

        corridorCtr.Cancel();

        if (!placementCtr.Begin(getDefine))
            return;

        ChangedState(BuildState.PreviewNewBuilding);
    }

    public void StartBuildMode()
    {
        ChangedState(BuildState.Idle);
    }


    /// <summary>
    /// 현재 Preivew 중인 신규 건물 또는 수정 중인 건물의 회전을 요청
    /// </summary>
    /// <param name="step">90도 단위로 적용할 회전의 단계</param>
    public void RotateCurrentBuilding(int step)
    {
        if (currentState == BuildState.PreviewNewBuilding)
        {
            placementCtr.Rotate(step);
            return;
        }

        if(currentState == BuildState.PreviewModifyBuilding)
        {
            modifyCtr.Rotate(step);
            return;
        }
    }

    /// <summary>
    /// 현재 Preview 중인 신규 건물 또는 수정 중인 건물에 마우스의 지면 포인터 위치를 전달
    /// </summary>
    /// <param name="getWorldPoint">포인터가 가리키는 월드 위치</param>
    public void SetGroundPointer(Vector3 getWorldPoint)
    {
        if (currentState == BuildState.PreviewNewBuilding)
        {
            placementCtr.SetGroundPointer(getWorldPoint);
            return;
        }

        if(currentState == BuildState.PreviewModifyBuilding)
        {
            modifyCtr.SetGroundPointer(getWorldPoint);
            return;
        }
    }

    /// <summary>
    /// 현재 건설 상태에 따라 건물 배치, 수정 적용 또는 통로 배치를 요청
    /// 요청에 성공하면 해당 작업을 종료하고 다른 상태로 전환
    /// </summary>
    public void SetPrimaryClick()
    {
        if(currentState == BuildState.PreviewNewBuilding)
        {
            if (placementCtr.TryPlace())
                CancelCurrentBuild();

            return;
        }

        if(currentState == BuildState.PreviewModifyBuilding)
        {
            if (modifyCtr.TryApply())
                ChangedState(BuildState.BuildingSelected);

            return;
        }

        if(currentState == BuildState.PreviewCorridor)
        {
            if (corridorCtr.TryPlace())
                ChangedState(BuildState.BuildingSelected);

            return;
        }
    }

    /// <summary>
    /// 통로 Preview 상태에서 현재 경로에 새로운 분기점을 추가하도록 요청
    /// </summary>
    /// <returns>분기점 추가 성공 여부</returns>
    public bool TrySetRouteTurn()
    {
        if(currentState != BuildState.PreviewCorridor)
            return false;

        return corridorCtr.TryCreateJunction();
    }

    /// <summary>
    /// 현재 통로 경로를 계산하기 위한 월드 포인터 위치를 전달
    /// </summary>
    /// <param name="worldPointer">포인터가 가리키는 월드 위치</param>
    public void SetRoutePointer(Vector3 worldPointer)
    {
        if (currentState != BuildState.PreviewCorridor)
            return;

        corridorCtr.SetGroundPointer(worldPointer);
    }

    /// <summary>
    /// 통로 건설 중 현재 경로 끝점과 유효하지 않은 Preview를 초기화
    /// </summary>
    public void ClearRoutePointer()
    {
        if (currentState != BuildState.PreviewCorridor)
            return;

        corridorCtr.ClearPointer();
    }

    /// <summary>
    /// 진행중인 건설 행동과 선택 상태를 정리, 건설 모드를 종료
    /// </summary>
    public void ExitBuildMode()
    {
        if (currentState == BuildState.None)
            return;

        ChangedState(BuildState.None);
    }

    /// <summary>
    /// 현재 선택 또는 건설 행동을 취소하고 건설 대기 상태로 전환
    /// 건설 모드 자체는 종료하지 않음
    /// </summary>
    public void CancelCurrentAction()
    {
        if (currentState == BuildState.None || currentState == BuildState.Idle)
            return;

        ChangedState(BuildState.Idle);
    }

    /// <summary>
    /// 현재 진행중인 건설 preview를 취소하고 건설 대기 상태로 전환
    /// 건설 모드 자체를 종료하지는 않음
    /// </summary>
    public void CancelCurrentBuild()
    {
        if (currentState == BuildState.None || currentState == BuildState.Idle)
            return;

        ChangedState(BuildState.Idle);
    }
}
