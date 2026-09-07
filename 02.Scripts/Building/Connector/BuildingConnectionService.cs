using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;

/// <summary>
/// BuildingConnector 사이의 연결 가능 여부를 검중하고 양방향 연결 상태를 변경
/// 건물 배치나 Corridor 경로 계획은 담당하지 않음
/// </summary>
public class BuildingConnectionService : MonoBehaviour
{
    private float posTolerance = 0.1f;
    private float facingDotTolerance = 0.99f;

    /// <summary>
    /// Runtime 건물의 모든 Connector가 연결되지 않은 상태인지 확인
    /// </summary>
    /// <param name="runtime">연결 해제 상태를 확인할 Runtime 건물</param>
    /// <returns>유효한 모든 Connector가 열결되지 않으면 true 반환</returns>
    private bool ValidateDisconnected(BuildingRuntime runtime)
    {
        if (runtime == null || runtime.Connectors == null)
            return false;

        foreach(BuildingConnector connector in runtime.Connectors)
        {
            if(connector == null) 
                continue;

            if (connector.ConnectedConnector != null)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Runtime 건물에 존재하는 모든 연결이 정상적인 양방향 관계인지 검증
    /// 연결되지 않은 Connector는 정상 상태로 허용
    /// </summary>
    /// <param name="runtime">연결 상태를 검증할 Runtime 건물</param>
    /// <returns>존재하는 모든 연결이 서로 다른 건물 사이의 양방향 연결이면 true</returns>
    public bool ValidateConnections(BuildingRuntime runtime)
    {
        if (runtime == null || runtime.Connectors == null)
            return false;

        foreach(BuildingConnector connector in runtime.Connectors)
        {
            if(connector == null) 
                continue;

            BuildingConnector connected = connector.ConnectedConnector;

            if (connected == null)
                continue;

            if (connected.ConnectedConnector != connector)
                return false;

            if(connected == connector)
                return false;

            BuildingRuntime connectedOwner = connected.GetComponentInParent<BuildingRuntime>();

            if (connectedOwner == null || connectedOwner == runtime)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 두 Connecotr의 소유 건물, 점유 상태, 거리와 방향을 이용해 연결 가능 여부를 확인
    /// 이미 서로 연결된 Conenector는 점유 상태와 관계없이 다시 검증 가능함
    /// </summary>
    /// <param name="first">연결 가능 여부를 확인할 첫번째 Connector</param>
    /// <param name="second">두번 째 Connector</param>
    /// <returns>두 Connector가 모두 연결 가능하면 true 반환</returns>
    public bool CanConnect(BuildingConnector first, BuildingConnector second)
    {
        if (first == null || second == null)
            return false;

        if (first == second)
            return false;

        BuildingRuntime firstOwner = first.GetComponentInParent<BuildingRuntime>();
        BuildingRuntime secondOwner = second.GetComponentInParent<BuildingRuntime>();

        if (firstOwner == null || secondOwner == null)
            return false;

        if (firstOwner == secondOwner)
            return false;

        bool alreadyConnected = first.ConnectedConnector == second && second.ConnectedConnector == first;

        if (!alreadyConnected && (first.IsOccupied || second.IsOccupied))
            return false;

        float sqrTolerance = posTolerance * posTolerance;
        float sqrDis = (first.transform.position - second.transform.position).sqrMagnitude;

        if (sqrDis > sqrTolerance)
            return false;

        Vector3 firstForward = first.transform.forward.normalized;
        Vector3 secondForward = second.transform.forward.normalized;

        float facingDot = Vector3.Dot(firstForward, -secondForward);

        return facingDot >= facingDotTolerance;
    }

    /// <summary>
    /// 연결 조건을 만족하는 두 Connector를 서로 연결하고 양방향 상태를 확인
    /// 이미 서로 연결된 상태라면 성공으로 처리
    /// </summary>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns>두 Connector가 정상적인 양방향 연결 상태라면 true</returns>
    public bool TryConnect(BuildingConnector first, BuildingConnector second)
    {
        if(!CanConnect(first, second))
            return false;

        if(first.ConnectedConnector == second && second.ConnectedConnector == first)
            return true;

        first.SetConnector(second);
        second.SetConnector(first);

        return first.ConnectedConnector == second && second.ConnectedConnector == first;
    }

    /// <summary>
    /// 지정한 Connector와 연결할 수 있는 가장 가까운 Connector를 후보 건물에서 찾아 연결
    /// 연결 가능한 대상이 없으면 열린 Connector를 유지하고 성공으로 처리
    /// </summary>
    /// <param name="connector">선택적으로 자동 연결할 Connector</param>
    /// <param name="candidates">연결 대상을 검색할 Runtime 건물 목록</param>
    /// <returns>연결 대상이 없거나 연결에 성공하면 true</returns>
    public bool TryConnectIfMatchExists(BuildingConnector connector, IEnumerable<BuildingRuntime> candidates)
    {
        if (connector == null || candidates == null)
            return false;

        if (connector.IsOccupied)
            return false;

        BuildingConnector bestTarget = null;
        float minSqrDistance = float.MaxValue;

        foreach (BuildingRuntime candidate in candidates)
        {
            if (candidate == null || candidate.Connectors == null)
                continue;

            foreach (BuildingConnector target in candidate.Connectors)
            {
                if (!CanConnect(connector, target))
                    continue;

                float sqrDistance = (connector.transform.position - target.transform.position).sqrMagnitude;

                if (sqrDistance >= minSqrDistance)
                    continue;

                minSqrDistance = sqrDistance;
                bestTarget = target;
            }
        }

        // 연결 가능한 대상이 없는 것은 정상적인 열린 복도 상태다.
        if (bestTarget == null)
            return true;

        return TryConnect(connector, bestTarget);
    }

    /// <summary>
    /// 새 건물의 비어 있는 각 Connector에 대해 후보 건물에게 가장 가까운 연결 대상을 찾아 연결
    /// 연결 도중 실패하면 이번 요청에서 성공한 연결을 모두 해제
    /// 단, 연결 가능한 대상이 없는 것은 실패로 처리하지 않음
    /// </summary>
    /// <param name="newBuilding">새 Runtime 건물</param>
    /// <param name="candidates">연결 대상을 검색할 runtime 건물</param>
    /// <returns>발견된 모든 연결 대상과의 연결 처리에 성공하면 true</returns>
    public bool TryConnectNewBuilding(BuildingRuntime newBuilding, IEnumerable<BuildingRuntime> candidates)
    {
        if(newBuilding == null || newBuilding.Connectors == null) 
            return false;

        if (candidates == null)
            return false;

        List<BuildingConnector> connectors = new List<BuildingConnector>();

        foreach(BuildingConnector connector in newBuilding.Connectors)
        {
            if(connector == null || connector.IsOccupied)
                continue;

            // 연결에 실패하면, 일부만 연결이 남지 않도록 연결을 역순으로 해제
            if (!TryConnectIfMatchExists(connector, candidates))
            {
                for (int i = connectors.Count - 1; i >= 0; i--)
                    TryDisconnect(connectors[i]);

                return false;
            }

            if (connector.IsOccupied)
                connectors.Add(connector);
        }

        return true;
    }

    /// <summary>
    /// 지정한 Connector와 상대 Connector의 양방향 연결을 해제
    /// 이미 연결되지 않은 Connector는 상공으로 처리
    /// </summary>
    /// <param name="connector"></param>
    /// <returns>양쪽 모두 연결이 해제되었으면 true</returns>
    public bool TryDisconnect(BuildingConnector connector)
    {
        if (connector == null)
            return false;

        BuildingConnector connected = connector.ConnectedConnector;

        if (connected == null)
            return true;

        if (connected.ConnectedConnector != connector)
            return false;

        if (connected == connector)
            return false;

        connector.ClearConnector();
        connected.ClearConnector();

        return connector.ConnectedConnector == null && connected.ConnectedConnector == null;
    }

    /// <summary>
    /// Runtime 건물의 전체 연결 상태를 검증한 후, 모든 Connector 연결을 해제
    /// 잘못된 연결 상태가 발견되면 연결을 변경하지 않고 실패
    /// </summary>
    /// <param name="runtime">모든 연결을 해제할 Runtime 건물</param>
    /// <returns>모든 Connector가 연결 해제 상채면 true</returns>
    public bool TryDisconnectAll(BuildingRuntime runtime)
    {
        if (runtime == null)
            return false;

        if (!ValidateConnections(runtime))
            return false;

        foreach(BuildingConnector connector in runtime.Connectors)
        {
            if (connector == null)
                continue;

            if (!TryDisconnect(connector))
                return false;
        }

        return ValidateDisconnected(runtime);
    }
}
