using System;
using UnityEngine;

/// <summary>
/// 가구 배치, 재배치와 제거 요청을 처리하고 Grid와 생산 Registry 등록을 조율
/// </summary>
public class FurnitureService : MonoBehaviour
{
    [SerializeField]
    private FurniturePlacementValidator validator;
    [SerializeField]
    private FurnitureRegistry registry;
    [SerializeField, Min(0.01f)]
    private float cellSize = 0.5f;

    public float CellSize => cellSize;

    public bool CheckEnoughConsume(FurnitureDefine define, IInventoryConsumption inventoryConsumption)
    {
        return inventoryConsumption.CanConsume(define.MaterialItemRequirements);
    }

    /// <summary>
    /// 가구 배치 후보가 현재 설치 가능한지 Validator를 통해 확인
    /// </summary>
    /// <param name="define">배치할 가구의 정적 데이터</param>
    /// <param name="candidate">검사할 가구 배치 후보</param>
    /// <returns>가구를 설치할 수 있으면 true 반환</returns>
    public bool CanPlace(FurnitureDefine define, FurniturePlacementCandidate candidate, IInventoryConsumption inventoryConsumption)
    {
        if(!validator.CanPlace(define, candidate, cellSize))
            return false;

        return CheckEnoughConsume(define, inventoryConsumption);
    }

    /// <summary>
    /// 설치된 가구를 지정한 후보 위치로 재배치 할 수 있는지 확인
    /// 원본 가구의 Grid 점유와 Collider는 검사에서 제외
    /// </summary>
    /// <param name="runtime">재배치할 가구</param>
    /// <param name="candidate">재배치 후보</param>
    /// <returns>재배치할 수 있느면 true</returns>
    public bool CanReplace(FurnitureRuntime runtime, FurniturePlacementCandidate candidate)
    {
        if (!registry.Contains(runtime))
            return false;

        return validator.CanPlace(runtime.Define, candidate, cellSize, runtime);
    }

    public bool CanRemove(FurnitureRuntime runtime)
    {
        return runtime != null && runtime.Define != null && registry.Contains(runtime);
    }

    /// <summary>
    /// 가구 배치 후보를 최종 검증한 후 Runtime 가구를 생성하고 Grid에 등록
    /// Grid 등록이나 재료 소비에 실패하면 생성한 가구를 제거
    /// 설치와 재료 소비가 완료된 뒤 생산 상태를 등록
    /// </summary>
    /// <param name="define">설치할 가구의 정적 데이터</param>
    /// <param name="candidate">설치에 사용할 가구 배치 후보</param>
    /// <param name="inventoryConsumption">재료 소비에 사용할 Inventory 접근점</param>
    /// <param name="placedFurniture">설치에 성공한 Runtime 가구</param>
    /// <returns>가구 설치와 재료 소비가 완료되면 true</returns>
    public bool TryPlace(FurnitureDefine define, FurniturePlacementCandidate candidate, IInventoryConsumption inventoryConsumption, out FurnitureRuntime placedFurniture)
    {
        placedFurniture = null;

        if (!CanPlace(define, candidate, inventoryConsumption))
            return false;

        FurnitureRuntime newFurniture = Instantiate(define.Prefab, candidate.Pos, candidate.Rotation);

        string instanceID = Guid.NewGuid().ToString("N");

        newFurniture.Init(instanceID, define, candidate.Location);

        if (!registry.TryRegister(newFurniture, candidate.OccupiedCells))
        {
            Destroy(newFurniture.gameObject);

            return false;
        }

        if (!inventoryConsumption.TryConsume(define.MaterialItemRequirements))
        {
            if (!registry.TryUnregister(newFurniture))
            {
                throw new InvalidOperationException("가구 배치 Rollback 중 Registry 등록 해제 실패");
            }

            Destroy(newFurniture.gameObject);

            return false;
        }

        placedFurniture = newFurniture;

        return true;
    }

    
    public bool TryReplace(FurnitureRuntime runtime, FurniturePlacementCandidate candidate)
    {
        if (!CanReplace(runtime, candidate))
            return false;

        if (!registry.TryUpdate(runtime, candidate.Location, candidate.OccupiedCells))
            return false;

        runtime.transform.SetPositionAndRotation(candidate.Pos, candidate.Rotation);

        runtime.SetLocation(candidate.Location);

        return true;
    }

    /// <summary>
    /// 가구의 Grid 등록과 생산 등록을 해제하고 Runtime 가구를 제거
    /// Grid 등록 해제에 실패하면 생산 등록과 가구를 유지
    /// </summary>
    /// <param name="runtime">제거할 Runtime 가구</param>
    /// <returns>가구 제거에 성공하면 true</returns>
    public bool TryRemove(FurnitureRuntime runtime)
    {
        if (!CanRemove(runtime))
            return false;

        if (!registry.TryUnregister(runtime))
            return false;

        Destroy(runtime.gameObject);

        return true;
    }

    /// <summary>
    /// Scene에 배치된 가구를 가까운 Grid 위치로 맞추고 초기화 및 등록
    /// 기존 배치 검증에서 자기 Collider를 제외하며 가구 생성과 재료 소비는 수행하지 않음
    /// </summary>
    /// <param name="runtime">등록할 Scene 가구</param>
    /// <param name="ownerBuilding">실내 가구의 소속 건물이며 실외이면 null</param>
    public void RegisterExisting(FurnitureRuntime runtime, BuildingRuntime ownerBuilding)
    {
        FurnitureDefine define = runtime.Define;

        if (define == null)
            throw new InvalidOperationException($"{runtime.name}: FurnitureDefine을 지정해야 함");

        if (registry.Contains(runtime))
            throw new InvalidOperationException($"{runtime.name}: 이미 등록된 가구");

        Quaternion originRotation = ownerBuilding != null ? ownerBuilding.transform.rotation : Quaternion.identity;

        Quaternion relativeRotation = Quaternion.Inverse(originRotation) * runtime.transform.rotation;

        int rotationStep = Mathf.RoundToInt(relativeRotation.eulerAngles.y / 90f) % 4;

        if (!FurniturePlacementCalculator.TryCreateCandidate(define, runtime.transform.position, rotationStep, ownerBuilding, cellSize, out FurniturePlacementCandidate candidate))
            throw new InvalidOperationException($"{runtime.name}: 시작 가구 배치 계산 실패");

        if (!validator.CanPlace(define, candidate, cellSize, runtime))
            throw new InvalidOperationException($"{runtime.name}: 바닥, 소속 건물 또는 배치 충돌을 확인");

        runtime.transform.SetPositionAndRotation(candidate.Pos, candidate.Rotation);

        runtime.Init(Guid.NewGuid().ToString("N"), define, candidate.Location);

        if (!registry.TryRegister(runtime, candidate.OccupiedCells))
            throw new InvalidOperationException($"{runtime.name}: 시작 가구 Grid 등록 실패");
    }
}