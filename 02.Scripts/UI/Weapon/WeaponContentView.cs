using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 무기 목록에서 무기 하나의 표시와 클릭 입력을 담당
/// </summary>
public sealed class WeaponContentView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private Image icon;
    [SerializeField]
    private TextMeshProUGUI weaponNameText;
    [SerializeField]
    private GameObject lockState;
    [SerializeField]
    private GameObject selectFrame;

    private WeaponUnlockDefine define;

    public event Action<WeaponUnlockDefine> OnClicked;

    /// <summary>
    /// 외부 이벤트 연결 제거
    /// </summary>
    private void OnDestroy()
    {
        OnClicked = null;
    }

    /// <summary>
    /// 현재 표시 중인 무기의 클릭 이벤트 전달
    /// </summary>
    /// <param name="eventData">Pointer 클릭 정보</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (define == null)
            throw new InvalidOperationException("Weapon Content에 표시 데이터가 설정되지 않음");

        OnClicked?.Invoke(define);
    }

    /// <summary>
    /// 무기 목록 표시 데이터 적용
    /// </summary>
    /// <param name="data">적용할 무기 해금 표시 데이터</param>
    public void SetViewData(WeaponUnlockViewData data)
    {
        if (data.Define == null || data.Define.Weapon == null)
            throw new ArgumentException("Weapon Content에 유효하지 않은 무기 데이터가 전달됨", nameof(data));

        define = data.Define;

        HoldItemDefine weapon = define.Weapon;

        icon.sprite = weapon.Icon;
        icon.enabled = weapon.Icon != null;

        weaponNameText.text = weapon.DisplayName;

        lockState.SetActive(!data.IsUnlocked);
    }

    /// <summary>
    /// InfoPanel에 선택된 무기 표시 여부 설정
    /// </summary>
    /// <param name="isSelected">현재 선택된 무기 여부</param>
    public void SetSelected(bool isSelected)
    {
        selectFrame.SetActive(isSelected);
    }

    /// <summary>
    /// Category 필터에 따른 표시 여부 설정
    /// </summary>
    /// <param name="isVisible">표시 여부</param>
    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    /// <summary>
    /// 현재 표시 데이터 제거
    /// </summary>
    public void Clear()
    {
        define = null;

        icon.sprite = null;
        icon.enabled = false;
        weaponNameText.text = string.Empty;

        lockState.SetActive(false);
        selectFrame.SetActive(false);

        gameObject.SetActive(false);
    }
}