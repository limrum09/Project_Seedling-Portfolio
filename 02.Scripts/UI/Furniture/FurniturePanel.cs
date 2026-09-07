using System;
using System.Collections.Generic;
using UnityEngine;

public class FurniturePanel : MonoBehaviour
{
    [Serializable]
    private sealed class FurnitureCategoryToggleBind
    {
        [SerializeField]
        private FurnitureCategory category;
        [SerializeField]
        private UIToggleView toggleView;

        public void Bind(IUISelectionHandler<FurnitureCategory> handler)
        {
            toggleView.Bind(category, handler);
        }

        public void UnBind()
        {
            toggleView.Unbind();
        }
    }

    [SerializeField]
    private CanvasGroup group;
    [SerializeField]
    private FurnitureInfoPanel infoPanel;

    [Header("Category")]
    [SerializeField]
    private List<FurnitureCategoryToggleBind> categoryToggleBinds = new List<FurnitureCategoryToggleBind>();

    [Header("Slots")]
    [SerializeField]
    private List<FurnitureSlotView> slots = new List<FurnitureSlotView>();

    public event Action<FurnitureDefine> OnSelectedFurniture;
    public event Action<FurnitureDefine, RectTransform> OnPointerEnter;
    public event Action OnPointerExit;
    public event Action<FurnitureCategory> OnCategoryRequested;

    public IReadOnlyList<FurnitureSlotView> Slots => slots;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        for(int i = 0; i < slots.Count; i++)
        {
            slots[i].OnClickSlot += OnClickSlot;
            slots[i].OnEnterPointer += OnEnterPointer;
            slots[i].OnExitPointer += OnExitPointer;
        }
    }

    public void BindCategoryToggle(IUISelectionHandler<FurnitureCategory> handler)
    {
        for(int i = 0; i < categoryToggleBinds.Count; i++)
        {
            categoryToggleBinds[i].Bind(handler);
        }
    }

    public void UnBindCategoryToggles()
    {
        for(int i = 0; i <categoryToggleBinds.Count; i++)
        {
            categoryToggleBinds[i].UnBind();
        }
    }

    public void InfoPanelShow(FurnitureDefine define, IReadOnlyList<ItemRequirementViewData> materials, RectTransform rect)
    {
        infoPanel.Show(define, materials, rect);
    }

    public void InfoPanelHide()
    {
        infoPanel.Hide();
    }

    public void Show()
    {
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    public void Hide()
    {
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        InfoPanelHide();
    }

    public void OnClickSlot(FurnitureDefine define)
    {
        OnSelectedFurniture?.Invoke(define);
    }

    public void OnEnterPointer(FurnitureDefine define, RectTransform pos)
    {
        OnPointerEnter?.Invoke(define, pos);
    }

    public void OnExitPointer()
    {
        OnPointerExit?.Invoke();
    }
}
