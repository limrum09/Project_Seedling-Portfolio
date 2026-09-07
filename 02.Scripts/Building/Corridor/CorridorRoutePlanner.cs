using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Corridor 시작점과 포인터 위치를 이용해 Module 배치, Junction과 경로 구간을 계산을 하지만,
/// Grid 점유 가능 여부를 판단하거나 Runtime 건물을 생성하지는 않음
/// </summary>
public static class CorridorRoutePlanner
{
    private static int Dot(Vector2Int a,  Vector2Int b)
    {
        return a.x * b.x + a.y * b.y;
    }

    /// <summary>
    /// 시작 Anchor에서 목표 위치까지의 셀 변위를 시작 방향으로 투영하여 전방 거리를 계산
    /// 목표가 시작 방향의 앞쪽에 있지 않으면 실패
    /// </summary>
    /// <param name="startAnchor">경로 계산을 시작할 Anchor</param>
    /// <param name="targetPos">경로가 향할 목표</param>
    /// <param name="grid">월드 위치를 셀로 변경할 Grid</param>
    /// <param name="forwardCells">시작 방향을 기준으로 계산된 전방 셀 거리</param>
    /// <returns>목표가 시작점 전방에 있고 거리를 계산하면 true</returns>
    private static bool TryGetRouteDistances(CorridorRouteAnchor startAnchor, Vector3 targetPos, BuildingGrid grid, out int forwardCells)
    {
        forwardCells = 0;

        Vector3 flatForward = Vector3.ProjectOnPlane(startAnchor.Rotation * Vector3.forward, Vector3.up).normalized;

        if (flatForward.sqrMagnitude <= 0.001f)
            return false;

        float epsilon = grid.CellSize * 0.1f;

        // 시작 건물의 셀이 아닌, Connector 바깥의 첫 번째 셀 부터 거리를 계산
        Vector3 outSideStartPosition = startAnchor.Position + flatForward * epsilon;

        Vector2Int startCell = grid.WorldToCell(outSideStartPosition);
        Vector2Int targetCell = grid.WorldToCell(targetPos);

        ConnectorDirection worldDir = CorridorGeometry.GetGridDir(flatForward);

        Vector2Int gridForward = CorridorGeometry.GetGridVector(worldDir);

        if (gridForward == Vector2Int.zero)
            return false;

        Vector2Int deltaCell = targetCell - startCell;

        forwardCells = Dot(deltaCell, gridForward);

        return forwardCells > 0;
    }

    /// <summary>
    /// 중심과 회전을 기준으로 포인터의 Local 방향을 계산하여 가장 가까운 Connector 방향을 결정
    /// 포인터가 크기 범위 안에 있거나 두 축의 거리가 같으면 방향을 결정하지 않음
    /// </summary>
    /// <param name="center">방향 계산 기준의 월드 중심</param>
    /// <param name="rotation">Local 방향 변환에 사용할 월드 회전</param>
    /// <param name="pointerPos">포인터의 월드 위치</param>
    /// <param name="localSize">기준 영역의 Local 가로와 세로 크기</param>
    /// <param name="dir">계산된 Connector 방향이며, 실패하면 None</param>
    /// <returns>하나의 Connector 방향을 결정하면 true</returns>
    public static bool TryResolveConnectorDirection(Vector3 center, Quaternion rotation, Vector3 pointerPos, Vector2 localSize, out ConnectorDirection dir)
    {
        dir = ConnectorDirection.None;

        Vector3 toPointer = Vector3.ProjectOnPlane(pointerPos - center, Vector3.up);
        Vector3 localPointer = Quaternion.Inverse(rotation) * toPointer;

        float absX = Mathf.Abs(localPointer.x);
        float absZ = Mathf.Abs(localPointer.z);

        float halfWidth = Mathf.Max(0.01f, localSize.x * 0.5f);
        float halfDepth = Mathf.Max(0.01f, localSize.y * 0.5f);

        if (absX < halfWidth && absZ < halfDepth)
            return false;

        // Local X축과 Z축 중 중심에서 더 멀리 벗어난 축을 Connector 방향으로 선택
        if (absZ > absX)
        {
            dir = localPointer.z >= 0f ? ConnectorDirection.Forward : ConnectorDirection.Back;
            return true;
        }
        else if (absX > absZ)
        {
            dir = localPointer.x >= 0f ? ConnectorDirection.Right : ConnectorDirection.Left;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 전방 셀 거리와 Module 하나의 진행 셀 수를 이용해 필요한 Module 개수를 계산
    /// </summary>
    /// <param name="distanceCells">경로가 진행해야 하는 전체 셀 거리</param>
    /// <param name="advanceCells">Module 하나가 진행하는 셀 수</param>
    /// <param name="exactEndTarget">정확히 연결해야 하는 끝점이 있으면 true</param>
    /// <returns>배치할 Module 개수이며, 입력이 유효하지 않으면 0반환</returns>
    private static int CalculateModuleCount(int distanceCells, int advanceCells)
    {
        if (distanceCells <= 0 || advanceCells <= 0)
            return 0;

        return distanceCells / advanceCells;
    }

    /// <summary>
    /// 열린 경로 Anchor에 Module 하나를 맞춰 배치하고, Module 계획과 점유 셀을 목록에 추가
    /// </summary>
    /// <param name="define">추가할 Corridor Module의 정적 데이터</param>
    /// <param name="entryDir">경로가 Module에 진입할 방향</param>
    /// <param name="exitDir">경로가 Module에서 나갈 방향</param>
    /// <param name="openPos">현재 Anchor의 월드 위치</param>
    /// <param name="openRotation">현재 Anchor의 월드 회전</param>
    /// <param name="grid">Module의 점유 셀을 계산할 Grid</param>
    /// <param name="modules">생성된 Module계획을 추가할 목록</param>
    /// <param name="allCells">Module의 점유 셀을 추가할 전체 셀 목록</param>
    /// <param name="nextOpenPos">Module 출구에 생성된 다음 Anchor 위치</param>
    /// <param name="nextOpenRotation">Module 출구에 생성된 다음 Anchor 회전</param>
    /// <returns>Module 계획과 다음 Anchor를 생성하면 true</returns>
    private static bool TryAddModule(BuildingDefine define, ConnectorDirection entryDir, ConnectorDirection exitDir, 
        Vector3 openPos, Quaternion openRotation, BuildingGrid grid, List<CorridorModulePlan> modules, 
        List<Vector2Int> allCells, out Vector3 nextOpenPos, out Quaternion nextOpenRotation)
    {
        nextOpenPos = Vector3.zero;
        nextOpenRotation = Quaternion.identity;

        if (!CorridorGeometry.TryGetModulePos(define, entryDir, exitDir, openPos, openRotation, out Vector3 modulePos, out Quaternion moduleRotation, out nextOpenPos, out nextOpenRotation))
            return false;

        List<Vector2Int> cells = CorridorGeometry.GetOccupiedCells(define, modulePos, moduleRotation, grid);

        if (cells == null || cells.Count == 0)
            return false;

        CorridorRouteAnchor startAnchor = new CorridorRouteAnchor(openPos, openRotation);
        CorridorRouteAnchor endAnchor = new CorridorRouteAnchor(nextOpenPos, nextOpenRotation);

        modules.Add(new CorridorModulePlan(define, modulePos, moduleRotation, entryDir, exitDir, cells, startAnchor, endAnchor));

        allCells.AddRange(cells);

        return true;
    }

    /// <summary>
    /// 지정한 개수만큼 Straight Module을 연속해서 추가하고 열린 Anchor를 갱신
    /// 도중에 실패해도 이미 목록에 추가된 Module은 이 매서드에서 제거하지 않음
    /// </summary>
    /// <param name="count">추가할 Module 개수</param>
    /// <param name="straightDefine">Module의 정적 데이터</param>
    /// <param name="grid">계산할 grid</param>
    /// <param name="modules">계획을 누적할 목록</param>
    /// <param name="allCells">점유 셀을 누적할 목록</param>
    /// <param name="openPos">현재 열린 Anchor의 위치, 성공하면 마지막 출구 위치로 갱신</param>
    /// <param name="openRotation">현재 열린 Anchor의 회전, 성공하면 마지막 출구 회전으로 갱신</param>
    /// <returns>요청한 모든 Straight Module을 추가하면 true</returns>
    private static bool TryAddStraightModules(int count, BuildingDefine straightDefine, BuildingGrid grid, List<CorridorModulePlan> modules, List<Vector2Int> allCells, ref Vector3 openPos, ref Quaternion openRotation)
    {
        for(int i = 0; i < count; i++)
        {
            bool added = TryAddModule(straightDefine, ConnectorDirection.Back, ConnectorDirection.Forward, openPos, openRotation, grid, modules, allCells, out Vector3 nextPos, out Quaternion nextRotation);

            if (!added)
                return false;

            openPos = nextPos;
            openRotation = nextRotation;
        }

        return true;
    }


    /// <summary>
    /// 시작 Anchor에서 포인터까지 이어지는 직선 Corridor구간을 게획
    /// </summary>
    /// <param name="startAnchor">경로 구간의 시작 Anchor</param>
    /// <param name="pointerPos">경로가 향할 포인터의 월드 위치</param>
    /// <param name="straightDefine">배치할 Corridor의 정적 데이터</param>
    /// <param name="grid">grid</param>
    /// <param name="plan">생성된 Corridor 경로 구간 계획, 실패하면 null</param>
    /// <returns>하나 이상의 Straight Module을 포함한 경로 구간을 생성하면 true</returns>
    public static bool TryCreateSegment(CorridorRouteAnchor startAnchor, Vector3 pointerPos, BuildingDefine straightDefine, BuildingGrid grid, out CorridorRouteSegmentPlan plan)
    {
        plan = null;

        if (startAnchor == null)
            return false;

        if(straightDefine == null) 
            return false;

        if (grid == null)
            return false;

        if (!TryGetRouteDistances(startAnchor, pointerPos, grid, out int forwardCells))
            return false;

        List<CorridorModulePlan> modules = new List<CorridorModulePlan>();
        List<Vector2Int> allCells = new List<Vector2Int>();

        Vector3 openPos = startAnchor.Position;
        Quaternion openRotation = startAnchor.Rotation;

        int straightAdvanceCells = Mathf.Max(1, straightDefine.GridFootPrint.y);
        int straightCount = CalculateModuleCount(forwardCells, straightAdvanceCells);

        if (straightCount <= 0)
            return false;

        if (!TryAddStraightModules(straightCount, straightDefine, grid, modules, allCells, ref openPos, ref openRotation))
            return false;

        if (modules.Count == 0)
            return false;

        CorridorRouteAnchor endAnchor = new CorridorRouteAnchor(openPos, openRotation);

        plan = new CorridorRouteSegmentPlan(modules,allCells, endAnchor);

        return true;
    }

    /// <summary>
    /// Straight 경로 구간의 마지막 Module을 출구가 확정되지 않은 Junction Module로 교체
    /// 마지막 Module 앞의 경로가 있으면 별도의 유지 구간으로 반환
    /// </summary>
    /// <param name="straightSegment">마지막 Module을 교체할 Straight 경로 구간</param>
    /// <param name="junctionDefine">대신 배치할 Junction의 정적 데이터</param>
    /// <param name="grid">점유 셀 계산에 사용할 Grid</param>
    /// <param name="retainedSegment">마지막 Module을 제외하고 유지된 구간, 남은 Module이 없으면 null</param>
    /// <param name="junctionModule">출구 방향과 끝 Anchor가 아직 확정하지 않은 Junction</param>
    /// <returns>Straight Module을 Junction으로 교체하면 true</returns>
    public static bool TryReplaceLastStraightWithJunction(CorridorRouteSegmentPlan straightSegment, BuildingDefine junctionDefine, BuildingGrid grid, out CorridorRouteSegmentPlan retainedSegment, out CorridorModulePlan junctionModule)
    {
        retainedSegment = null;
        junctionModule = null;

        if (straightSegment == null)
            return false;

        if (straightSegment.Modules == null || straightSegment.Modules.Count == 0)
            return false;

        if (junctionDefine == null || junctionDefine.BuildingPrefab == null)
            return false;

        if (grid == null)
            return false;

        int lastIndex = straightSegment.Modules.Count - 1;

        CorridorModulePlan lastStraight = straightSegment.Modules[lastIndex];

        if (lastStraight == null || lastStraight.StartAnchor == null)
            return false;

        List<CorridorModulePlan> retainedModules = new List<CorridorModulePlan>();
        List<Vector2Int> retainedCells = new List<Vector2Int>();

        for(int i = 0; i < lastIndex; i++)
        {
            CorridorModulePlan module = straightSegment.Modules[i];

            if (module == null || module.OccupiedCells == null)
                return false;

            retainedModules.Add(module);
            retainedCells.AddRange(module.OccupiedCells);
        }

        BuildingRuntime junctionPrefab = junctionDefine.BuildingPrefab;
        CorridorRouteAnchor startAnchor = lastStraight.StartAnchor;

        if (!CorridorGeometry.TryCalculateSnapPos(junctionPrefab, ConnectorDirection.Back, startAnchor.Position, startAnchor.Rotation, out Vector3 modulePos, out Quaternion moduleRotation))
            return false;

        List<Vector2Int> occupiedCells = CorridorGeometry.GetOccupiedCells(junctionDefine, modulePos, moduleRotation, grid);

        if (occupiedCells == null || occupiedCells.Count == 0)
            return false;

        junctionModule = new CorridorModulePlan(junctionDefine, modulePos, moduleRotation, ConnectorDirection.Back, ConnectorDirection.None, occupiedCells, startAnchor, null);

        if(retainedModules.Count > 0)
            retainedSegment = new CorridorRouteSegmentPlan(retainedModules, retainedCells, startAnchor);

        return true;
    }

    /// <summary>
    /// Runtime 건물의 크기와 포인터 위치를 이용해 해당 방향의 비점유 Connector를 선택
    /// </summary>
    /// <param name="runtime">Connector를선택할 Runtime 건물</param>
    /// <param name="pointerPos">선택 방향을 결정할 포인터의 월드 위치</param>
    /// <param name="grid">건물의 월드 크기를 계산할 Grid</param>
    /// <param name="connector">선택된 비점유 Connector</param>
    /// <returns>포인터 방향에서 선택 가능한 Connector를 찾으면 true</returns>
    public static bool TrySelectRuntimeConnector(BuildingRuntime runtime, Vector3 pointerPos, BuildingGrid grid, out BuildingConnector connector)
    {
        connector = null;

        if (runtime == null)
            return false;

        if (grid == null)
            return false;

        Vector2 localSize = new Vector2(runtime.Define.GridFootPrint.x * grid.CellSize, runtime.Define.GridFootPrint.y * grid.CellSize);

        if (!TryResolveConnectorDirection(runtime.transform.position, runtime.transform.rotation, pointerPos, localSize, out ConnectorDirection dir))
            return false;

        BuildingConnector candidate = runtime.GetConnector(dir);

        if (candidate == null)
            return false;

        if (candidate.IsOccupied)
            return false;

        connector = candidate;

        return true;
    }

    /// <summary>
    /// 포인터 방향을 이용해 Junction의 출구를 결정하고 끝 Anchor가 포함된 새 Module 계획을 생성
    /// 진입 방향인 Back과 방향을 결정할 수 없는 None은 출구로 허용하지 않음
    /// </summary>
    /// <param name="junctionModule">출구가 아직 확정 안된 Module</param>
    /// <param name="pointerPos">출구 방향을 결정할 포인터 위치</param>
    /// <param name="grid">Junction의 월드 크기 계산에 사용할 Grid</param>
    /// <param name="resolvedJunction">출구 방향과 끝 Anchor가 설정된 Junction Module</param>
    /// <param name="exitAnchor">선택된 출구 Connector의 Anchor</param>
    /// <returns>유효한 Junction 출구를 결정하면 true</returns>
    public static bool TryResolveJunctionExit(CorridorModulePlan junctionModule, Vector3 pointerPos, BuildingGrid grid, out CorridorModulePlan resolvedJunction, out CorridorRouteAnchor exitAnchor)
    {
        resolvedJunction = null;
        exitAnchor = null;

        if (junctionModule == null)
            return false;

        if (junctionModule.Define == null || junctionModule.Define.BuildingPrefab == null)
            return false;

        if (grid == null)
            return false;

        Vector2 localSize = new Vector2(junctionModule.Define.GridFootPrint.x * grid.CellSize, junctionModule.Define.GridFootPrint.y * grid.CellSize);

        if (!TryResolveConnectorDirection(junctionModule.Pos, junctionModule.Rotation, pointerPos, localSize, out ConnectorDirection exitDir))
            return false;

        if (exitDir == ConnectorDirection.Back || exitDir == ConnectorDirection.None)
            return false;

        if (!CorridorGeometry.TryGetConnectorAnchor(junctionModule.Define, junctionModule.Pos, junctionModule.Rotation, exitDir, out exitAnchor))
            return false;

        resolvedJunction = new CorridorModulePlan(junctionModule.Define, junctionModule.Pos, junctionModule.Rotation, junctionModule.EnterDirection, exitDir, junctionModule.OccupiedCells, junctionModule.StartAnchor, exitAnchor);

        return true;
    }
}
