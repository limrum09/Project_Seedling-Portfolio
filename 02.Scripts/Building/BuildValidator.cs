using System.Collections.Generic;
using UnityEngine;

public enum BuildPlacementMode
{
    Ground,
    Connector
}

/// <summary>
/// 건물이 점유할 셀 목록을 이용해 건물 배치 가능 여부를 검증
/// 건물을 생성하거나 셀 점유 정보를 Registry에 등록하지 않음
/// </summary>
public class BuildValidator : MonoBehaviour
{
    [SerializeField]
    private GridRegistry gridRegister;

    /// <summary>
    /// 주어진 모든 셀의 새 건물이 점유할 수 있는지 GridRegirtry를 통해 확인
    /// </summary>
    /// <param name="cells">배치 가능 여부를 확인할 셀 목록</param>
    /// <returns>건물 배치 가능 여부</returns>
    public bool CheckCanPlace(IReadOnlyList<Vector2Int> cells) => CheckCanPlace(cells, null);

    /// <summary>
    /// 지정한 Runtime 건물이 현재 점유한 셀을 제외하고 새로운 셀의 점유 가능 여부를 확인
    /// </summary>
    /// <param name="cells">배치 가능 벼루르 확인할 셀 목록</param>
    /// <param name="ignore">점유 검사에서 제외할 Runtime 건물</param>
    /// <returns>점유 충돌이 없다면 true 반환</returns>
    public bool CheckCanPlace(IReadOnlyList<Vector2Int> cells, BuildingRuntime ignore) => gridRegister.CanOccupy(cells, ignore);
}
