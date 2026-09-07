using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FurnitureDefine과 바닥 Raycast 결과를 이용해 가구의 배치 후보를 계산
/// 배치 가능 여부를 판단하거나 실제 가구를 생성하지 않음
/// </summary>
public static class FurniturePlacementCalculator
{
    /// <summary>
    /// 바닥 Raycast 결과로 가구의 배치 후보를 계산
    /// </summary>
    /// <param name="define">배치할 가구의 정적 데이터</param>
    /// <param name="surfaceHit">가구를 배치할 표면의 Raycast 결과</param>
    /// <param name="rotationStep">90도 단위의 회전 단계</param>
    /// <param name="cellSize">가구 Grid 한 셀의 크기</param>
    /// <param name="candidate">계산된 가구 배치 후보</param>
    /// <returns>배치 후보 생성에 성공하면 true</returns>
    public static bool TryCreateCandidate(FurnitureDefine define, RaycastHit surfaceHit, int rotationStep, float cellSize, out FurniturePlacementCandidate candidate)
    {
        candidate = null;

        if (surfaceHit.collider == null)
            return false;

        BuildingRuntime ownerBuilding = surfaceHit.collider.GetComponentInParent<BuildingRuntime>();

        return TryCreateCandidate(define, surfaceHit.point, rotationStep, ownerBuilding, cellSize, out candidate, surfaceHit.collider);
    }

    /// <summary>
    /// 월드 위치와 소속 건물을 기준으로 가장 가까운 가구 배치 후보를 계산
    /// </summary>
    /// <param name="define">배치할 가구의 정적 데이터</param>
    /// <param name="worldPos">배치 기준 월드 위치</param>
    /// <param name="rotationStep">설치 공간 기준 90도 단위의 회전 단계</param>
    /// <param name="ownerBuilding">실내 가구의 소속 건물이며 실외이면 null</param>
    /// <param name="cellSize">가구 Grid 한 셀의 크기</param>
    /// <param name="candidate">계산된 가구 배치 후보</param>
    /// <param name="placementSurface">Pointer 배치 표면이며 시작 배치에서는 생략 가능</param>
    /// <returns>배치 후보 생성에 성공하면 true</returns>
    public static bool TryCreateCandidate(FurnitureDefine define, Vector3 worldPos, int rotationStep, BuildingRuntime ownerBuilding, float cellSize, out FurniturePlacementCandidate candidate, Collider placementSurface = null)
    {
        candidate = null;

        if (define == null || cellSize <= 0f)
            return false;

        Vector2Int footprint = define.GridFootprint;

        if (footprint.x <= 0 || footprint.y <= 0)
            return false;

        int normalizedRotationStep = (rotationStep % 4 + 4) % 4;

        FurnitureSpaceType spaceType = ownerBuilding != null ? FurnitureSpaceType.Indoor : FurnitureSpaceType.Outdoor;

        Vector3 originPosition = ownerBuilding != null ? ownerBuilding.transform.position : Vector3.zero;

        Quaternion originRotation = ownerBuilding != null ? ownerBuilding.transform.rotation : Quaternion.identity;

        Vector2Int rotatedFootprint = FurnitureFootprintCalculator.GetRotatedFootprint(footprint, normalizedRotationStep);

        Vector2Int startCell = FurnitureGridCalculator.WorldToStartCell(worldPos, rotatedFootprint, originPosition, originRotation, cellSize);

        List<Vector2Int> occupiedCells = FurnitureFootprintCalculator.GetOccupiedCells(startCell, footprint, normalizedRotationStep);

        Vector3 localPosition = Quaternion.Inverse(originRotation) * (worldPos - originPosition);

        Vector3 position = FurnitureGridCalculator.GetCellsWorldCenter(occupiedCells, localPosition.y, originPosition, originRotation, cellSize);

        Quaternion rotation = originRotation * Quaternion.Euler(0f, normalizedRotationStep * 90f, 0f);

        FurnitureLocation location = new FurnitureLocation(spaceType, ownerBuilding, startCell, normalizedRotationStep);

        candidate = new FurniturePlacementCandidate(location, occupiedCells, position, rotation, placementSurface);

        return true;
    }
}