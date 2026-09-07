using System.Collections.Generic;
using UnityEngine;

public class BuildingPresenter : IUISelectionHandler<BuildingCategory>
{
    private IBuildingPlacementAccess buildAccess;
    private IInventoryReadAccess inventoryReadAccess;
    private BuildingPanel panel;

    private BuildingDefine hoverDefine;
    private RectTransform hoverRect;
    private BuildingCategory buildingCategory;
    private bool isInit;

    private void ClearHoverInfo()
    {
        hoverDefine = null;
        hoverRect = null;

        panel.InfoPanelHide();
    }

    private void SelectBuilding(BuildingDefine define)
    {
        buildAccess.StartSingleBuild(define);
    }

    private void EnterPointer(BuildingDefine define, RectTransform rect)
    {
        hoverDefine = define;
        hoverRect = rect;

        RefreshHoverInfo();
    }

    private void ExitPointer()
    {
        ClearHoverInfo();
    }

    private void CategoryChanged(BuildingCategory category)
    {
        buildingCategory = category;

        ClearHoverInfo();
        RefreshSlot();
    }

    private void InventoryChanged(InventoryChangeSet changeSet)
    {
        RefreshHoverInfo();
        RefreshSlot();
    }

    private void BuildStateChanged(BuildState state)
    {
        if (state == BuildState.BuildingSelected)
            return;

        ClearHoverInfo();
    }

    /// <summary>
    /// 건물 요구 재료를 InfoPanel 표시 데이터로 변환
    /// </summary>
    /// <param name="define">표시할 건물 정의</param>
    /// <param name="inventoryAmounts">현재 Inventory 보유량 조회</param>
    /// <returns>재료별 현재 보유량과 필요 수량</returns>
    private List<ItemRequirementViewData> CreateMaterialViewData(BuildingDefine define, InventoryAmountLookup inventoryAmounts)
    {
        IReadOnlyList<ItemRequirementState> states = inventoryAmounts.CreateRequirementStates(define.MaterialItemRequirements);

        List<ItemRequirementViewData> materials = new List<ItemRequirementViewData>(states.Count);

        for (int i = 0; i < states.Count; i++)
        {
            ItemRequirementState state = states[i];

            materials.Add(new ItemRequirementViewData(state.Item.DisplayName, state.InventoryAmount, state.RequiredAmount));
        }

        return materials;
    }

    /// <summary>
    /// 현재 Category와 Inventory 보유량에 맞춰 건물 Slot 상태 갱신
    /// </summary>
    private void RefreshSlot()
    {
        InventoryAmountLookup inventoryAmounts = new InventoryAmountLookup(
                inventoryReadAccess.GetAllSnapshot());

        foreach (BuildingSlotView slot in panel.Slots)
        {
            bool isVisible = buildingCategory == BuildingCategory.All || buildingCategory == slot.Define.Category;

            slot.SetVisible(isVisible);

            bool canConstruct = inventoryAmounts.HasEnough(slot.Define.MaterialItemRequirements);

            slot.SetCanConstruct(canConstruct);
        }
    }

    private void RefreshHoverInfo()
    {
        if (hoverDefine == null || hoverRect == null)
            return;

        InventoryAmountLookup inventoryAmount = new InventoryAmountLookup(inventoryReadAccess.GetAllSnapshot());
        
        List<ItemRequirementViewData> materials = CreateMaterialViewData(hoverDefine, inventoryAmount);

        panel.InfoPanelShow(hoverDefine, materials, hoverRect);
    }

    public BuildingPresenter(IBuildingPlacementAccess getBuildAccess, IInventoryReadAccess readAccess, BuildingPanel getPanel)
    {
        buildAccess = getBuildAccess;
        inventoryReadAccess = readAccess;
        panel = getPanel;
    }

    public void Init()
    {
        if (isInit)
            return;

        isInit = true;

        panel.OnSelectedFurniture += SelectBuilding;
        panel.OnPointerEnter += EnterPointer;
        panel.OnPointerExit += ExitPointer;
        panel.OnCategoryRequested += CategoryChanged;

        inventoryReadAccess.OnInventoryChange += InventoryChanged;
        buildAccess.OnChangedState += BuildStateChanged;
    }

    public void Dispose()
    {
        if (!isInit)
            return;

        isInit = false;

        panel.OnSelectedFurniture -= SelectBuilding;
        panel.OnPointerEnter -= EnterPointer;
        panel.OnPointerExit -= ExitPointer;
        panel.OnCategoryRequested -= CategoryChanged;

        inventoryReadAccess.OnInventoryChange -= InventoryChanged;
        buildAccess.OnChangedState  -= BuildStateChanged;

        hoverDefine = null;
        hoverRect = null;
    }

    public void Show()
    {
        buildAccess.StartBuildMode();
        RefreshSlot();
    }

    public void Hide()
    {
        ClearHoverInfo();
        buildAccess.ExitBuildMode();
    }

    public void Select(BuildingCategory value)
    {
        buildingCategory = value;

        ClearHoverInfo();
        RefreshSlot();
    }
}
