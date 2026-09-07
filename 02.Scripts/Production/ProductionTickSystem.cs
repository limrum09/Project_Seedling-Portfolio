using UnityEngine;

/// <summary>
/// 등록된 건물과 가구 중 생산 기능이 있는 시설에 경과 시간을 전달
/// 시설 등록과 아이템 회수는 담당하지 않음
/// </summary>
public class ProductionTickSystem : MonoBehaviour
{
    [SerializeField]
    private GridRegistry buildingRegistry;
    [SerializeField]
    private FurnitureRegistry furnitureRegistry;

    /// <summary>
    /// 현재 Frame의 경과 시간을 건물과 가구의 생산 상태에 반영
    /// </summary>
    private void Update()
    {
        float deltaTime = Time.deltaTime;

        AdvanceBuildings(deltaTime);
        AdvanceFurniture(deltaTime);
    }

    /// <summary>
    /// 등록된 건물 중 생산 기능이 있는 건물에 경과 시간을 전달
    /// </summary>
    /// <param name="deltaTime">이번 Frame에서 경과한 시간</param>
    private void AdvanceBuildings(float deltaTime)
    {
        foreach (BuildingRuntime building in buildingRegistry.RegisteredBuildings)
        {
            ProductionRuntime production = building.Production;

            if (production != null)
                production.Advance(deltaTime);
        }
    }

    /// <summary>
    /// 등록된 가구 중 생산 기능이 있는 가구에 경과 시간을 전달
    /// </summary>
    /// <param name="deltaTime">이번 Frame에서 경과한 시간</param>
    private void AdvanceFurniture(float deltaTime)
    {
        foreach (FurnitureRuntime furniture in furnitureRegistry.RegisteredFurniture)
        {
            ProductionRuntime production = furniture.Production;

            if (production != null)
                production.Advance(deltaTime);
        }
    }
}