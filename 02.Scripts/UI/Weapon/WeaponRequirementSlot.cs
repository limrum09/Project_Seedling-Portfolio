using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 무기 해금에 필요한 재료 하나의 현재 수량과 필요 수량을 표시
/// </summary>
public sealed class WeaponRequirementSlot : MonoBehaviour
{
    [SerializeField]
    private Image icon;
    [SerializeField]
    private TextMeshProUGUI currentAmountText;
    [SerializeField]
    private TextMeshProUGUI needAmountText;
    [SerializeField]
    private GameObject enoughUI;
    [SerializeField]
    private GameObject notEnoughUI;

    /// <summary>
    /// 재료 요구 상태 표시
    /// </summary>
    /// <param name="data">표시할 재료 요구 상태</param>
    public void SetRequirement(WeaponRequirementViewData data)
    {
        if (data.Item == null)
            throw new ArgumentException("Weapon Requirement에 Item이 없음", nameof(data));

        icon.sprite = data.Item.Icon;
        icon.enabled = data.Item.Icon != null;

        currentAmountText.text = data.CurrentAmount.ToString();
        needAmountText.text = data.RequiredAmount.ToString();

        enoughUI.SetActive(data.HasEnough);
        notEnoughUI.SetActive(!data.HasEnough);
    }

    /// <summary>
    /// 현재 재료 표시 제거
    /// </summary>
    public void Clear()
    {
        icon.sprite = null;
        icon.enabled = false;

        currentAmountText.text = string.Empty;
        needAmountText.text = string.Empty;

        enoughUI.SetActive(false);
        notEnoughUI.SetActive(false);
    }
}