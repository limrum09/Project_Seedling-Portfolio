using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 설치된 생산 시설에 World UI를 생성하고 필요한 참조를 연결
/// UI에서 전달한 회수 요청은 ProductionCollectService에 위임
/// </summary>
public sealed class ProductionWorldUIBinder : MonoBehaviour
{
    [Header("Registry")]
    [SerializeField]
    private GridRegistry buildingRegistry;
    [SerializeField]
    private FurnitureRegistry furnitureRegistry;

    [Header("UI")]
    [SerializeField]
    private ProductionWorldUI uiPrefab;

    [Header("Local Position")]
    [SerializeField]
    private Vector3 buildingOffset = new Vector3(0f, 3f, 0f);
    [SerializeField]
    private Vector3 furnitureOffset = new Vector3(0f, 2f, 0f);

    private readonly Dictionary<Transform, ProductionWorldUI> worldUIs = new Dictionary<Transform, ProductionWorldUI>();
    private readonly ProductionCollectService collectService = new ProductionCollectService();

    // Bind 전과 Unbind 후에는 연결 정보가 없는 상태
    private Camera worldCamera;
    private IInventoryItemReceiver itemReceiver;
    private bool isBound;

    private void OnEnable()
    {
        if (!isBound)
            return;

        ConnectFacilities();
    }

    private void OnDisable()
    {
        DisconnectFacilities();
    }

    /// <summary>
    /// 시설 등록과 해제를 구독하고 기존 시설의 UI를 생성
    /// </summary>
    private void ConnectFacilities()
    {
        buildingRegistry.OnBuildingRegistered += HandleBuildingRegistered;
        buildingRegistry.OnBuildingUnregistered += HandleBuildingUnregistered;

        furnitureRegistry.OnFurnitureRegistered += HandleFurnitureRegistered;
        furnitureRegistry.OnFurnitureUnregistered += HandleFurnitureUnregistered;

        foreach (BuildingRuntime building in buildingRegistry.RegisteredBuildings)
        {
            HandleBuildingRegistered(building);
        }

        foreach (FurnitureRuntime furniture in furnitureRegistry.RegisteredFurniture)
        {
            HandleFurnitureRegistered(furniture);
        }
    }

    /// <summary>
    /// 시설 등록과 해제 구독을 해제하고 생성한 UI를 모두 제거
    /// </summary>
    private void DisconnectFacilities()
    {
        if (!isBound)
            return;

        buildingRegistry.OnBuildingRegistered -= HandleBuildingRegistered;
        buildingRegistry.OnBuildingUnregistered -= HandleBuildingUnregistered;

        furnitureRegistry.OnFurnitureRegistered -= HandleFurnitureRegistered;
        furnitureRegistry.OnFurnitureUnregistered -= HandleFurnitureUnregistered;

        foreach (ProductionWorldUI ui in worldUIs.Values)
        {
            DestroyUI(ui);
        }

        worldUIs.Clear();
    }

    /// <summary>
    /// 등록된 건물의 생산 UI를 생성
    /// </summary>
    /// <param name="runtime">등록된 건물</param>
    private void HandleBuildingRegistered(BuildingRuntime runtime)
    {
        CreateUI(runtime.transform, runtime, buildingOffset);
    }

    /// <summary>
    /// 등록 해제된 건물의 생산 UI를 제거
    /// </summary>
    /// <param name="runtime">등록 해제된 건물</param>
    private void HandleBuildingUnregistered(BuildingRuntime runtime)
    {
        RemoveUI(runtime.transform);
    }

    /// <summary>
    /// 등록된 가구의 생산 UI를 생성
    /// </summary>
    /// <param name="runtime">등록된 가구</param>
    private void HandleFurnitureRegistered(FurnitureRuntime runtime)
    {
        CreateUI(runtime.transform, runtime, furnitureOffset);
    }

    /// <summary>
    /// 등록 해제된 가구의 생산 UI를 제거
    /// </summary>
    /// <param name="runtime">등록 해제된 가구</param>
    private void HandleFurnitureUnregistered(FurnitureRuntime runtime)
    {
        RemoveUI(runtime.transform);
    }

    /// <summary>
    /// 생산 기능이 있는 시설의 자식으로 UI를 생성하고 표시와 요청 참조를 연결
    /// Production이 null인 시설은 생산 기능이 없으므로 생성하지 않음
    /// </summary>
    /// <param name="owner">UI를 소유할 시설 Transform</param>
    /// <param name="source">시설의 생산 상태 접근점</param>
    /// <param name="localOffset">시설 기준 UI 위치</param>
    private void CreateUI(Transform owner, IProductionSource source, Vector3 localOffset)
    {
        if (source.Production == null)
            return;

        ProductionWorldUI ui = Instantiate(uiPrefab, owner);
        ui.transform.localPosition = localOffset;

        ui.Bind(source.Production, worldCamera, RequestCollect);

        worldUIs.Add(owner, ui);
    }

    /// <summary>
    /// 시설에 연결된 UI를 제거
    /// 생산 기능이 없어 UI가 생성되지 않은 시설은 변경하지 않음
    /// </summary>
    /// <param name="owner">등록 해제된 시설 Transform</param>
    private void RemoveUI(Transform owner)
    {
        if (!worldUIs.TryGetValue(owner, out ProductionWorldUI ui))
            return;

        worldUIs.Remove(owner);

        DestroyUI(ui);
    }

    /// <summary>
    /// UI 입력과 구독을 먼저 중지한 뒤 오브젝트를 제거
    /// Scene 종료 시 시설과 함께 먼저 파괴된 UI는 건너뜀
    /// </summary>
    /// <param name="ui">제거할 UI이며 시설과 함께 이미 파괴되었을 수 있음</param>
    private void DestroyUI(ProductionWorldUI ui)
    {
        if (ui == null)
            return;

        ui.gameObject.SetActive(false);
        Destroy(ui.gameObject);
    }

    /// <summary>
    /// UI에서 전달한 생산 항목의 회수를 Service에 요청
    /// </summary>
    /// <param name="output">회수할 생산 항목</param>
    private void RequestCollect(ProductionOutputRuntime output)
    {
        collectService.TryCollect(output, itemReceiver, out _);
    }



    /// <summary>
    /// World Camera와 회수 아이템 수신 접근점을 연결
    /// 기존 연결이 있으면 시설 구독과 UI를 정리한 뒤 다시 연결
    /// </summary>
    /// <param name="camera">UI에 전달할 World Camera</param>
    /// <param name="receiver">회수 아이템을 받을 Inventory 접근점</param>
    public void Bind(Camera camera, IInventoryItemReceiver receiver)
    {
        DisconnectFacilities();

        worldCamera = camera;
        itemReceiver = receiver;
        isBound = true;

        if (isActiveAndEnabled)
            ConnectFacilities();
    }

    /// <summary>
    /// 시설 구독과 생성한 UI를 정리하고 연결 정보를 해제
    /// </summary>
    public void Unbind()
    {
        DisconnectFacilities();

        worldCamera = null;
        itemReceiver = null;
        isBound = false;
    }
}