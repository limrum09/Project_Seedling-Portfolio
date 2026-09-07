using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Corridor 건설 중 확정된 경로 구간과 현재 Junction 상태를 누적 관리하고, Preview 구간을 포함한 전체 경로 게호기을 생성
/// 경로 형상을 계산하거나 건물을 배치하지 않음
/// </summary>
public sealed class CorridorRouteDraft
{
    private readonly List<CorridorRouteSegmentPlan> commitSegments = new List<CorridorRouteSegmentPlan>();

    public BuildingConnector OriginalStartConnector { get; private set; }
    public CorridorRouteAnchor CurrentAnchor { get; private set; }
    public CorridorModulePlan CurrentJunction { get; private set; }
    public bool IsStarted => OriginalStartConnector != null && CurrentAnchor != null;
    public bool HasCurrentJunction => CurrentJunction != null;

    /// <summary>
    /// 경로구간에 Module, 점유 셀과 끝 Anchor가 정상적으로 구성되어 있는지 확인
    /// </summary>
    /// <param name="segment">검증할 구간의 계획</param>
    /// <returns>Draft에 추가할 수 있는 경로 구간이면 true</returns>
    private static bool IsValidSegment(CorridorRouteSegmentPlan segment)
    {
        if (segment == null)
            return false;

        if (segment.Modules == null || segment.Modules.Count == 0)
            return false;

        if (segment.OccupiedCells == null)
            return false;

        for(int i = 0; i  < segment.Modules.Count; i++)
        {
            CorridorModulePlan module = segment.Modules[i];

            if (module == null || module.OccupiedCells == null)
                return false;
        }

        return segment.EndAnchor != null;
    }

    /// <summary>
    /// Mudile이 현재 Junction으로 사용하기 위한 기본 조건을 만족하는지 확인
    /// </summary>
    /// <param name="junction">검증할 Junction Module</param>
    /// <returns>정의, 시작 Anchor, 진입 방향과 점유 셀이 유효하면 true</returns>
    private static bool IsValidCurrentJunction(CorridorModulePlan junction)
    {
        if(junction == null) 
            return false;

        if (junction.Define == null)
            return false;

        if (junction.StartAnchor == null)
            return false;

        if (junction.EnterDirection != ConnectorDirection.Back)
            return false;

        if (junction.OccupiedCells == null || junction.OccupiedCells.Count == 0)
            return false;

        return true;
    }

    /// <summary>
    /// Junction의 출구 방향과 끝 Anchor가 확정되어 있는지 확인
    /// </summary>
    /// <param name="junction">출구가 확정된 Junction Module</param>
    /// <returns>Junction의 유효한 출구의 끝 Anchor가 설정되어 있으면 true</returns>
    private static bool IsValidResolvedJunction(CorridorModulePlan junction)
    {
        if (!IsValidCurrentJunction(junction))
            return false;

        if (junction.ExitDirection == ConnectorDirection.Back || junction.ExitDirection == ConnectorDirection.None)
            return false;

        return junction.EndAnchor != null;
    }

    /// <summary>
    /// 기존 Draft를 초기화 하고, 지정한 비점유 Connector에서 새로운 Draft를 시작
    /// 시작에 실패해도 기존 Draft는 복구하지 않음
    /// </summary>
    /// <param name="startConnector">경로 시작점으로 사용할 Connector</param>
    /// <returns>시작 Connector에서 Anchor를 생성하면 true</returns>
    public bool Begin(BuildingConnector startConnector)
    {
        // 새로 시작 시도는 성공 여부와 상관없이 이전 Draft를 제거
        Clear();

        if (startConnector == null || startConnector.IsOccupied)
            return false;

        CorridorRouteAnchor startAnchor = CorridorRouteAnchor.FromConnector(startConnector);

        if (startAnchor == null)
            return false;

        OriginalStartConnector = startConnector;
        CurrentAnchor = startAnchor;

        return true;
    }

    /// <summary>
    /// 현재 Junction이 없는 상태에서 확정된 경로 구간을 Draft에 추가하고, 현재 Anchor를 구간의 끝 Anchor로 이동
    /// </summary>
    /// <param name="segment">확정 구간으로 추가할 경로 계획</param>
    /// <returns>경로 구간을 정상적으로 추가하면 true</returns>
    public bool Append(CorridorRouteSegmentPlan segment)
    {
        if (!IsStarted)
            return false;

        if (HasCurrentJunction)
            return false;

        if (!IsValidSegment(segment))
            return false;

        commitSegments.Add(segment);
        CurrentAnchor = segment.EndAnchor;

        return true;
    }

    /// <summary>
    /// 현재 Draft에 확정 대기 중인 Junction을 설정하고 현재 Anchor를 Junction 시작점으로 이동
    /// </summary>
    /// <param name="junction">현재 Junction으로 설정할 Module</param>
    /// <returns>Junction을 정상적으로 설정하면 true</returns>
    public bool SetCurrentJunction(CorridorModulePlan junction)
    {
        if (!IsStarted)
            return false;

        if (HasCurrentJunction)
            return false;

        if (!IsValidCurrentJunction(junction))
            return false;

        CurrentJunction = junction;
        CurrentAnchor = junction.StartAnchor;

        return true;
    }

    /// <summary>
    /// 현재 Junction의 출구를 확정 구간으로 추가하고, 선택적인 후속 구간을 이어 붙인 뒤, 다음 Junction을 현재 Juncton으로 설정
    /// 모든 입력을 검증한 후에만 Draft 상태를 변경
    /// </summary>
    /// <param name="resolvedJunction">출구가 결정된 현재 Junction</param>
    /// <param name="followSegment">현재 Junction뒤에 이어질 경로 구간, 없으면 null</param>
    /// <param name="nextJunction">새롭게 확정 대기 상태가 될 다음 Junction</param>
    /// <returns>현재 Junction 확정과 다음 Juction설정에 성공하면 true</returns>
    public bool CommitCurrentJunction(CorridorModulePlan resolvedJunction, CorridorRouteSegmentPlan followSegment, CorridorModulePlan nextJunction)
    {
        if (!IsStarted)
            return false;

        if (!HasCurrentJunction)
            return false;

        if (!IsValidResolvedJunction(resolvedJunction))
            return false;

        if (followSegment != null && !IsValidSegment(followSegment))
            return false;

        if (!IsValidCurrentJunction(nextJunction))
            return false;

        List<CorridorModulePlan> junctionModules = new List<CorridorModulePlan> { resolvedJunction };

        List<Vector2Int> junctionCells = new List<Vector2Int>(resolvedJunction.OccupiedCells);

        CorridorRouteSegmentPlan junctionSegment = new CorridorRouteSegmentPlan(junctionModules, junctionCells, resolvedJunction.EndAnchor);

        commitSegments.Add(junctionSegment);

        if(followSegment != null)
            commitSegments.Add(followSegment);

        CurrentAnchor = nextJunction.StartAnchor;
        CurrentJunction = nextJunction;

        return true;
    }

    /// <summary>
    /// 확정된 경로 구간, 현재 Junction과 선택적인 preview 구간을 순서대로 결합하여, 실제 배치 또는 표시에 사용할 전체 경로 계획을 생성
    /// </summary>
    /// <param name="pendingModule">현재 Junction 상태를 반영할 Module</param>
    /// <param name="previewSegment">현재 Junction 뒤에 표시할 임의 경로 구간, 없으면 null</param>
    /// <param name="routePlan">생성된 전체 Corridor 경로 계획, 실패하면 null</param>
    /// <returns>하나 이상의 Module을 포함한 경로 계획을 생성하면 true</returns>
    public bool TryCreateCombinedPlan(CorridorModulePlan pendingModule, CorridorRouteSegmentPlan previewSegment, out CorridorRoutePlan routePlan)
    {
        routePlan = null;

        if (!IsStarted)
            return false;

        List<CorridorModulePlan> modules = new List<CorridorModulePlan>();

        List<Vector2Int> occupiedCells = new List<Vector2Int>();

        // 확정 구간, 현재 Junction, Preview 구간 순서로 전체 경로를 결합
        foreach(CorridorRouteSegmentPlan segment in commitSegments)
        {
            if (segment == null)
                return false;

            modules.AddRange(segment.Modules);
            occupiedCells.AddRange(segment.OccupiedCells);
        }

        if (HasCurrentJunction)
        {
            if (pendingModule == null)
                return false;

            modules.Add(pendingModule);
            occupiedCells.AddRange(pendingModule.OccupiedCells);
        }
        else if (pendingModule != null)
            return false;

        if(previewSegment != null)
        {
            modules.AddRange(previewSegment.Modules);
            occupiedCells.AddRange(previewSegment.OccupiedCells);
        }

        if (modules.Count == 0)
            return false;

        routePlan = new CorridorRoutePlan(OriginalStartConnector, modules, occupiedCells);

        return true;
    }

    /// <summary>
    /// 확정된 경로 구간, 시작 Connector, 현재 Anchor와 Junction을 모두 초기화
    /// </summary>
    public void Clear()
    {
        commitSegments.Clear();
        OriginalStartConnector = null;
        CurrentAnchor = null;
        CurrentJunction = null;
    }
}