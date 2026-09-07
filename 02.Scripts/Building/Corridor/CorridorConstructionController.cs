using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 선택한 건물에서 시작하는 Corridor 건설 입력 상태를 관리하고 경로 계획, Preview 표시와 실제 배치 요청을 조율
/// 경로 형상을 직접 계산하거나 Runtime 건물을 직접 생성하지 않음
/// </summary>
public class CorridorConstructionController : MonoBehaviour
{
    [SerializeField]
    private CorridorRoutePreviewController corriderPreview;

    [Header("Components")]
    [SerializeField]
    private BuildingService buildingService;
    [SerializeField]
    private BuildingGrid grid;

    [Header("Defines")]
    [SerializeField]
    private BuildingDefine straightCorridorDefine;
    [SerializeField]
    private BuildingDefine junctionCorridorDefine;

    private CorridorRoutePlan currentRoutePlan;
    private CorridorRouteSegmentPlan currentPreviewSegment;
    private BuildingConnector currentStartCandidate;
    private CorridorModulePlan currentResolvedJunction;

    private readonly CorridorRouteDraft routeDraft = new CorridorRouteDraft();

    private BuildingRuntime selectedRuntimeBuilding;
    private bool currentCanPlace;

    public bool IsActive => selectedRuntimeBuilding != null;
    public bool CanPlace => currentCanPlace;

    /// <summary>
    /// Junction이 확정되기 전 선택 건물의 시작 Connector를 결정
    /// 포인터 위치까지의 초기 경로를 계획하여 Preview로 표시
    /// </summary>
    /// <param name="pointerPos"></param>
    private void UpdateInitialRoutePreview(Vector3 pointerPos)
    {
        InitCurrentValues();

        if (selectedRuntimeBuilding == null)
        {
            ClearRoutePreview();
            return;
        }

        if (!CorridorRoutePlanner.TrySelectRuntimeConnector(selectedRuntimeBuilding, pointerPos, grid, out BuildingConnector candidate))
        {
            SetStartCandidate(null);
            ClearRoutePreview();
            return;
        }

        SetStartCandidate(candidate);

        CorridorRouteAnchor startAnchor = CorridorRouteAnchor.FromConnector(candidate);

        if (startAnchor == null)
        {
            ClearRoutePreview();
            return;
        }

        if (!CorridorRoutePlanner.TryCreateSegment(startAnchor, pointerPos, straightCorridorDefine, grid, out CorridorRouteSegmentPlan previewSegment))
        {
            ClearRoutePreview();
            return;
        }

        currentPreviewSegment = previewSegment;

        currentRoutePlan = new CorridorRoutePlan(candidate, previewSegment.Modules, previewSegment.OccupiedCells);

        currentCanPlace = buildingService.CanPlace(currentRoutePlan.OccupiedCells);

        corriderPreview.Show(currentRoutePlan, currentCanPlace);
    }

    /// <summary>
    /// 현재 Junction의 출구를 결정하고 다음 경로 구간을 계획하여 기존 Draft와 결합된 전체 경로를 preview로 표시
    /// </summary>
    /// <param name="pointerPos">다음 경로 구간을 계산할 포인터의 월드 위치</param>
    /// <param name="targetConnector">경로를 연결할 대상 Connector, 지면은 null</param>
    private void UpdateJunctionRoutePreview(Vector3 pointerPos)
    {
        InitCurrentValues();

        if (!routeDraft.HasCurrentJunction)
        {
            ClearRoutePreview();
            return;
        }

        if (!CorridorRoutePlanner.TryResolveJunctionExit(routeDraft.CurrentJunction, pointerPos, grid, out CorridorModulePlan resolvedJunction, out CorridorRouteAnchor exitAnchor))
        {
            ShowCommittedRouteWithJunction();
            return;
        }

        if (!CorridorRoutePlanner.TryCreateSegment(exitAnchor, pointerPos, straightCorridorDefine, grid, out CorridorRouteSegmentPlan previewSegment))
        {
            ShowCommittedRouteWithJunction();
            return;
        }

        if (!routeDraft.TryCreateCombinedPlan(resolvedJunction, previewSegment, out CorridorRoutePlan combinedPlan))
        {
            ShowCommittedRouteWithJunction();
            return;
        }

        currentResolvedJunction = resolvedJunction;
        currentPreviewSegment = previewSegment;
        currentRoutePlan = combinedPlan;

        currentCanPlace = buildingService.CanPlace(currentRoutePlan.OccupiedCells);

        corriderPreview.Show(currentRoutePlan, currentCanPlace);
    }

    /// <summary>
    /// 현재 Draft와 확정 대기 중인 Junction을 결합하여 Preview로 표시
    /// 추가 경로 구간은 포함하지 않음
    /// </summary>
    private void ShowCommittedRouteWithJunction()
    {
        InitCurrentValues();

        if (!routeDraft.HasCurrentJunction)
        {
            ClearRoutePreview();
            return;
        }

        if (!routeDraft.TryCreateCombinedPlan(routeDraft.CurrentJunction, null, out CorridorRoutePlan displayPlan))
        {
            ClearRoutePreview();
            return;
        }

        currentRoutePlan = displayPlan;
        currentCanPlace = buildingService.CanPlace(currentRoutePlan.OccupiedCells);

        corriderPreview.Show(displayPlan, currentCanPlace);
    }

    /// <summary>
    /// 경로 시작 후보 Connector를 변경, 이전 후보의 표시를 해제
    /// </summary>
    /// <param name="connector">새로운 시작 Connector, 해제시 null받음</param>
    private void SetStartCandidate(BuildingConnector connector)
    {
        if (currentStartCandidate == connector)
            return;

        if (currentStartCandidate != null)
            currentStartCandidate.SetDisplay(false);

        currentStartCandidate = connector;

        if (currentStartCandidate != null)
            currentStartCandidate.SetDisplay(true);
    }

    /// <summary>
    /// 현재 Junction 존재 여부에 따라 초기 경로 또는 Junction 이후 경로 Preview 갱신
    /// </summary>
    /// <param name="pointerPos"></param>
    private void UpdateRoutePreview(Vector3 pointerPos)
    {
        if (routeDraft.HasCurrentJunction)
        {
            UpdateJunctionRoutePreview(pointerPos);
            return;
        }

        UpdateInitialRoutePreview(pointerPos);
    }

    /// <summary>
    /// 현재 경로의 마지막 Straight Module을 지정한 Junction Module로 교체한 후, 경로 계획을 생성
    /// </summary>
    /// <param name="junctionModule">마지막 Straight Module을 대신할 Junction Module</param>
    /// <param name="replacePlan">교체된 module 목록과 점유 셀로 생성된 경로 계획</param>
    /// <returns>교체 경로 계획을 정상적으로 생성하면 true</returns>
    private bool TryCreateReplacementRoutePlan(CorridorModulePlan junctionModule, out CorridorRoutePlan replacePlan)
    {
        replacePlan = null;

        if (junctionModule == null)
            return false;

        if (currentRoutePlan == null || currentRoutePlan.Modules == null || currentRoutePlan.Modules.Count == 0)
            return false;

        int lastIndex = currentRoutePlan.Modules.Count - 1;

        List<CorridorModulePlan> modules = new List<CorridorModulePlan>();
        List<Vector2Int> occupiedCells = new List<Vector2Int>();

        // 현재 경로 마지막 Straight Module을 제외한 앞부분을 유지
        for (int i = 0; i < lastIndex; i++)
        {
            CorridorModulePlan module = currentRoutePlan.Modules[i];

            if (module == null || module.OccupiedCells == null)
                return false;

            modules.Add(module);
            occupiedCells.AddRange(module.OccupiedCells);
        }

        // 마지막 Straight Module 대신 Junction Module을 추가
        modules.Add(junctionModule);
        occupiedCells.AddRange(junctionModule.OccupiedCells);

        replacePlan = new CorridorRoutePlan(currentRoutePlan.StartConnector, modules, occupiedCells);

        return true;
    }

    /// <summary>
    /// 초기 Preview 경로의 마지막 Straight Module을 첫 Junction으로 교체하고, 새로운 Corridor Draft를 시작
    /// Draft 구성에 실패하면 시작된 Draft를 초기화
    /// </summary>
    /// <returns>생성에 성공하면 true</returns>
    private bool TryCreateFirstJunction()
    {
        if (currentStartCandidate == null || currentStartCandidate.IsOccupied)
            return false;

        if (currentPreviewSegment == null || currentRoutePlan == null)
            return false;

        if (!CorridorRoutePlanner.TryReplaceLastStraightWithJunction(currentPreviewSegment, junctionCorridorDefine, grid, out CorridorRouteSegmentPlan retainedSegment, out CorridorModulePlan junctionModule))
            return false;

        if (!TryCreateReplacementRoutePlan(junctionModule, out CorridorRoutePlan replacePlan))
            return false;

        if (!buildingService.CanPlace(replacePlan.OccupiedCells))
            return false;

        if (!routeDraft.Begin(currentStartCandidate))
            return false;

        if (retainedSegment != null && !routeDraft.Append(retainedSegment))
        {
            routeDraft.Clear();
            return false;
        }

        if (!routeDraft.SetCurrentJunction(junctionModule))
        {
            routeDraft.Clear();
            return false;
        }

        ShowCommittedRouteWithJunction();

        return true;
    }

    /// <summary>
    /// 현재 Juncton의 출구와 다음 경로 구간을 확정하고, 마지막 Straight Module을 다음 Junction으로 교체
    /// </summary>
    /// <returns>다음 Junction까지 진행하는데 성공하면 true</returns>
    private bool TryAdvanceJunction()
    {
        if (!routeDraft.HasCurrentJunction)
            return false;

        if (currentResolvedJunction == null)
            return false;

        if (currentPreviewSegment == null || currentRoutePlan == null)
            return false;

        if (!CorridorRoutePlanner.TryReplaceLastStraightWithJunction(currentPreviewSegment, junctionCorridorDefine, grid, out CorridorRouteSegmentPlan retainedSegment, out CorridorModulePlan nextJunction))
            return false;

        if (!TryCreateReplacementRoutePlan(nextJunction, out CorridorRoutePlan replacePlan))
            return false;

        if (!buildingService.CanPlace(replacePlan.OccupiedCells))
            return false;

        if (!routeDraft.CommitCurrentJunction(currentResolvedJunction, retainedSegment, nextJunction))
            return false;

        ShowCommittedRouteWithJunction();

        return true;
    }

    /// <summary>
    /// 현재 경로 계산 결과와 배치 가능 상태를 초기화
    /// Draft, Connector 선택과 표시중인 Preview는 변경하지 않음
    /// </summary>
    private void InitCurrentValues()
    {
        currentRoutePlan = null;
        currentPreviewSegment = null;
        currentResolvedJunction = null;
        currentCanPlace = false;
    }

    /// <summary>
    /// 현재 경로 계산 결과를 초기화 하고 Corridor Preview를 숨김
    /// </summary>
    private void ClearRoutePreview()
    {
        currentRoutePlan = null;
        currentPreviewSegment = null;
        currentResolvedJunction = null;
        currentCanPlace = false;

        corriderPreview.Hide();
    }

    /// <summary>
    /// 기존 Corridor 건설 상태를 취소하고 지정한 Runtime 건물에서 새로운 건설 시작
    /// </summary>
    /// <param name="runtime">경로 시작 건물로 사용할 Runtime 건물</param>
    /// <returns>건설 시작 상태를 정상적으로 구성하면 true</returns>
    public bool Begin(BuildingRuntime runtime)
    {
        if (runtime == null)
            return false;

        Cancel();

        selectedRuntimeBuilding = runtime;

        selectedRuntimeBuilding.SetConnectorsSelectable(false);

        return true;
    }

    /// <summary>
    /// Connector 끝점 선택을 해제하고 지정한 지면 위치까지 경로 preview를 갱신
    /// </summary>
    /// <param name="worldPoint"></param>
    public void SetGroundPointer(Vector3 worldPoint)
    {
        UpdateRoutePreview(worldPoint);
    }

    /// <summary>
    /// 현재 preview 경로의 마지막 부분을 Junction으로 확정
    /// Draft가 시작되지 않으면 첫 Junction을 생성하고, 이후에는 다른 junction으로 진행
    /// </summary>
    /// <returns>확정에 성공하면 true</returns>
    public bool TryCreateJunction()
    {
        if (currentPreviewSegment == null || currentRoutePlan == null)
            return false;

        if (!currentCanPlace)
            return false;

        if (!routeDraft.IsStarted)
            return TryCreateFirstJunction();

        return TryAdvanceJunction();
    }

    /// <summary>
    /// 현재 경로 계획을 BuildingService에 전달하여 전체 Corridor 배치를 시도
    /// 배치에 실패하면 현재 Preview를 배치 불가 상태로 표기
    /// </summary>
    /// <returns>전체 Corridor경로 배치에 성공하면 true</returns>
    public bool TryPlace()
    {
        if (!IsActive)
            return false;

        if (!currentCanPlace || currentRoutePlan == null)
            return false;

        bool beginForfinalPlacement = false;

        if (!routeDraft.IsStarted)
        {
            if (currentStartCandidate == null)
                return false;

            // Junction 없이 바로 배치하는 경로도 최종 배치에 필요한 Draft 시작점을 구성
            if (!routeDraft.Begin(currentStartCandidate))
                return false;

            beginForfinalPlacement = true;
        }

        bool placed = buildingService.TryPlaceRoute(currentRoutePlan, out List<BuildingRuntime> placedBuildings);

        if (!placed)
        {
            currentCanPlace = false;

            // 이번 배치 시도에서 새로 시작한 Draft만 되돌림
            if (beginForfinalPlacement)
                routeDraft.Clear();

            corriderPreview.Show(currentRoutePlan, false);

            return false;
        }

        return true;
    }

    /// <summary>
    /// 현재 포인터로 선택한 경로 끝점과 계산된 Preview를 초기화
    /// 선택한 시작 건물과 확정된 Route Draft는 유지
    /// </summary>
    public void ClearPointer()
    {
        SetStartCandidate(null);
        ClearRoutePreview();
    }

    /// <summary>
    /// 선택 건물, Connector표시, 경로 계획, Draft와  preview 모두 초기화하여 현재 Corridor 건설 작업을 취소
    /// </summary>
    public void Cancel()
    {
        if (selectedRuntimeBuilding != null)
            selectedRuntimeBuilding.SetConnectorsSelectable(false);

        selectedRuntimeBuilding = null;

        currentRoutePlan = null;
        currentPreviewSegment = null;
        currentResolvedJunction = null;
        currentCanPlace = false;

        SetStartCandidate(null);

        routeDraft.Clear();

        corriderPreview.Hide();
    }
}
