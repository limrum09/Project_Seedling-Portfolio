using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 그리드 셀과 해당 셀을 점유한 Runtime 건물의 관계를 관리
/// 건물을 생성하거나 배치에 필요한 게임 규칙을 판단하지 않음
/// </summary>
public class GridRegistry : MonoBehaviour
{
    Dictionary<Vector2Int, BuildingRuntime> occupiedCell = new Dictionary<Vector2Int, BuildingRuntime>();
    private Dictionary<BuildingRuntime, List<Vector2Int>> buildingCells = new Dictionary<BuildingRuntime, List<Vector2Int>>();

    public IEnumerable<Vector2Int> OccupiedCells => occupiedCell.Keys;
    public IEnumerable<BuildingRuntime> RegisteredBuildings => buildingCells.Keys;

    public event Action<BuildingRuntime> OnBuildingRegistered;
    public event Action<BuildingRuntime> OnBuildingUnregistered;

    /// <summary>
    /// Runtime 건물이 점유할 셀 목록을 Registry에 등록
    /// 건물이 이미 등록되었거나 셀을 점유할 수 없으면 등록하지 않음
    /// </summary>
    /// <param name="runtime">등록할 Runtime 건물</param>
    /// <param name="cells">건물이 점유할 셀들의 목록</param>
    /// <returns>등록 성공 여부</returns>
    public bool TryRegister(BuildingRuntime runtime, IReadOnlyList<Vector2Int> cells)
    {
        if (runtime == null)
            return false;

        if (buildingCells.ContainsKey(runtime))
            return false;

        if (!CanOccupy(cells))
            return false;

        foreach (Vector2Int cell in cells)
        {
            occupiedCell.Add(cell, runtime);
        }

        buildingCells.Add(runtime, new List<Vector2Int>(cells));

        OnBuildingRegistered?.Invoke(runtime);

        return true;
    }

    /// <summary>
    /// Runtime 건물이 점유한 모든 셀과 건물 등록 정보를 제거
    /// 등록되지 않은 건물이면 변경하지 않음
    /// </summary>
    /// <param name="runtime">등록 해제할 Runtime 건물</param>
    public void UnRegister(BuildingRuntime runtime)
    {
        if (!buildingCells.TryGetValue(runtime, out List<Vector2Int> cells))
            return;

        foreach(Vector2Int cell in cells)
        {
            if(TryGetOccupant(cell, out BuildingRuntime occupant) && occupant == runtime)
            {
                occupiedCell.Remove(cell);
            }
        }

        buildingCells.Remove(runtime);

        OnBuildingUnregistered?.Invoke(runtime);
    }

    /// <summary>
    /// 주어진 모든 셀을 새 건물이 점유할 수 있는지 확인
    /// 셀 목록이 없거나 중복된 셀 또는 이미 점유된 셀이 있으면 false를 반환
    /// 넘겨준 모든 셀이 점유 가능하다면 true를 반환
    /// </summary>
    /// <param name="cells">점유 가능 여부를 확인할 셀 목록</param>
    /// <returns>모든 셀의 점유 가능 여부</returns>
    public bool CanOccupy(IReadOnlyList<Vector2Int> cells)
    {
        return CanOccupy(cells, null);
    }

    /// <summary>
    /// 지정한 Runtime 건물이 점유한 셀을 제외하고모든 셀의 점유 가능 여부를 확인
    /// 셀 목록이 비어 있거나 중복된 셀 또는 다른 건물이 점유한 셀이 있으면 false를 반환
    /// </summary>
    /// <param name="cells">점유 가능 여부를 확인할 셀</param>
    /// <param name="ignore">검사에 제외할 건물</param>
    /// <returns>점유 충돌이 없으면 true반환</returns>
    public bool CanOccupy(IReadOnlyList<Vector2Int> cells, BuildingRuntime ignore)
    {
        if (cells == null || cells.Count == 0)
            return false;

        HashSet<Vector2Int> unique = new HashSet<Vector2Int>();

        foreach (Vector2Int cell in cells)
        {
            if (!unique.Add(cell))
                return false;

            if (occupiedCell.TryGetValue(cell, out BuildingRuntime occuped) && occuped != ignore)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 요청한 셀을 점유한 Runtime건물을 확인
    /// </summary>
    /// <param name="cell">점유 건물을 확인할 셀</param>
    /// <param name="runtime">셀을 점유한 Runtime 걸물</param>
    /// <returns>건물의 점유 여부 확인</returns>
    public bool TryGetOccupant(Vector2Int cell, out BuildingRuntime runtime)
    {
        return occupiedCell.TryGetValue(cell, out runtime);
    }

    /// <summary>
    /// 지정한 Runtime 건물이 점유한 셀 목록의 복사본을 반환
    /// </summary>
    /// <param name="runtime">점유 셀을 조회할 Runtime 건물</param>
    /// <param name="cells">조회된 점유 셀 목록, 실패하면 nul</param>
    /// <returns>건물이 Refigtry에 등록 되어 있으면 true</returns>
    public bool TryGetOccupiedCells(BuildingRuntime runtime, out IReadOnlyList<Vector2Int> cells)
    {
        cells = null;

        if (runtime == null)
            return false;

        if (!buildingCells.TryGetValue(runtime, out List<Vector2Int> registerCells))
            return false;

        cells = new List<Vector2Int>(registerCells);

        return true;
    }

    /// <summary>
    /// 등록된 Runtime 건물의 점유 셀을 새로운 셀 목록으로 변경할 수 있는지 확인
    /// 현재 건물이 점유한 셀은 충돌 검사에서 제외
    /// </summary>
    /// <param name="runtime">점유 셀을 변경할 Runtime 건물</param>
    /// <param name="newCells">새로운 점유 셀 목록</param>
    /// <returns>건물이 등록되어 있고, 새로운 셀을 모두 점유할 수 있으면 true반환</returns>
   public bool CanUpdateRegist(BuildingRuntime runtime, IReadOnlyList<Vector2Int> newCells)
    {
        if (runtime == null)
            return false;

        if (!buildingCells.ContainsKey(runtime))
            return false;

        return CanOccupy(newCells, runtime);
    }

    /// <summary>
    /// 등록된 Runtime 건물의 기존 점유 셀을 해제하고 새로운 셀 목록으로 교체
    /// 변경 조건을 만족하지 앟으면 기존 점유 정보 유지
    /// </summary>
    /// <param name="runtime"></param>
    /// <param name="newCells"></param>
    /// <returns>점유 셀 변경에 성공하면 true 반환</returns>
    public bool TryUpdateRegist(BuildingRuntime runtime, IReadOnlyList<Vector2Int> newCells)
    {
        // 기존 점유 정보를 제거하기 전에 새로운 셀 목록 전체를 검증
        if(!CanUpdateRegist(runtime, newCells)) 
            return false;

        // 호출자가 전달한 목록의 변경이 Registry 내부 상태에 영향을 주지 않도록 복사
        List<Vector2Int> oldCells = buildingCells[runtime];
        List<Vector2Int> replaceCells = new List<Vector2Int>(newCells);

        foreach(Vector2Int cell in oldCells)
        {
            if (occupiedCell.TryGetValue(cell, out BuildingRuntime occuped) && occuped == runtime)
                occupiedCell.Remove(cell);
        }

        foreach(Vector2Int cell in replaceCells)
        {
            occupiedCell.Add(cell, runtime);
        }

        buildingCells[runtime] = replaceCells;

        return true;
    }
}
