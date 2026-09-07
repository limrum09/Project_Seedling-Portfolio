using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// 단일 건물과 Corridor 경로의 배치 요청을 받아 Runtime 생성과 배치 처리를 조율
/// 건물 초기화, Grid 등록과 Connector 연결을 순서대로 처리
/// 개별 배치 및 연결 규칙은 담당 객체에 위임
/// </summary>
public class BuildingService : MonoBehaviour
{
    /// <summary>
    /// 서로 연결할 두 BuildingConnector를 하나의 연결 쌍으로 관리
    /// </summary>
    private class ConnectorPair
    {
        public BuildingConnector First { get; }
        public BuildingConnector Second { get; }

        public ConnectorPair(BuildingConnector First, BuildingConnector Second)
        {
            this.First = First;
            this.Second = Second;
        }
    }

    [SerializeField]
    private BuildValidator validator;
    [SerializeField]
    private GridRegistry gridRegistry;
    [SerializeField]
    private BuildingConnectionService connectionService;

    /// <summary>
    /// 모든 연결 쌍을 검증한 후 순서대로 연결
    /// 도중에 실패하면 이번 요청에서 성공한 연결을 모두 해제
    /// </summary>
    /// <param name="pairs">연결할 Connector 쌍 목록</param>
    /// <returns>모든 Connector 쌍의 연결에 성공하면 true</returns>
    private bool TryConnectAll(IReadOnlyList<ConnectorPair> pairs)
    {
        if(!CanConnectAll(pairs)) 
            return false;

        List<ConnectorPair> connectedPairs = new List<ConnectorPair>();

        foreach (var pair in pairs)
        {
            if(!connectionService.TryConnect(pair.First, pair.Second))
            {
                // 일부 연결만 남지 않도록 이번 요청에서 성공한 연결을 역순으로 해제
                for(int i = connectedPairs.Count - 1; i >= 0; i--)
                {
                    connectionService.TryDisconnect(connectedPairs[i].First);
                }

                return false;
            }

            connectedPairs.Add(pair);
        }

        return true;
    }

    /// <summary>
    /// 모든 연결 쌍이 연결 시도에 필요한 기본 조건을 만족하는지 확인
    /// 실제 위치와 방향을 포함한 연결 규칙은 BuildingConnectionService에서 검증
    /// </summary>
    /// <param name="pairs">검증할 Connector 쌍 목록</param>
    /// <returns>모든 연결 쌍이 유효하고 점유되지 않았으면 true</returns>
    private bool CanConnectAll(IReadOnlyList<ConnectorPair> pairs)
    {
        if(pairs == null || pairs.Count == 0) 
            return false;

        foreach(ConnectorPair pair in pairs)
        {
            if (pair == null)
                return false;

            if(pair.First == null || pair.Second == null)
                return false;

            if (pair.First == pair.Second)
                return false;

            if (pair.First.IsOccupied)
                return false;

            if (pair.Second.IsOccupied)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 경로 계획과 생성된 건물 목록을 이용해 시작점과 Module 사이의 필수 Connector를 연결
    /// 마지막 출구에 연결 가능한 대상이 있으면 자동 연결하고, 대상이 없으면 열린 상태로 유지
    /// 연결 처리에 실패하면 호출부가 전체 배치를 Rollback할 수 있도록 false를 반환
    /// </summary>
    /// <param name="plan">시작 Connector와 Module 연결 정보가 포함된 Corridor 경로 계획</param>
    /// <param name="buildings">경로 계획의 Module 순서대로 생성된 Runtime 건물 목록</param>
    /// <returns>필수 연결과 선택적인 마지막 출구 연결 처리에 성공하면 true</returns>
    private bool TryConnectRoute(CorridorRoutePlan plan, IReadOnlyList<BuildingRuntime> buildings)
    {
        if (plan == null)
            return false;

        if (buildings == null)
            return false;

        if (buildings.Count == 0)
            return false;

        if (plan.Modules.Count != buildings.Count)
            return false;

        List<ConnectorPair> pairs = new List<ConnectorPair>();

        BuildingConnector firstEnter = buildings[0].GetConnector(plan.Modules[0].EnterDirection);

        pairs.Add(new ConnectorPair(plan.StartConnector, firstEnter));

        for(int i = 0; i < buildings.Count - 1; i++)
        {
            BuildingConnector currentExit = buildings[i].GetConnector(plan.Modules[i].ExitDirection);

            BuildingConnector nextEnter = buildings[i + 1].GetConnector(plan.Modules[i + 1].EnterDirection);

            pairs.Add(new ConnectorPair(currentExit, nextEnter));
        }

        if (!TryConnectAll(pairs))
            return false;

        int lastIndex = buildings.Count - 1;
        ConnectorDirection lastExitDirection = plan.Modules[lastIndex].ExitDirection;

        // 출구가 아직 정해지지 않은 Junction은 열린 상태로 유지한다.
        if (lastExitDirection == ConnectorDirection.None)
            return true;

        BuildingConnector lastExit = buildings[lastIndex].GetConnector(lastExitDirection);

        if (lastExit == null)
            return false;

        return connectionService.TryConnectIfMatchExists(lastExit, gridRegistry.RegisteredBuildings);
    }

    /// <summary>
    /// 이번 배치 요청에서 생성된 건물의 연결과 Grid 등록을 해제하고 건물을 제거
    /// 처리한 건물 목록을 비운 뒤, 실패 결과를 반환
    /// </summary>
    /// <param name="placedBuilding">되돌릴 Runtime 건물 목록</param>
    /// <returns>Rollback 호출부에서 바로 반환할 false</returns>
    private bool RollbackPlacedBuildings(List<BuildingRuntime> placedBuilding)
    {
        if (placedBuilding == null)
            return false;

        foreach(BuildingRuntime building in placedBuilding)
        {
            if (building == null)
                continue;

            connectionService.TryDisconnectAll(building);
            gridRegistry.UnRegister(building);
            Destroy(building.gameObject);
        }

        placedBuilding.Clear();

        return false;
    }

    /// <summary>
    /// 주어진 셀 목록에 건물을 배치할 수 있는지 BuildPlacementValidator를 통해 확인
    /// </summary>
    /// <param name="cells">배치 가능 여부를 확인할 셀 목록</param>
    /// <returns>건물 배치 가능 여부</returns>
    public bool CanPlace(IReadOnlyList<Vector2Int> cells)
    {
        return validator.CheckCanPlace(cells);
    }

    /// <summary>
    /// 건물의 점유 셀을 계산하고 배치를 검증한 뒤 Runtime 생성과 Grid 등록, Connector 연결을 처리
    /// Grid 등록이나 Connector 연결에 실패하면 생성한 건물을 제거
    /// 배치가 완료되면 건물을 초기화하고 생산 상태를 등록
    /// </summary>
    /// <param name="define">배치할 건물의 정적 데이터</param>
    /// <param name="startCell">건물이 점유할 영역의 시작 셀</param>
    /// <param name="dir">건물의 배치 방향</param>
    /// <param name="worldPos">생성할 건물의 월드 위치</param>
    /// <param name="worldRotation">생성할 건물의 월드 회전 값</param>
    /// <param name="placeBuilding">배치에 성공한 Runtime 건물</param>
    /// <returns>건물 배치에 성공하면 true</returns>
    public bool TryPlace(BuildingDefine define, Vector2Int startCell, ConnectorDirection dir, Vector3 worldPos, Quaternion worldRotation, out BuildingRuntime placeBuilding)
    {
        placeBuilding = null;

        if (define == null)
            return false;

        List<Vector2Int> occupiedCells = GridFootprintCalculator.GetOccupiedCells(startCell, define.GridFootPrint, dir);

        if (!validator.CheckCanPlace(occupiedCells))
            return false;

        BuildingRuntime newBuilding = Instantiate(define.BuildingPrefab, worldPos, worldRotation);

        if (newBuilding == null)
            return false;

        newBuilding.Init(define);

        bool registerd = gridRegistry.TryRegister(newBuilding, occupiedCells);

        if (!registerd)
        {
            Destroy(newBuilding.gameObject);
            return false;
        }

        if (!connectionService.TryConnectNewBuilding(newBuilding, gridRegistry.RegisteredBuildings))
        {
            gridRegistry.UnRegister(newBuilding);
            Destroy(newBuilding.gameObject);

            return false;
        }

        placeBuilding = newBuilding;

        return true;    
    }

    /// <summary>
    /// Corridor 경로의 모든 Module을 생성하고 Grid 등록과 Connector 연결을 처리
    /// 생성, Grid 등록이나 연결에 실패하면 이번 요청에서 배치한 모든 건물을 되돌림
    /// 전체 경로의 배치가 완료된 뒤 각 건물의 생산 상태를 등록
    /// </summary>
    /// <param name="routePlan">배치할 Module과 연결 정보가 포함된 경로 계획</param>
    /// <param name="placedBuilding">배치에 성공한 Runtime 건물 목록</param>
    /// <returns>전체 경로의 배치에 성공하면 true</returns>
    public bool TryPlaceRoute(CorridorRoutePlan routePlan, out List<BuildingRuntime> placedBuilding)
    {
        placedBuilding = new List<BuildingRuntime>();

        if (routePlan == null)
            return false;

        if (routePlan.Modules == null || routePlan.Modules.Count == 0)
            return false;

        if (!validator.CheckCanPlace(routePlan.OccupiedCells))
            return false;

        foreach(CorridorModulePlan module in routePlan.Modules)
        {
            if (module == null)
                return RollbackPlacedBuildings(placedBuilding);

            if (module.Define == null || module.Define.BuildingPrefab == null)
                return RollbackPlacedBuildings(placedBuilding);

            BuildingRuntime newBuilding = Instantiate(module.Define.BuildingPrefab, module.Pos, module.Rotation);

            if (newBuilding == null)
                return RollbackPlacedBuildings(placedBuilding);

            newBuilding.Init(module.Define);

            bool registered = gridRegistry.TryRegister(newBuilding, module.OccupiedCells);

            if (!registered)
            {
                Destroy(newBuilding.gameObject);
                return RollbackPlacedBuildings(placedBuilding);
            }

            placedBuilding.Add(newBuilding);
        }

        // 모든 Module의 생성과 Grid 등록이 끝난 뒤 경로 전체의 Connector를 연결
        if (!TryConnectRoute(routePlan, placedBuilding))
            return RollbackPlacedBuildings(placedBuilding);

        return true;
    }

    /// <summary>
    /// Scene에 배치된 건물을 가까운 Grid 위치로 맞추고 초기화 및 등록
    /// 건물 오브젝트는 새로 생성하지 않음
    /// </summary>
    /// <param name="runtime">등록할 Scene 건물</param>
    /// <param name="grid">배치 위치 계산에 사용할 Grid</param>
    public void RegisterExisting(BuildingRuntime runtime, BuildingGrid grid)
    {
        BuildingDefine define = runtime.Define;

        if (define == null)
            throw new InvalidOperationException($"{runtime.name}: BuildingDefine을 지정해야 함");

        int rotationStep = BuildingPlacementCalculator.GetRotationStep(runtime.transform.rotation);

        if (!BuildingPlacementCalculator.TryCreateCandidate(define, grid, runtime.transform.position, rotationStep, out BuildingPlacementCandidate candidate))
            throw new InvalidOperationException($"{runtime.name}: 시작 건물 배치 계산 실패");

        runtime.transform.SetPositionAndRotation(candidate.Pos, candidate.Rotation);

        runtime.Init(define);

        if (!gridRegistry.TryRegister(runtime, candidate.OccupiedCells))
            throw new InvalidOperationException($"{runtime.name}: 건물 중복 등록 또는 점유 셀 겹침");
    }

    /// <summary>
    /// 위치와 등록이 확정된 시작 건물 사이의 Connector를 연결
    /// </summary>
    /// <param name="buildings">등록을 완료한 시작 건물 목록</param>
    public void ConnectExisting(IReadOnlyList<BuildingRuntime> buildings)
    {
        foreach (BuildingRuntime building in buildings)
        {
            if (!connectionService.TryConnectNewBuilding(building, buildings))
                throw new InvalidOperationException($"{building.name}: 시작 건물 Connector 연결 실패");
        }
    }
}