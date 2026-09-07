using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 무기 Category Toggle 하나와 Category 값을 연결
/// </summary>
[Serializable]
public sealed class WeaponCategoryToggleBind
{
    [SerializeField]
    private WeaponCategory category;
    [SerializeField]
    private UIToggleView toggleView;

    public WeaponCategory Category => category;
    public UIToggleView Toggle => toggleView;

    /// <summary>
    /// Toggle을 Category 선택 처리자와 연결
    /// </summary>
    /// <param name="handler">Category 선택 처리자</param>
    public void Bind(IUISelectionHandler<WeaponCategory> handler)
    {
        toggleView.Bind(category, handler);
    }

    /// <summary>
    /// Toggle의 Category 선택 연결 해제
    /// </summary>
    public void Unbind()
    {
        toggleView.Unbind();
    }
}

/// <summary>
/// 무기 목록과 상세 정보 및 Loadout 슬롯 View를 관리
/// </summary>
public sealed class WeaponPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField]
    private CanvasGroup group;
    [SerializeField]
    private SelectWeaponInfoPanel infoPanel;

    [Header("Category")]
    [SerializeField]
    private List<WeaponCategoryToggleBind> categoryToggleBinds = new List<WeaponCategoryToggleBind>();

    [Header("Weapon List")]
    [SerializeField]
    private Transform weaponContentRoot;
    [SerializeField]
    private WeaponContentView weaponContentPrefab;

    [Header("Loadout")]
    [SerializeField]
    private WeaponLoadoutSlotView mainWeaponView;
    [SerializeField]
    private WeaponLoadoutSlotView reserveWeaponView;

    [Header("Tweens")]
    [SerializeField]
    private UITweenSequencePlayer showTween;
    [SerializeField]
    private UITweenSequencePlayer hideTween;

    private readonly List<WeaponContentView> contentViews = new List<WeaponContentView>();

    public event Action<WeaponUnlockDefine> OnWeaponSelected;
    public event Action<WeaponUnlockDefine> OnUnlockRequested;
    public event Action<WeaponUnlockDefine> OnSelectRequested;
    public event Action<WeaponSlot> OnLoadoutSlotSelected;
    public event Action<WeaponSlot> OnLoadoutSlotRemoveRequested;

    private void Awake()
    {
        infoPanel.OnUnlockRequested += HandleUnlockRequested;
        infoPanel.OnSelectRequested += HandleSelectRequested;

        mainWeaponView.OnSelected += HandleLoadoutSlotSelected;
        mainWeaponView.OnRemoveRequested += HandleLoadoutRemoveRequested;

        reserveWeaponView.OnSelected += HandleLoadoutSlotSelected;
        reserveWeaponView.OnRemoveRequested += HandleLoadoutRemoveRequested;

        SetLoadoutSelectionMode(false);
        Hide();
    }

    private void OnDestroy()
    {
        infoPanel.OnUnlockRequested -= HandleUnlockRequested;
        infoPanel.OnSelectRequested -= HandleSelectRequested;

        mainWeaponView.OnSelected -= HandleLoadoutSlotSelected;
        mainWeaponView.OnRemoveRequested -= HandleLoadoutRemoveRequested;

        reserveWeaponView.OnSelected -= HandleLoadoutSlotSelected;
        reserveWeaponView.OnRemoveRequested -= HandleLoadoutRemoveRequested;

        for (int i = 0; i < contentViews.Count; i++)
        {
            if (contentViews[i] != null)
                contentViews[i].OnClicked -= HandleWeaponSelected;
        }
    }

    /// <summary>
    /// 부족한 Weapon Content 수만큼 View 생성
    /// </summary>
    /// <param name="requiredCount">필요한 Content 수</param>
    private void EnsureContentViewCount(int requiredCount)
    {
        while (contentViews.Count < requiredCount)
        {
            WeaponContentView view = Instantiate(weaponContentPrefab, weaponContentRoot);

            view.OnClicked += HandleWeaponSelected;
            contentViews.Add(view);
        }
    }

    /// <summary>
    /// Weapon Content 클릭 이벤트 전달
    /// </summary>
    /// <param name="definition">클릭한 무기 해금 정의</param>
    private void HandleWeaponSelected(WeaponUnlockDefine definition)
    {
        OnWeaponSelected?.Invoke(definition);
    }

    /// <summary>
    /// 무기 해금 Button 요청 전달
    /// </summary>
    /// <param name="definition">해금할 무기 정의</param>
    private void HandleUnlockRequested(WeaponUnlockDefine definition)
    {
        OnUnlockRequested?.Invoke(definition);
    }

    /// <summary>
    /// 장착 위치 선택 Button 요청 전달
    /// </summary>
    /// <param name="definition">장착할 무기 정의</param>
    private void HandleSelectRequested(WeaponUnlockDefine definition)
    {
        OnSelectRequested?.Invoke(definition);
    }

    /// <summary>
    /// Main 또는 Reserve 슬롯 선택 이벤트 전달
    /// </summary>
    /// <param name="slot">선택한 Loadout 슬롯</param>
    private void HandleLoadoutSlotSelected(WeaponSlot slot)
    {
        OnLoadoutSlotSelected?.Invoke(slot);
    }

    private void HandleLoadoutRemoveRequested(WeaponSlot slot)
    {
        OnLoadoutSlotRemoveRequested?.Invoke(slot);
    }

    /// <summary>
    /// Category Toggle을 선택 처리자와 연결
    /// </summary>
    /// <param name="handler">Weapon Category 선택 처리자</param>
    public void BindCategoryToggles(IUISelectionHandler<WeaponCategory> handler)
    {
        for (int i = 0; i < categoryToggleBinds.Count; i++)
        {
            categoryToggleBinds[i].Bind(handler);
        }
    }

    /// <summary>
    /// Category Toggle 연결 해제
    /// </summary>
    public void UnbindCategoryToggles()
    {
        for (int i = 0; i < categoryToggleBinds.Count; i++)
        {
            categoryToggleBinds[i].Unbind();
        }
    }

    /// <summary>
    /// All Category Toggle을 선택 상태로 초기화
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void ResetCategoryAll()
    {
        for(int i = 0; i < categoryToggleBinds.Count; i++)
        {
            if(categoryToggleBinds[i].Category == WeaponCategory.All)
            {
                categoryToggleBinds[i].Toggle.Select();

                return;
            }
        }

        throw new InvalidOperationException("Weapon Category Toggle에 All Toggle이 없음");
    }

    /// <summary>
    /// 전체 무기 목록을 생성하거나 현재 상태로 갱신
    /// </summary>
    /// <param name="viewData">전체 무기 표시 데이터</param>
    /// <param name="category">현재 선택된 Category</param>
    /// <param name="selectedDefinition">현재 InfoPanel에서 선택한 무기</param>
    public void SetWeaponList(IReadOnlyList<WeaponUnlockViewData> viewData, WeaponCategory category, WeaponUnlockDefine selectedDefinition)
    {
        if (viewData == null)
            throw new ArgumentNullException(nameof(viewData));

        EnsureContentViewCount(viewData.Count);

        for (int i = 0; i < contentViews.Count; i++)
        {
            WeaponContentView view = contentViews[i];

            if (i >= viewData.Count)
            {
                view.Clear();
                continue;
            }

            WeaponUnlockViewData data = viewData[i];
            HoldItemDefine weapon = data.Define.Weapon;

            bool isVisible = category == WeaponCategory.All || weapon.WeaponCategory == category;

            view.SetViewData(data);
            view.SetSelected(data.Define == selectedDefinition);
            view.SetVisible(isVisible);
        }
    }

    /// <summary>
    /// Main과 Reserve 무기 표시 갱신
    /// </summary>
    /// <param name="data">현재 Loadout 표시 데이터</param>
    public void SetLoadout(WeaponLoadoutViewData data)
    {
        mainWeaponView.SetWeapon(data.MainWeapon);
        reserveWeaponView.SetWeapon(data.ReserveWeapon);
    }

    /// <summary>
    /// 선택한 무기의 상세 정보 표시
    /// </summary>
    /// <param name="data">선택한 무기 표시 데이터</param>
    /// <param name="canSelect">현재 Station에서 장착 가능 여부</param>
    public void ShowWeaponInfo(WeaponUnlockViewData data)
    {
        infoPanel.Show(data);
    }

    /// <summary>
    /// 무기 상세 정보 숨김
    /// </summary>
    public void HideWeaponInfo()
    {
        infoPanel.Hide();
    }

    /// <summary>
    /// Main과 Reserve 장착 위치 선택 모드 설정
    /// </summary>
    /// <param name="isEnabled">선택 모드 활성 여부</param>
    public void SetLoadoutSelectionMode(bool isEnabled)
    {
        mainWeaponView.SetSelectionMode(isEnabled);
        reserveWeaponView.SetSelectionMode(isEnabled);
    }

    /// <summary>
    /// Loadout 요청 처리 중 슬롯 입력 상태 설정
    /// </summary>
    /// <param name="isPending">요청 처리 중 여부</param>
    public void SetLoadoutPending(bool isPending)
    {
        mainWeaponView.SetInteractionEnabled(!isPending);
        reserveWeaponView.SetInteractionEnabled(!isPending);

        infoPanel.SetSelectPending(isPending);
    }

    /// <summary>
    /// 해금 요청 처리 중 Button 입력 상태 설정
    /// </summary>
    /// <param name="isPending">요청 처리 중 여부</param>
    public void SetUnlockPending(bool isPending)
    {
        infoPanel.SetUnlockPending(isPending);
    }

    /// <summary>
    /// Weapon Panel 표시
    /// </summary>
    public void Show()
    {
        showTween.Play();
    }

    public void TweenShow()
    {
        ResetCategoryAll();

        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    /// <summary>
    /// Weapon Panel 숨김
    /// </summary>
    public void Hide()
    {
        hideTween.Play();
    }

    public void TweenHide()
    {
        SetLoadoutSelectionMode(false);
        SetLoadoutPending(false);
        SetUnlockPending(false);
        HideWeaponInfo();

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }
}