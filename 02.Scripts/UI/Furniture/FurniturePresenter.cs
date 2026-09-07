using System.Collections.Generic;
using UnityEngine;

public class FurniturePresenter : IUISelectionHandler<FurnitureCategory>
{
    private IFurniturePlacementAccess furnitureAccess;
    private IInventoryReadAccess inventoryReadAccess;
    private FurniturePanel panel;

    private FurnitureDefine hoverDefine;
    private RectTransform hoverRect;
    private FurnitureCategory furnitureCategory;
    private bool isInit;

    private void ClearHoverInfo()
    {
        hoverDefine = null;
        hoverRect = null;

        panel.InfoPanelHide();
    }

    private void SelectFurniture(FurnitureDefine define)
    {
        furnitureAccess.Begin(define);
    }

    private void EnterPointer(FurnitureDefine define, RectTransform rect)
    {
        hoverDefine = define;
        hoverRect = rect;

        RefreshHoverInfo();
    }

    private void ExitPointer()
    {
        ClearHoverInfo();
    }

    private void CategoryChanged(FurnitureCategory category)
    {
        furnitureCategory = category;

        ClearHoverInfo();
        RefreshSlot();
    }

    private void InventoryChanged(InventoryChangeSet changeSet)
    {
        RefreshHoverInfo();
        RefreshSlot();
    }

    private void FurnitureStateChanged(FurnitureState state)
    {
        if (state == FurnitureState.SelectingFurniture)
            return;

        ClearHoverInfo();
    }

    /// <summary>
    /// 가구 요구 재료를 InfoPanel 표시 데이터로 변환
    /// </summary>
    /// <param name="define">표시할 가구 정의</param>
    /// <param name="inventoryAmounts">현재 Inventory 보유량 조회</param>
    /// <returns>재료별 현재 보유량과 필요 수량</returns>
    private List<ItemRequirementViewData> CreateMaterialViewData(FurnitureDefine define, InventoryAmountLookup inventoryAmounts)
    {
        IReadOnlyList<ItemRequirementState> states = inventoryAmounts.CreateRequirementStates(define.MaterialItemRequirements);

        List<ItemRequirementViewData> materials = new List<ItemRequirementViewData>(states.Count);

        for (int i = 0; i < states.Count; i++)
        {
            ItemRequirementState state = states[i];

            materials.Add(
                new ItemRequirementViewData(state.Item.DisplayName, state.InventoryAmount, state.RequiredAmount));
        }

        return materials;
    }

    /// <summary>
    /// 현재 Category와 Inventory 보유량에 맞춰 가구 Slot 상태 갱신
    /// </summary>
    private void RefreshSlot()
    {
        InventoryAmountLookup inventoryAmounts = new InventoryAmountLookup(inventoryReadAccess.GetAllSnapshot());

        foreach (FurnitureSlotView slot in panel.Slots)
        {
            bool isVisible = furnitureCategory == FurnitureCategory.All || furnitureCategory == slot.Define.Category;

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

    public FurniturePresenter(IFurniturePlacementAccess getFurnitureAccess, IInventoryReadAccess readAccess, FurniturePanel getPanel)
    {
        furnitureAccess = getFurnitureAccess;
        inventoryReadAccess = readAccess;
        panel = getPanel;
    }

    public void Init()
    {
        if (isInit)
            return;

        isInit = true;

        panel.OnSelectedFurniture += SelectFurniture;
        panel.OnPointerEnter += EnterPointer;
        panel.OnPointerExit += ExitPointer;
        panel.OnCategoryRequested += CategoryChanged;

        inventoryReadAccess.OnInventoryChange += InventoryChanged;
        furnitureAccess.OnChangedState += FurnitureStateChanged;
    }

    public void Dispose()
    {
        if (!isInit)
            return;

        isInit = false;

        panel.OnSelectedFurniture -= SelectFurniture;
        panel.OnPointerEnter -= EnterPointer;
        panel.OnPointerExit -= ExitPointer;
        panel.OnCategoryRequested -= CategoryChanged;

        inventoryReadAccess.OnInventoryChange -= InventoryChanged;
        furnitureAccess.OnChangedState -= FurnitureStateChanged;

        hoverDefine = null;
        hoverRect = null;
    }

    public void Show()
    {
        furnitureAccess.StartPlaceMode();
        RefreshSlot();
    }

    public void Hide()
    {
        ClearHoverInfo();
        furnitureAccess.CancelPlaceMode();
    }

    public void Select(FurnitureCategory value)
    {
        furnitureCategory = value;

        ClearHoverInfo();
        RefreshSlot();
    }
}
