using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Corridor Module 하나의 정적 정의, 월드 Transform, 진입 및 출구 방향, 점유 셀과 경로 Anchor를 묶어 전달하는 계획 데이터
/// </summary>
public class CorridorModulePlan
{
    public BuildingDefine Define { get; }
    public Vector3 Pos { get; }
    public Quaternion Rotation { get; }

    public ConnectorDirection EnterDirection { get; }
    public ConnectorDirection ExitDirection { get; }

    public IReadOnlyList<Vector2Int> OccupiedCells { get; }

    public CorridorRouteAnchor StartAnchor { get; }
    public CorridorRouteAnchor EndAnchor { get; }

    public CorridorModulePlan(BuildingDefine define, Vector3 pos, Quaternion rotation,
        ConnectorDirection enterDirection, ConnectorDirection exitDirection, IReadOnlyList<Vector2Int> occupiedCells,
        CorridorRouteAnchor startAnchor, CorridorRouteAnchor endAnchor)
    {
        Define = define;
        Pos = pos;
        Rotation = rotation;
        EnterDirection = enterDirection;
        ExitDirection = exitDirection;
        OccupiedCells = occupiedCells;
        StartAnchor = startAnchor;
        EndAnchor = endAnchor;
    }
}

/// <summary>
/// 연속된 Corridor Module 목록과 전체 점유 셀, 다음 경로 계산에 사용할 끝 Anchor를 관리하는 구간 계획
/// </summary>
public sealed class CorridorRouteSegmentPlan
{
    public IReadOnlyList<CorridorModulePlan> Modules { get; }
    public IReadOnlyList<Vector2Int> OccupiedCells { get; }
    public CorridorRouteAnchor EndAnchor { get; }

    public CorridorRouteSegmentPlan(IReadOnlyList<CorridorModulePlan> modules, IReadOnlyList<Vector2Int> occupiedCells, CorridorRouteAnchor endAnchor)
    {
        Modules = modules;
        OccupiedCells = occupiedCells;
        EndAnchor = endAnchor;
    }
}

/// <summary>
/// Corridor 건설 요청에 필요한 전체 Module과 점유 셀, 시작 Connector를 관리하는 경로 계획
/// </summary>
public class CorridorRoutePlan
{
    public IReadOnlyList<CorridorModulePlan> Modules { get; }
    public IReadOnlyList<Vector2Int> OccupiedCells { get; }

    public BuildingConnector StartConnector { get; }

    public CorridorRoutePlan(BuildingConnector startConnector, IReadOnlyList<CorridorModulePlan> modules, IReadOnlyList<Vector2Int> occupiedCells)
    {
        StartConnector = startConnector;
        Modules = modules;
        OccupiedCells = occupiedCells;
    }
}

/// <summary>
/// Corridor 경로 계산의 기준이 되는 월드 위치와 회전을 보관, Connector로 생성된 경우 생성 지점의 Transform을 저장
/// </summary>
public class CorridorRouteAnchor
{
    public Vector3 Position { get; }
    public Quaternion Rotation { get; }

    public CorridorRouteAnchor(Vector3 position, Quaternion rotation)
    {
        Position = position;
        Rotation = rotation;
    }

    /// <summary>
    /// Connector의 현재 월드 위치와 회전을 복사하여 새로운 경로 Anchor를 생성
    /// </summary>
    /// <param name="connector">Anchor 생성 기준으로 사용할 Connector</param>
    /// <returns>생성된 Anchor, Connector가 null이라면 null</returns>
    public static CorridorRouteAnchor FromConnector(BuildingConnector connector)
    {
        if (connector == null)
            return null;

        return new CorridorRouteAnchor(connector.transform.position, connector.transform.rotation);
    }
}
