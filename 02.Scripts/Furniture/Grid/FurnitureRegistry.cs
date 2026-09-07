using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 가구 공간과 셀 좌표를 결합한 Runtime 점유 키
/// </summary>
internal readonly struct FurnitureCellKey : IEquatable<FurnitureCellKey>
{
    private readonly FurnitureSpaceType spaceType;
    private readonly int ownerBuildingInstanceID;
    private readonly Vector2Int cell;

    /// <summary>
    /// 가구 점유 셀 키를 생성
    /// </summary>
    /// <param name="spaceType">셀이 속한 공간 종류</param>
    /// <param name="ownerBuilding">실내 공간을 소유한 Runtime 건물</param>
    /// <param name="cell">공간 내부의 셀 좌표</param>
    public FurnitureCellKey(FurnitureSpaceType spaceType, BuildingRuntime ownerBuilding, Vector2Int cell)
    {
        this.spaceType = spaceType;
        this.ownerBuildingInstanceID = ownerBuilding != null ? ownerBuilding.GetInstanceID() : 0;
        this.cell = cell;
    }

    /// <summary>
    /// 다른 가구 셀 키와 공간, 소유 건물, 셀 좌표가 같은지 확인
    /// </summary>
    /// <param name="other">비교할 가구 셀 키</param>
    /// <returns>모든 키 값이 같으면 true 반환</returns>
    public bool Equals(FurnitureCellKey other)
    {
        return spaceType == other.spaceType && ownerBuildingInstanceID == other.ownerBuildingInstanceID && cell == other.cell;
    }

    /// <summary>
    /// 지정한 객체가 같은 가구 셀 키인지 확인
    /// </summary>
    /// <param name="obj">비교할 객체</param>
    /// <returns>같은 가구 셀 키이면 true 반환</returns>
    public override bool Equals(object obj)
    {
        return obj is FurnitureCellKey other && Equals(other);
    }

    /// <summary>
    /// 공간, 소유 건물, 셀 좌표를 이용해 Hash 값을 계산
    /// </summary>
    /// <returns>가구 셀 키의 Hash 값</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;

            hash = hash * 31 + (int)spaceType;
            hash = hash * 31 + ownerBuildingInstanceID;
            hash = hash * 31 + cell.GetHashCode();

            return hash;
        }
    }
}

/// <summary>
/// 설치된 가구와 공간별 점유 셀의 관계를 관리
/// 가구 배치 규칙을 판단하거나 가구를 생성 및 제거하지 않음
/// </summary>
public class FurnitureRegistry : MonoBehaviour
{
    private readonly Dictionary<FurnitureCellKey, FurnitureRuntime> occupiedCells = new Dictionary<FurnitureCellKey, FurnitureRuntime>();

    private readonly Dictionary<FurnitureRuntime, List<FurnitureCellKey>> furnitureCells = new Dictionary<FurnitureRuntime, List<FurnitureCellKey>>();

    public IEnumerable<FurnitureRuntime> RegisteredFurniture => furnitureCells.Keys;

    public event Action<FurnitureRuntime> OnFurnitureRegistered;
    public event Action<FurnitureRuntime> OnFurnitureUnregistered;

    /// <summary>
    /// 가구 위치의 공간 종류와 소유 건물 관계가 유효한지 확인
    /// </summary>
    /// <param name="location">확인할 가구 위치</param>
    /// <returns>공간과 소유 건물 관계가 유효하면 true 반환</returns>
    private bool IsValidLocation(FurnitureLocation location)
    {
        if (location.SpaceType == FurnitureSpaceType.Indoor)
            return location.OwnerBuilding != null;

        return location.OwnerBuilding == null;
    }

    /// <summary>
    /// 가구 위치와 셀 목록을 Registry에서 사용할 점유 키 목록으로 변환
    /// </summary>
    /// <param name="location">셀들이 속한 가구 위치</param>
    /// <param name="cells">변환할 셀 목록</param>
    /// <param name="keys">생성한 점유 키 목록</param>
    /// <returns>유효한 점유 키 목록을 생성하면 true 반환</returns>
    private bool TryCreateKeys(FurnitureLocation location, IReadOnlyList<Vector2Int> cells, out List<FurnitureCellKey> keys)
    {
        keys = null;

        if (!IsValidLocation(location))
            return false;

        if (cells == null || cells.Count == 0)
            return false;

        HashSet<Vector2Int> uniqueCells = new HashSet<Vector2Int>();

        List<FurnitureCellKey> createdKeys = new List<FurnitureCellKey>(cells.Count);

        foreach (Vector2Int cell in cells)
        {
            if (!uniqueCells.Add(cell))
                return false;

            createdKeys.Add(new FurnitureCellKey(location.SpaceType, location.OwnerBuilding, cell));
        }

        keys = createdKeys;

        return true;
    }

    /// <summary>
    /// 지정한 공간의 모든 셀을 가구가 점유할 수 있는지 확인
    /// </summary>
    /// <param name="location">가구가 설치될 공간과 위치</param>
    /// <param name="cells">점유 가능 여부를 확인할 셀 목록</param>
    /// <param name="ignore">점유 검사에서 제외할 가구</param>
    /// <returns>모든 셀을 점유할 수 있으면 true 반환</returns>
    public bool CanOccupy(FurnitureLocation location, IReadOnlyList<Vector2Int> cells, FurnitureRuntime ignore = null)
    {
        if (!TryCreateKeys(location, cells, out List<FurnitureCellKey> keys))
            return false;

        foreach (FurnitureCellKey key in keys)
        {
            if (occupiedCells.TryGetValue(key, out FurnitureRuntime occupant) && occupant != ignore)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 설치된 가구와 점유 셀을 Registry에 등록
    /// 이미 등록된 가구이거나 셀을 점유할 수 없으면 등록하지 않음
    /// </summary>
    /// <param name="runtime">등록할 Runtime 가구</param>
    /// <param name="cells">가구가 점유할 셀 목록</param>
    /// <returns>가구와 모든 점유 셀을 등록하면 true 반환</returns>
    public bool TryRegister(FurnitureRuntime runtime, IReadOnlyList<Vector2Int> cells)
    {
        if (runtime == null || runtime.Define == null)
            return false;

        if (furnitureCells.ContainsKey(runtime))
            return false;

        FurnitureLocation location = runtime.Location;

        if (!TryCreateKeys(location, cells, out List<FurnitureCellKey> keys))
            return false;

        if (!CanOccupy(location, cells))
            return false;

        foreach (FurnitureCellKey key in keys)
        {
            occupiedCells.Add(key, runtime);
        }

        furnitureCells.Add(runtime, keys);

        OnFurnitureRegistered?.Invoke(runtime);

        return true;
    }

    /// <summary>
    /// 등록된 가구의 점유 위치롸 셀 목록을 변경
    /// 다른 가구가 점유한 셀이 포함되면 기존 등록을 유지하고 실패
    /// </summary>
    /// <param name="runtime">점유 정보를 변경할 Runtime 가구</param>
    /// <param name="newLocation">변경할 가구 위치</param>
    /// <param name="newCells">변경할 점유 셀 목록</param>
    /// <returns>점유 정보 변경에 성공하면 true 반환</returns>
    public bool TryUpdate(FurnitureRuntime runtime, FurnitureLocation newLocation, IReadOnlyList<Vector2Int> newCells)
    {
        if(runtime == null || runtime.Define == null)
            return false;

        if (!furnitureCells.TryGetValue(runtime, out List<FurnitureCellKey> oldKeys))
            return false;

        if (!TryCreateKeys(newLocation, newCells, out List<FurnitureCellKey> newKeys))
            return false;

        foreach(FurnitureCellKey newKey in newKeys)
        {
            if (occupiedCells.TryGetValue(newKey, out FurnitureRuntime occupant) && occupant != runtime)
                return false;
        }

        foreach(FurnitureCellKey oldKey in oldKeys)
        {
            if(occupiedCells.TryGetValue(oldKey, out FurnitureRuntime occupant) && occupant == runtime)
                occupiedCells.Remove(oldKey);
        }

        foreach (FurnitureCellKey newKey in newKeys)
        {
            occupiedCells.Add(newKey, runtime);
        }

        furnitureCells[runtime] = newKeys;

        return true;
    }

    /// <summary>
    /// 가구가 점유한 모든 셀과 가구 등록 정보를 제거
    /// 점유 상태가 하나라도 일치하지 않으면 기존 등록을 유지하고 시패
    /// </summary>
    /// <param name="runtime">등록을 해제할 Runtime 가구</param>
    /// <returns>모든 점유 셀과 가구 등록을 해제하면서 true</returns>
    public bool TryUnregister(FurnitureRuntime runtime)
    {
        if (runtime == null)
            return false;

        if (!furnitureCells.TryGetValue(runtime, out List<FurnitureCellKey> keys))
            return false;

        foreach (FurnitureCellKey key in keys)
        {
            if (!occupiedCells.TryGetValue(key, out FurnitureRuntime occupant))
                return false;

            if(occupant != runtime)
                return false;
        }

        foreach(FurnitureCellKey key in keys)
        {
            occupiedCells.Remove(key);
        }
        
        furnitureCells.Remove(runtime);

        OnFurnitureUnregistered?.Invoke(runtime);

        return true;
    }

    public bool Contains(FurnitureRuntime runtime) => runtime != null && furnitureCells.ContainsKey(runtime);
}