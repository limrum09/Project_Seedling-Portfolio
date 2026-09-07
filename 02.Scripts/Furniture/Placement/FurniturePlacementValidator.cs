using UnityEngine;

/// <summary>
/// 가구 배치 후보의 설치 범위, 바닥, Grid 점유와 물리 충돌을 검증
/// 가구를 생성하거나 Registry에 등록하지 않음
/// </summary>
public class FurniturePlacementValidator : MonoBehaviour
{
    [SerializeField]
    private FurnitureRegistry registry;
    [SerializeField]
    private LayerMask placementSurfaceMask;
    [SerializeField]
    private LayerMask obstacleMask;

    [Header("Values")]
    [SerializeField, Min(0.01f)]
    private float supportRayHeight = 0.25f;
    [SerializeField, Min(0.01f)]
    private float supportRayDistance = 0.5f;
    [SerializeField, Range(0f, 1f)]
    private float minimumFloorNormalDot = 0.7f;
    [SerializeField, Min(0f)]
    private float collisionInset = 0.02f;

    /// <summary>
    /// 가구의 설치 범위와 후보 공간이 일치하는지 확인
    /// </summary>
    /// <param name="define">배치할 가구의 정적 데이터</param>
    /// <param name="location">검사할 가구 위치</param>
    /// <returns>가구가 후보 공간에 설치될 수 있으면 true 반환</returns>
    private bool CheckPlacementScope(FurnitureDefine define, FurnitureLocation location)
    {
        if (define.Scope == FurniturePlacementScope.IndoorOnly)
            return location.SpaceType == FurnitureSpaceType.Indoor;

        return true;
    }

    /// <summary>
    /// 실내 가구가 소속된 건물에서 가구 설치를 허용하는지 확인
    /// </summary>
    /// <param name="location">검사할 가구 위치</param>
    /// <returns>실외이거나 소속 건물이 가구 설치를 허용하면 true 반환</returns>
    private bool CheckOwnerBuilding(FurnitureLocation location)
    {
        if (location.SpaceType == FurnitureSpaceType.Outdoor)
            return location.OwnerBuilding == null;

        if (location.OwnerBuilding == null)
            return false;

        if (location.OwnerBuilding.Define == null)
            return false;

        return location.OwnerBuilding.Define.AllowsFurniturePlacement;
    }

    /// <summary>
    /// 후보의 모든 점유 셀 아래에 같은 공간의 설치 표면이 있는지 확인
    /// </summary>
    /// <param name="candidate">검사할 가구 배치 후보</param>
    /// <param name="cellSize">가구 Grid 한 셀의 크기</param>
    /// <returns>모든 점유 셀이 같은 공간의 바닥에 지지되면 true 반환</returns>
    private bool CheckSupportingSurface(FurniturePlacementCandidate candidate, float cellSize)
    {
        FurnitureLocation location = candidate.Location;
        BuildingRuntime ownerBuilding = location.OwnerBuilding;

        Vector3 originPos = ownerBuilding != null ? ownerBuilding.transform.position : Vector3.zero;

        Quaternion originRotation = ownerBuilding != null ? ownerBuilding.transform.rotation : Quaternion.identity;

        Vector3 localCandidatePos = Quaternion.Inverse(originRotation) * (candidate.Pos - originPos);

        foreach (Vector2Int cell in candidate.OccupiedCells)
        {
            Vector3 cellCenter = FurnitureGridCalculator.CellToWorldCenter(cell, localCandidatePos.y, originPos, originRotation, cellSize);

            Vector3 rayOrigin = cellCenter + Vector3.up * supportRayHeight;

            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, supportRayDistance, placementSurfaceMask, QueryTriggerInteraction.Ignore))
                return false;

            float floorDot = Vector3.Dot(hit.normal, Vector3.up);

            if (floorDot < minimumFloorNormalDot)
                return false;

            BuildingRuntime hitBuilding = hit.collider.GetComponentInParent<BuildingRuntime>();

            if (location.SpaceType == FurnitureSpaceType.Indoor)
            {
                if (hitBuilding != ownerBuilding)
                    return false;
            }
            else if (hitBuilding != null)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 가구의 대표 BoxCollider가 건물 구조물이나 설치 불가 영역과 겹치는지 확인
    /// </summary>
    /// <param name="define">배치할 가구의 정적 데이터</param>
    /// <param name="candidate">검사할 가구 배치 후보</param>
    /// <returns>장애물과 겹치지 않으면 true 반환</returns>
    private bool CheckObstacleCollision(FurnitureDefine define, FurniturePlacementCandidate candidate, FurnitureRuntime ignore)
    {
        BoxCollider placementBounds = define.Prefab.PlacementBounds;

        Vector3 prefabScale = define.Prefab.transform.lossyScale;

        Vector3 scaledCenter = Vector3.Scale(placementBounds.center, prefabScale);

        Vector3 worldCenter = candidate.Pos + candidate.Rotation * scaledCenter;

        Vector3 absoluteScale = new Vector3(Mathf.Abs(prefabScale.x), Mathf.Abs(prefabScale.y), Mathf.Abs(prefabScale.z));

        Vector3 halfExtents = Vector3.Scale(placementBounds.size * 0.5f, absoluteScale);

        halfExtents.x = Mathf.Max(0.001f, halfExtents.x - collisionInset);
        halfExtents.y = Mathf.Max(0.001f, halfExtents.y - collisionInset);
        halfExtents.z = Mathf.Max(0.001f, halfExtents.z - collisionInset);

        Collider[] colliders = Physics.OverlapBox(worldCenter, halfExtents, candidate.Rotation, obstacleMask, QueryTriggerInteraction.Collide);

        foreach(Collider collider in colliders)
        {
            if (ignore != null && collider.GetComponentInParent<FurnitureRuntime>() == ignore)
                continue;

            return false;
        }

        return true;
    }

    /// <summary>
    /// 가구 배치 후보가 모든 설치 조건을 만족하는지 확인
    /// </summary>
    /// <param name="define">배치할 가구의 정적 데이터</param>
    /// <param name="candidate">검사할 가구 배치 후보</param>
    /// <param name="cellSize">가구 Grid 한 셀의 크기</param>
    /// <param name="ignore">Grid 점유 검사에서 제외할 가구</param>
    /// <returns>가구를 설치할 수 있으면 true 반환</returns>
    public bool CanPlace(FurnitureDefine define, FurniturePlacementCandidate candidate, float cellSize, FurnitureRuntime ignore = null)
    {
        if (define == null || candidate == null)
            return false;

        if (!CheckPlacementScope(define, candidate.Location))
            return false;

        if (!CheckOwnerBuilding(candidate.Location))
            return false;

        if (!registry.CanOccupy(candidate.Location, candidate.OccupiedCells, ignore))
            return false;

        if (!CheckSupportingSurface(candidate, cellSize))
            return false;

        return CheckObstacleCollision(define, candidate, ignore);
    }
}