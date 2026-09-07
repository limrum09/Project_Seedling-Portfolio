using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryPanel : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup group;

    [Header("Panels")]
    [SerializeField]
    private InventoryItemInfo infoPanel;
    [SerializeField]
    private InventoryDragIcon dragPanel;
    [SerializeField]
    private ItemRemoveCheckPanel removeCheckPanel;


    [Header("Inventory Slots")]
    [SerializeField]
    private InvenSlotView prefabInvenSlot;
    [SerializeField]
    private Transform invenSlotPos;
    [SerializeField]
    private List<InvenSlotView> invenSlots = new List<InvenSlotView>();

    [Header("Slider")]
    [SerializeField]
    private TextMeshProUGUI weightText;
    [SerializeField]
    private Slider inventorySlider;
    [SerializeField]
    private Image inventoryFill;

    [Header("Color")]
    [SerializeField]
    private Color normalWeightColor;
    [SerializeField]
    private Color enoughWeightColor;
    [SerializeField]
    private Color overWeightColor;
    [SerializeField]
    private Color maxiumWeightColor;

    private float normalInvenWeight;
    private float maxInvenWeight;
    private float currentInvenWeight;
    private int selectSlotIndex;
    private bool isDragging;

    public event Action<int, int> OnSlotMoveRequested;
    public event Action<int> OnSlotDiscardRequested;

    private void OnDestroy()
    {
        OnSlotMoveRequested = null;
        OnSlotDiscardRequested = null;

        isDragging = false;
        selectSlotIndex = -1;
    }

    private void ShowRemoveCheckPanel()
    {
        removeCheckPanel.Show();

        InvenSlotView slot = invenSlots[selectSlotIndex];

        removeCheckPanel.SetText(slot.SlotItem.DisplayName, slot.Amount, slot.SlotItem.UnitWeight);
    }

    private void SelectSlot(int index)
    {
        selectSlotIndex = index;

        ItemDefine slotItem = invenSlots[selectSlotIndex].SlotItem;

        if (slotItem == null)
        {
            selectSlotIndex = -1;
            return;
        }   

        infoPanel.HideItemInfo();
        isDragging = true;

        dragPanel.Show(slotItem.Icon);
    }

    private void MoveDragIcon(Vector2 mosuePos)
    {
        if (!isDragging)
            return;

        dragPanel.MoveDragIcon(mosuePos);
    }

    private void MoveDragEnd(int index)
    {
        if (!isDragging)
            return;

        isDragging = false;

        if (index < 0)
        {
            dragPanel.Hide();
            isDragging = false;

            ShowRemoveCheckPanel();

            return;
        }

        // 위치 스왑 Service로 요청해야함
        int firstIndex = selectSlotIndex;
        selectSlotIndex = -1;
        dragPanel.Hide();

        int secondIndex = index;

        if (firstIndex == secondIndex)
            return;

        OnSlotMoveRequested?.Invoke(firstIndex, secondIndex);
    }

    private void MouseEnterSlot(int index)
    {
        if (isDragging)
            return;

        ItemDefine slotItem = invenSlots[index].SlotItem;

        if (slotItem == null)
        {
            infoPanel.HideItemInfo();
            return;
        }

        ShowSlotItemInfo(index);
    }

    private void MouseExitSlot()
    {
        if (isDragging)
            return;

        infoPanel.HideItemInfo();
    }

    private void CancelDrag()
    {
        if (!isDragging)
            return;

        isDragging = false;
        selectSlotIndex = -1;
        dragPanel.Hide();
    }

    private Color GetWeightColor()
    {
        if (currentInvenWeight == normalInvenWeight)
            return enoughWeightColor;

        if (currentInvenWeight > normalInvenWeight && currentInvenWeight < maxInvenWeight)
            return overWeightColor;

        if (currentInvenWeight >= maxInvenWeight)
            return maxiumWeightColor;

        return normalWeightColor;
    }

    private void WeightText()
    {
        string color = ColorUtility.ToHtmlStringRGB(GetWeightColor());
        weightText.text = $"가방 : <color=#{color}>{currentInvenWeight:F1} </color>/ {normalInvenWeight:F1}";
    }

    private void UpdateInventorySlider()
    {
        float sliderValue = Mathf.Clamp(currentInvenWeight, 0f, normalInvenWeight);

        inventorySlider.SetValueWithoutNotify(sliderValue);
        inventoryFill.color = GetWeightColor();
    }

    public void InitSlots(int slotCount)
    {
        invenSlots.Clear();

        for (int i = 0; i < slotCount; i++)
        {
            InvenSlotView newSlot = Instantiate(prefabInvenSlot, invenSlotPos);
            newSlot.SetSnapshot(new InventorySlotSnapshot(i, null, 0));

            newSlot.OnEnterPointer += MouseEnterSlot;
            newSlot.OnExitPointer += MouseExitSlot;
            newSlot.OnCancelPointer += CancelDrag;
            newSlot.OnDragStart += SelectSlot;
            newSlot.OnDragMove += MoveDragIcon;
            newSlot.OnDragEnd += MoveDragEnd;

            invenSlots.Add(newSlot);
        }

        removeCheckPanel.Hide();
    }

    public void InitInventory(float normalWeight, float maxWeight)
    {
        normalInvenWeight = normalWeight;
        maxInvenWeight = maxWeight;

        inventorySlider.minValue = 0f;
        inventorySlider.maxValue = normalInvenWeight;
        inventorySlider.SetValueWithoutNotify(0f);

        UpdateInventorySlider();
    }

    public void SetSlot(InventoryChangeSet changeSet)
    {
        foreach (InventorySlotSnapshot snapshot in changeSet.Snapshots)
        {
            invenSlots[snapshot.Index].SetSnapshot(snapshot);
        }

        currentInvenWeight = changeSet.CurrentInventoryWeight;
        WeightText();
        UpdateInventorySlider();
    }

    public void SetVisible(bool isVisivle)
    {
        group.alpha = isVisivle ? 1 : 0;
        group.blocksRaycasts = isVisivle;
        group.interactable = isVisivle;

        infoPanel.HideItemInfo();

        if (!isVisivle)
        {
            dragPanel.Hide();
            isDragging = false;
            selectSlotIndex = -1;
        }
    }

    public void ShowSlotItemInfo(int index)
    {
        ItemDefine item = invenSlots[index].SlotItem;
        infoPanel.ShowItemInfo(item, invenSlots[index].Rect);
    }

    public void TryRemoveSlot()
    {
        OnSlotDiscardRequested?.Invoke(selectSlotIndex);

        removeCheckPanel.Hide();
        selectSlotIndex = -1;
    }

    public void CancelRemoveSlot()
    {
        removeCheckPanel.Hide();
        selectSlotIndex = -1;
    }
}
