using System;
using UnityEngine;

/// <summary>
/// Scene에 배치된 시작 건물과 가구를 순서대로 초기화
/// 배치 계산과 등록 처리는 각 Service에 요청
/// </summary>
public class SceneWorldInitializer : MonoBehaviour
{
    /// <summary>
    /// 시작 가구와 소속 건물 정보를 보관
    /// </summary>
    [Serializable]
    private class StartingFurniture
    {
        [SerializeField]
        private FurnitureRuntime runtime;
        [SerializeField]
        private BuildingRuntime ownerBuilding;

        public FurnitureRuntime Runtime => runtime;
        public BuildingRuntime OwnerBuilding => ownerBuilding;
    }

    [SerializeField]
    private BuildingGrid buildingGrid;
    [SerializeField]
    private BuildingService buildingService;
    [SerializeField]
    private FurnitureService furnitureService;

    [SerializeField]
    private BuildingRuntime[] startingBuildings = Array.Empty<BuildingRuntime>();
    [SerializeField]
    private StartingFurniture[] startingFurniture = Array.Empty<StartingFurniture>();

    private bool initializationAttempted;

    /// <summary>
    /// 시작 건물 배치와 연결을 완료한 뒤 시작 가구를 배치
    /// 초기화는 한 번만 실행하며 실패하면 설정 수정 후 Play를 다시 시작
    /// </summary>
    public void Init()
    {
        if (initializationAttempted)
            throw new InvalidOperationException("시작 시설 초기화는 한 번만 실행할 수 있음");

        initializationAttempted = true;

        foreach (BuildingRuntime building in startingBuildings)
        {
            buildingService.RegisterExisting(building, buildingGrid);
        }

        buildingService.ConnectExisting(startingBuildings);

        // 건물 이동과 Connector 연결 결과를 가구의 물리 검사에 반영
        Physics.SyncTransforms();

        foreach (StartingFurniture furniture in startingFurniture)
        {
            if (furniture.OwnerBuilding != null && Array.IndexOf(startingBuildings, furniture.OwnerBuilding) < 0)
                throw new InvalidOperationException($"{furniture.Runtime.name}: 소속 건물을 시작 건물 목록에 포함해야 함");

            furnitureService.RegisterExisting(furniture.Runtime, furniture.OwnerBuilding);
        }
    }
}