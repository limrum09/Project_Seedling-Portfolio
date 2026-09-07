using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildingInfoPanel : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup group;
    [SerializeField]
    private RectTransform panelRect;

    [Header("Viewer")]
    [SerializeField]
    private Image icon;
    [SerializeField]
    private TextMeshProUGUI buildingNameText;
    [SerializeField]
    private TextMeshProUGUI buildingCategoryText;
    [SerializeField]
    private TextMeshProUGUI buildingMaterialsText;
    [SerializeField]
    private TextMeshProUGUI buildingDesText;

    [Header("Color")]
    [SerializeField]
    private Color enoughTextColor;
    [SerializeField]
    private Color notEnoughTextColor;

    private void Awake()
    {
        group.blocksRaycasts = false;
        group.interactable = false;
        Hide();
    }

    private string CreateMaterialText(IReadOnlyList<ItemRequirementViewData> materials)
    {
        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < materials.Count; i++)
        {
            ItemRequirementViewData material = materials[i];

            Color color = material.HasEnough ? enoughTextColor : notEnoughTextColor;

            string colorCode = ColorUtility.ToHtmlStringRGB(color);

            builder.Append($"{material.DisplayName} - <color=#{colorCode}> {material.InventoryAmount} / {material.RequiredAmount}</color>");

            if (i < materials.Count - 1)
                builder.AppendLine();
        }

        return builder.ToString();
    }

    public void Show(BuildingDefine define, IReadOnlyList<ItemRequirementViewData> materials, RectTransform rect)
    {
        group.alpha = 1f;

        icon.sprite = define.Icon;
        buildingNameText.text = define.DisplayName;
        buildingCategoryText.text = define.Category.ToString();
        buildingMaterialsText.text = CreateMaterialText(materials);
        buildingDesText.text = string.Empty;

        Vector3 slotCenter = rect.TransformPoint(rect.rect.center);

        RectTransform panelParent = (RectTransform)panelRect.parent;
        Vector3 slotLocalPos = panelParent.InverseTransformPoint(slotCenter);

        Vector3 panelLocalPos = panelRect.localPosition;
        panelLocalPos.x = slotLocalPos.x;
        panelRect.localPosition = panelLocalPos;
    }

    public void Hide()
    {
        group.alpha = 0f;

        icon.sprite = null;
        buildingNameText.text = string.Empty;
        buildingCategoryText.text = string.Empty;
        buildingMaterialsText.text = string.Empty;
        buildingDesText.text = string.Empty;
    }
}
