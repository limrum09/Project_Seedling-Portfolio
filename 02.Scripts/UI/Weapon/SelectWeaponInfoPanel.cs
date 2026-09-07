using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 선택한 무기의 상세 정보와 해금 또는 장착 요청 Button을 표시
/// </summary>
public sealed class SelectWeaponInfoPanel : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField]
    private Image weaponIcon;
    [SerializeField]
    private TextMeshProUGUI weaponNameText;
    [SerializeField]
    private TextMeshProUGUI descriptionText;

    [Header("Stat")]
    [SerializeField]
    private TextMeshProUGUI damageText;
    [SerializeField]
    private TextMeshProUGUI attackSpeedText;
    [SerializeField]
    private TextMeshProUGUI attackRangeText;
    [SerializeField]
    private TextMeshProUGUI attackTypeText;
    [SerializeField]
    private TextMeshProUGUI energyCostText;

    [Header("Requirement")]
    [SerializeField]
    private GameObject requirementArea;
    [SerializeField]
    private Transform requirementContent;
    [SerializeField]
    private WeaponRequirementSlot requirementSlotPrefab;
    [SerializeField]
    private List<WeaponRequirementSlot> requirementSlots = new List<WeaponRequirementSlot>();

    [Header("Button")]
    [SerializeField]
    private Button unlockButton;
    [SerializeField]
    private Button selectButton;

    private WeaponUnlockDefine currentDefinition;
    private bool currentUnlocked;
    private bool currentCanUnlock;
    private bool unlockPending;
    private bool selectPending;

    public event Action<WeaponUnlockDefine> OnUnlockRequested;
    public event Action<WeaponUnlockDefine> OnSelectRequested;

    private void Awake()
    {
        unlockButton.onClick.AddListener(HandleUnlockRequested);
        selectButton.onClick.AddListener(HandleSelectRequested);
    }

    private void OnDestroy()
    {
        unlockButton.onClick.RemoveListener(HandleUnlockRequested);
        selectButton.onClick.RemoveListener(HandleSelectRequested);
    }

    /// <summary>
    /// 부족한 Requirement Slot 수만큼 View 생성
    /// </summary>
    /// <param name="requiredCount">필요한 Slot 수</param>
    private void EnsureRequirementSlotCount(int requiredCount)
    {
        while (requirementSlots.Count < requiredCount)
        {
            WeaponRequirementSlot slot = Instantiate(requirementSlotPrefab, requirementContent);

            requirementSlots.Add(slot);
        }
    }

    /// <summary>
    /// 모든 Requirement Slot을 초기화하고 숨김
    /// </summary>
    private void ClearRequirementSlots()
    {
        for (int i = 0; i < requirementSlots.Count; i++)
        {
            requirementSlots[i].Clear();
            requirementSlots[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 현재 해금 상태와 요청 처리 상태에 맞게 Button 표시 갱신
    /// </summary>
    private void UpdateButtonState()
    {
        bool hasData = currentDefinition != null;

        unlockButton.gameObject.SetActive(hasData && !currentUnlocked);
        unlockButton.interactable = hasData && !currentUnlocked && currentCanUnlock && !unlockPending;

        selectButton.gameObject.SetActive(hasData && currentUnlocked);
        selectButton.interactable = hasData && currentUnlocked && !selectPending;
    }

    /// <summary>
    /// 현재 선택된 무기의 해금 요청 전달
    /// </summary>
    private void HandleUnlockRequested()
    {
        if (currentDefinition == null)
            throw new InvalidOperationException("해금할 무기가 선택되지 않음");

        OnUnlockRequested?.Invoke(currentDefinition);
    }

    /// <summary>
    /// 현재 선택된 무기의 장착 위치 선택 요청 전달
    /// </summary>
    private void HandleSelectRequested()
    {
        if (currentDefinition == null)
            throw new InvalidOperationException("장착할 무기가 선택되지 않음");

        OnSelectRequested?.Invoke(currentDefinition);
    }

    /// <summary>
    /// 선택한 무기의 상세 정보 표시
    /// </summary>
    /// <param name="data">선택한 무기 표시 데이터</param>
    /// <param name="canSelect">현재 Station에서 장착 위치를 선택할 수 있는지 여부</param>
    public void Show(WeaponUnlockViewData data)
    {
        if (data.Define == null || data.Define.Weapon == null)
            throw new ArgumentException("유효하지 않은 Weapon Info 데이터", nameof(data));

        currentDefinition = data.Define;
        currentUnlocked = data.IsUnlocked;
        currentCanUnlock = data.CanUnlock;

        HoldItemDefine weapon = data.Define.Weapon;

        weaponIcon.sprite = weapon.Icon;
        weaponIcon.enabled = weapon.Icon != null;

        weaponNameText.text = weapon.DisplayName;
        descriptionText.text = weapon.Description;

        damageText.text = weapon.ItemDamage.ToString("0.##");
        attackSpeedText.text = weapon.AttackCooldown.ToString("0.##");
        attackRangeText.text = weapon.AttackRange.ToString("0.##");
        energyCostText.text = weapon.EnergyCost.ToString("0.##");

        switch (weapon.HoldCategory)
        {
            case HoldItemCategory.None:
                attackTypeText.text = "오류, 타입 입력 필요";
                break;
            case HoldItemCategory.Combat:
                attackTypeText.text = "일반";
                break;
            case HoldItemCategory.Mining:
                attackTypeText.text = "광물";
                break;
            case HoldItemCategory.Combat | HoldItemCategory.Mining:
                attackTypeText.text = "일반 + 광물";
                break;
        }

        ClearRequirementSlots();

        bool showRequirements = !data.IsUnlocked;

        requirementArea.SetActive(data.Requirements.Count > 0);

        if (showRequirements)
        {
            EnsureRequirementSlotCount(data.Requirements.Count);

            for (int i = 0; i < data.Requirements.Count; i++)
            {
                WeaponRequirementSlot slot = requirementSlots[i];

                slot.gameObject.SetActive(true);
                slot.SetRequirement(data.Requirements[i]);
            }
        }

        gameObject.SetActive(true);
        UpdateButtonState();
    }

    /// <summary>
    /// 무기 상세 정보 숨김
    /// </summary>
    public void Hide()
    {
        currentDefinition = null;
        currentUnlocked = false;
        currentCanUnlock = false;
        unlockPending = false;
        selectPending = false;

        ClearRequirementSlots();
        requirementArea.SetActive(false);

        unlockButton.gameObject.SetActive(false);
        selectButton.gameObject.SetActive(false);

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 해금 요청 처리 중 상태 설정
    /// </summary>
    /// <param name="isPending">요청 처리 중 여부</param>
    public void SetUnlockPending(bool isPending)
    {
        unlockPending = isPending;
        UpdateButtonState();
    }

    /// <summary>
    /// 장착 요청 처리 중 상태 설정
    /// </summary>
    /// <param name="isPending">요청 처리 중 여부</param>
    public void SetSelectPending(bool isPending)
    {
        selectPending = isPending;
        UpdateButtonState();
    }
}