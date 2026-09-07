using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildingPanel : MonoBehaviour
{
    [Serializable]
    private sealed class BuildingCategoryToggleBind
    {
        [SerializeField]
        private BuildingCategory category;
        [SerializeField]
        private UIToggleView toggleView;

        public void Bind(IUISelectionHandler<BuildingCategory> handler)
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
    private BuildingInfoPanel infoPanel;

    [Header("Category")]
    [SerializeField]
    private List<BuildingCategoryToggleBind> categoryToggleBinds = new List<BuildingCategoryToggleBind>();

    [Header("Slots")]
    [SerializeField]
    private List<BuildingSlotView> slots = new List<BuildingSlotView>();

    public event Action<BuildingDefine> OnSelectedFurniture;
    public event Action<BuildingDefine, RectTransform> OnPointerEnter;
    public event Action OnPointerExit;
    public event Action<BuildingCategory> OnCategoryRequested;

    public IReadOnlyList<BuildingSlotView> Slots => slots;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].OnClickSlot += OnClickSlot;
            slots[i].OnEnterPointer += OnEnterPointer;
            slots[i].OnExitPointer += OnExitPointer;
        }
    }

    public void BindCategoryToggle(IUISelectionHandler<BuildingCategory> handler)
    {
        for (int i = 0; i < categoryToggleBinds.Count; i++)
        {
            categoryToggleBinds[i].Bind(handler);
        }
    }

    public void UnBindCategoryToggles()
    {
        for (int i = 0; i < categoryToggleBinds.Count; i++)
        {
            categoryToggleBinds[i].UnBind();
        }
    }

    public void InfoPanelShow(BuildingDefine define, IReadOnlyList<ItemRequirementViewData> materials, RectTransform rect)
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

    public void OnClickSlot(BuildingDefine define)
    {
        OnSelectedFurniture?.Invoke(define);
    }

    public void OnEnterPointer(BuildingDefine define, RectTransform pos)
    {
        OnPointerEnter?.Invoke(define, pos);
    }

    public void OnExitPointer()
    {
        OnPointerExit?.Invoke();
    }
}