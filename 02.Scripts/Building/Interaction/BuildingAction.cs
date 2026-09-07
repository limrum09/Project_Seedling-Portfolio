/// <summary>
/// 선택된 Runtime 건물에 실행할 수 있는 건설 행동과 현재 연결 수를 나타냄
/// 행동 가능 여부만 바관하며 실제 건설, 수정 또는 철거는 실행하지 않음
/// </summary>
public sealed class BuildingAction
{
    public bool CanBuildCorridor { get; }
    public bool CanModify { get; }
    public bool CanRemove {  get; }
    public int ConnectedCount { get; }

    /// <summary>
    /// 건물에 실행할 수 있는 행동과 현재 연결 수를 이용해 결과를 생성
    /// </summary>
    /// <param name="canBuildCorridor"></param>
    /// <param name="canModify"></param>
    /// <param name="canRemove"></param>
    /// <param name="connectedCount"></param>
    public BuildingAction(bool canBuildCorridor, bool canModify, bool canRemove, int connectedCount)
    {
        CanBuildCorridor = canBuildCorridor;
        CanModify = canModify;
        CanRemove = canRemove;
        ConnectedCount = connectedCount;
    }

    public static BuildingAction None => new BuildingAction(false, false, false, 0);
}

/// <summary>
/// Runtime 건물의 상태를 이용해 실행 가능한 건설 행동을 판단
/// 건물 상태를 변경하거나 실제 건설 행동을 실행하지 않음
/// </summary>
public static class BuildingActionPolicy
{
    /// <summary>
    /// Runtime 건물의 사용 가능한 Connector와 현재 연결 수를 확인해 건설 행동과 결과를 생성
    /// 유효한 Runtime 건물이 아니면 아무 행동도 허용하지 않는 결과를 반환
    /// </summary>
    public static BuildingAction Evaluate(BuildingRuntime runtime)
    {
        if (runtime == null || runtime.Define == null)
            return BuildingAction.None;

        BuildingCategory category = runtime.Define.Category;

        bool canBuildingCorridor = runtime.HasAvailableConnector;

        return new BuildingAction(canBuildingCorridor, true, true, runtime.ConnectedConnectorCount);
    }
}
