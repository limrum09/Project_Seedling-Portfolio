using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Main 또는 Reserve 무기 슬롯의 표시와 선택 입력을 담당
/// </summary>
public sealed class WeaponLoadoutSlotView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private WeaponSlot slot;
    [SerializeField]
    private Image weaponIcon;
    [SerializeField]
    private TextMeshProUGUI weaponNameText;
    [SerializeField]
    private GameObject selectArea;
    [SerializeField]
    private Button selectAreaButton;

    private HoldItemDefine currentWeapon;
    private bool selectionMode;
    private bool interactionEnabled = true;

    public event Action<WeaponSlot> OnSelected;
    public event Action<WeaponSlot> OnRemoveRequested;

    private void Awake()
    {
        selectAreaButton.onClick.AddListener(HandleSelected);
        UpdateSelectionState();
    }

    private void OnDestroy()
    {
        selectAreaButton.onClick.RemoveListener(HandleSelected);
    }

    /// <summary>
    /// 현재 슬롯 선택 이벤트 전달
    /// </summary>
    private void HandleSelected()
    {
        if (!selectionMode || !interactionEnabled)
            return;

        OnSelected?.Invoke(slot);
    }

    /// <summary>
    /// SelectArea 활성 상태와 Button 입력 상태 갱신
    /// </summary>
    private void UpdateSelectionState()
    {
        selectArea.SetActive(selectionMode);
        selectAreaButton.interactable = selectionMode && interactionEnabled;
    }

    /// <summary>
    /// 현재 슬롯에 장착된 무기 표시
    /// </summary>
    /// <param name="weapon">장착된 무기 또는 비어 있으면 null</param>
    public void SetWeapon(HoldItemDefine weapon)
    {
        currentWeapon = weapon;

        if (weapon == null)
        {
            weaponIcon.sprite = null;
            weaponIcon.enabled = false;
            weaponNameText.text = string.Empty;

            return;
        }

        weaponIcon.sprite = weapon.Icon;
        weaponIcon.enabled = weapon.Icon != null;
        weaponNameText.text = weapon.DisplayName;
    }

    /// <summary>
    /// 장착 위치 선택 모드 설정
    /// </summary>
    /// <param name="isEnabled">선택 모드 활성 여부</param>
    public void SetSelectionMode(bool isEnabled)
    {
        selectionMode = isEnabled;
        UpdateSelectionState();
    }

    /// <summary>
    /// 장착 요청 처리 중 슬롯 입력 가능 여부 설정
    /// </summary>
    /// <param name="isEnabled">입력 가능 여부</param>
    public void SetInteractionEnabled(bool isEnabled)
    {
        interactionEnabled = isEnabled;
        UpdateSelectionState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        if (eventData.clickCount != 2)
            return;

        if (!interactionEnabled || currentWeapon == null)
            return;

        OnRemoveRequested?.Invoke(slot);
    }
}