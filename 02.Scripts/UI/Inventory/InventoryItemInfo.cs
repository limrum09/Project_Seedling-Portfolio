using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemInfo : MonoBehaviour
{
    [SerializeField]
    private GameObject itemPanel;
    [SerializeField]
    private RectTransform inventoryRect;
    [SerializeField]
    private RectTransform rect;

    [Header("Panel View")]
    [SerializeField]
    private Image icon;
    [SerializeField]
    private TextMeshProUGUI itemNameText;
    [SerializeField]
    private TextMeshProUGUI itemRarityText;
    [SerializeField]
    private TextMeshProUGUI itemCategoryText;
    [SerializeField]
    private TextMeshProUGUI itemDescriptionText;
    [SerializeField]
    private TextMeshProUGUI itemWeightText;

    private void SetPanelPos(RectTransform slotRect)
    {
        Bounds slotBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(inventoryRect, slotRect);
        Bounds panelBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(inventoryRect, rect);

        Rect bounds = inventoryRect.rect;

        float panelWidth = panelBounds.size.x;
        float panelHeight = panelBounds.size.y;

        float rightSpace = bounds.xMax - slotBounds.max.x - 10f;
        float leftSpace = slotBounds.min.x - bounds.xMin - 10f;

        bool placeRight = rightSpace >= panelWidth;

        if (!placeRight && leftSpace < panelWidth)
            placeRight = rightSpace >= leftSpace;

        Vector2 pivot = placeRight ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f);

        rect.pivot = pivot;

        float x = placeRight ? slotBounds.max.x + 10f : slotBounds.min.x - 10f;
        float y = slotBounds.center.y;

        if (panelHeight <= bounds.height)
            y = Mathf.Clamp(y, bounds.yMin + panelHeight * 0.5f, bounds.yMax - panelHeight * 0.5f);
        else
            y = bounds.center.y;

        x = Mathf.Clamp(x, bounds.xMin + panelWidth * pivot.x, bounds.xMax - panelWidth * (1f -pivot.x));

        rect.position = inventoryRect.TransformPoint(new Vector3(x, y, 0f));
    }

    public void ShowItemInfo(ItemDefine item, RectTransform slotRect)
    {
        itemPanel.SetActive(true);

        SetPanelPos(slotRect);

        icon.sprite = item.Icon;
        itemNameText.text = item.DisplayName;
        itemRarityText.text = item.Rarity.ToString();
        itemCategoryText.text = item.Category.ToString();
        itemDescriptionText.text = item.Description;
        itemWeightText.text = $"Weight : {item.UnitWeight.ToString()}";
    }

    public void HideItemInfo()
    {
        itemPanel.SetActive(false);

        icon.sprite = null;
        itemNameText.text = string.Empty;
        itemRarityText.text = string.Empty;
        itemCategoryText.text = string.Empty;
        itemDescriptionText.text = string.Empty;
        itemWeightText.text = string.Empty;
    }
}
