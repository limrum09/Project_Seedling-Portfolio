using System;
using System.Collections.Generic;

/// <summary>
/// 무기 해금 재료 하나의 현재 보유량과 필요 수량
/// </summary>
public readonly struct WeaponRequirementViewData
{
    public ItemDefine Item { get; }
    public int CurrentAmount { get; }
    public int RequiredAmount { get; }
    public bool HasEnough => CurrentAmount >= RequiredAmount;

    public WeaponRequirementViewData(ItemDefine item, int currentAmount, int requiredAmount)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        if (currentAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(currentAmount));

        if (requiredAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(requiredAmount));

        Item = item;
        CurrentAmount = currentAmount;
        RequiredAmount = requiredAmount;
    }
}

/// <summary>
/// 무기 정의와 플레이어별 해금 상태를 함께 전달
/// </summary>
public readonly struct WeaponUnlockViewData
{
    private readonly IReadOnlyList<WeaponRequirementViewData> requirements;

    public WeaponUnlockDefine Define { get; }
    public bool IsUnlocked { get; }
    public bool CanUnlock { get; }
    public IReadOnlyList<WeaponRequirementViewData> Requirements => requirements;

    public WeaponUnlockViewData(WeaponUnlockDefine define, bool isUnlocked, bool canUnlock, IReadOnlyList<WeaponRequirementViewData> getRequirements)
    {
        if (define == null)
            throw new ArgumentNullException(nameof(define));

        if (getRequirements == null)
            throw new ArgumentNullException(nameof(getRequirements));

        WeaponRequirementViewData[] copiedRequirements = new WeaponRequirementViewData[getRequirements.Count];

        for (int i = 0; i < getRequirements.Count; i++)
        {
            copiedRequirements[i] = getRequirements[i];
        }

        Define = define;
        IsUnlocked = isUnlocked;
        CanUnlock = canUnlock;
        requirements = Array.AsReadOnly(copiedRequirements);
    }
}

/// <summary>
/// Main과 Reserve 무기 및 해금된 무기 목록
/// </summary>
public readonly struct WeaponLoadoutViewData
{
    private readonly IReadOnlyList<HoldItemDefine> unlockedWeapons;

    public HoldItemDefine MainWeapon { get; }
    public HoldItemDefine ReserveWeapon { get; }
    public IReadOnlyList<HoldItemDefine> UnlockedWeapons => unlockedWeapons;

    public WeaponLoadoutViewData(HoldItemDefine mainWeapon, HoldItemDefine reserveWeapon, IReadOnlyList<HoldItemDefine> getUnlockedWeapons)
    {
        if (getUnlockedWeapons == null)
            throw new ArgumentNullException(nameof(getUnlockedWeapons));

        HoldItemDefine[] copiedWeapons = new HoldItemDefine[getUnlockedWeapons.Count];

        for (int i = 0; i < getUnlockedWeapons.Count; i++)
        {
            copiedWeapons[i] = getUnlockedWeapons[i];
        }

        MainWeapon = mainWeapon;
        ReserveWeapon = reserveWeapon;
        unlockedWeapons = Array.AsReadOnly(copiedWeapons);
    }
}

/// <summary>
/// 무기 Replica와 Inventory 상태를 Weapon Panel UI 및 요청으로 연결
/// </summary>
public sealed class WeaponPresenter : IUISelectionHandler<WeaponCategory>
{
    private readonly WeaponCatalog catalog;
    private readonly IPlayerWeaponReadAccess weaponReadAccess;
    private readonly IPlayerWeaponCommandGateway weaponGateway;
    private readonly IInventoryReadAccess inventoryReadAccess;
    private readonly WeaponPanel weaponPanel;

    private WeaponUnlockDefine selectedDefinition;
    private WeaponCategory selectedCategory = WeaponCategory.All;

    private int pendingUnlockRequestId;
    private int pendingSetLoadoutRequestId;
    private bool isEnabled;

    /// <summary>
    /// Weapon Presenter 생성
    /// </summary>
    /// <param name="getcatalog">전체 무기 Catalog</param>
    /// <param name="getWeaponReadAccess">무기 Replica 읽기 접근점</param>
    /// <param name="getWeaponGateway">무기 요청 Gateway</param>
    /// <param name="getInventoryReadAccess">Inventory Replica 읽기 접근점</param>
    /// <param name="panel">표시할 Weapon Panel</param>
    public WeaponPresenter(WeaponCatalog getCatalog, IPlayerWeaponReadAccess getWeaponReadAccess, IPlayerWeaponCommandGateway getWeaponGateway, IInventoryReadAccess getInventoryReadAccess, WeaponPanel panel)
    {
        catalog = getCatalog;

        weaponReadAccess = getWeaponReadAccess;

        weaponGateway = getWeaponGateway;

        inventoryReadAccess = getInventoryReadAccess;

        weaponPanel = panel;
    }

    /// <summary>
    /// 무기 해금 요구 재료를 Weapon UI 데이터로 변환
    /// </summary>
    /// <param name="requirements">무기 해금 요구 재료</param>
    /// <param name="inventoryAmounts">현재 Inventory 보유량 조회</param>
    /// <returns>재료별 현재 보유량과 필요 수량</returns>
    private IReadOnlyList<WeaponRequirementViewData> CreateRequirementViewData(IReadOnlyList<ItemRequirement> requirements, InventoryAmountLookup inventoryAmounts)
    {
        IReadOnlyList<ItemRequirementState> states = inventoryAmounts.CreateRequirementStates(requirements);

        WeaponRequirementViewData[] viewData = new WeaponRequirementViewData[states.Count];

        for (int i = 0; i < states.Count; i++)
        {
            ItemRequirementState state = states[i];

            viewData[i] = new WeaponRequirementViewData(state.Item, state.InventoryAmount, state.RequiredAmount);
        }

        return Array.AsReadOnly(viewData);
    }

    /// <summary>
    /// Weapon ID에 해당하는 무기 정의 반환
    /// </summary>
    /// <param name="weaponId">검색할 Weapon ID</param>
    /// <returns>검색된 HoldItemDefine</returns>
    private HoldItemDefine ResolveWeapon(string weaponId)
    {
        if (!catalog.TryGetUnlockDefine(weaponId, out WeaponUnlockDefine definition))
            throw new InvalidOperationException($"Weapon Catalog에서 ID를 찾을 수 없음: {weaponId}");

        return definition.Weapon;
    }

    /// <summary>
    /// 지정한 Category에 표시되는 첫 번째 무기 정의를 반환
    /// </summary>
    /// <param name="category">검색할 Weapon Category</param>
    /// <returns>첫 번째 무기 정의 또는 무기가 없으면 null</returns>
    private WeaponUnlockDefine FindFirstDefinition(WeaponCategory category)
    {
        for (int i = 0; i < catalog.Definitions.Count; i++)
        {
            WeaponUnlockDefine definition = catalog.Definitions[i];

            if (definition == null || definition.Weapon == null)
                throw new InvalidOperationException($"Weapon Catalog의 {i}번 설정이 올바르지 않음");

            if (category != WeaponCategory.All && definition.Weapon.WeaponCategory != category)
                continue;

            return definition;
        }

        return null;
    }

    /// <summary>
    /// 선택된 무기의 현재 UI 데이터 검색
    /// </summary>
    /// <param name="viewData">전체 무기 UI 데이터</param>
    /// <param name="definition">검색할 무기 정의</param>
    /// <param name="selectedData">검색된 UI 데이터</param>
    /// <returns>UI 데이터를 찾았으면 true</returns>
    private bool TryFindViewData(IReadOnlyList<WeaponUnlockViewData> viewData, WeaponUnlockDefine definition, out WeaponUnlockViewData selectedData)
    {
        for (int i = 0; i < viewData.Count; i++)
        {
            if (viewData[i].Define != definition)
                continue;

            selectedData = viewData[i];
            return true;
        }

        selectedData = default;
        return false;
    }

    /// <summary>
    /// 전체 무기 목록과 선택한 InfoPanel 갱신
    /// </summary>
    private void RefreshUnlockViews()
    {
        IReadOnlyList<WeaponUnlockViewData> viewData = GetUnlockViewData();

        weaponPanel.SetWeaponList(viewData, selectedCategory, selectedDefinition);

        if (selectedDefinition == null)
        {
            weaponPanel.HideWeaponInfo();
            return;
        }

        if (!TryFindViewData(viewData, selectedDefinition, out WeaponUnlockViewData selectedData))
            throw new InvalidOperationException("선택한 무기가 Weapon Catalog에 없음");

        weaponPanel.ShowWeaponInfo(selectedData);
    }

    /// <summary>
    /// Main과 Reserve Loadout UI 갱신
    /// </summary>
    private void RefreshLoadoutView()
    {
        weaponPanel.SetLoadout(GetLoadoutViewData());
    }

    /// <summary>
    /// 무기 목록과 Loadout 전체 UI 갱신
    /// </summary>
    private void RefreshAll()
    {
        RefreshUnlockViews();
        RefreshLoadoutView();
    }

    /// <summary>
    /// Weapon Content 선택을 InfoPanel에 반영
    /// </summary>
    /// <param name="definition">선택한 무기 정의</param>
    private void HandleWeaponSelected(WeaponUnlockDefine definition)
    {
        selectedDefinition = definition;

        weaponPanel.SetLoadoutSelectionMode(false);

        RefreshUnlockViews();
    }

    /// <summary>
    /// 선택한 무기의 해금을 Gateway에 요청
    /// </summary>
    /// <param name="definition">해금할 무기 정의</param>
    private void HandleUnlockRequested(WeaponUnlockDefine definition)
    {
        if (pendingUnlockRequestId != 0)
            return;

        selectedDefinition = definition;

        pendingUnlockRequestId = weaponGateway.RequestUnlockWeapon(definition.Weapon.ItemUID);

        weaponPanel.SetUnlockPending(true);
    }

    /// <summary>
    /// 해금된 무기의 장착 위치 선택 모드 시작
    /// </summary>
    /// <param name="definition">장착할 무기 정의</param>
    private void HandleSelectRequested(WeaponUnlockDefine definition)
    {
        if (!weaponReadAccess.IsWeaponUnlocked(definition.Weapon.ItemUID))
            throw new InvalidOperationException("해금되지 않은 무기의 장착 위치 선택이 요청됨");

        selectedDefinition = definition;

        weaponPanel.SetLoadoutSelectionMode(true);
    }

    /// <summary>
    /// 선택한 Loadout 슬롯으로 현재 무기 배치 요청
    /// </summary>
    /// <param name="slot">선택한 Loadout 슬롯</param>
    private void HandleLoadoutSlotSelected(WeaponSlot slot)
    {
        if (pendingSetLoadoutRequestId != 0)
            return;

        if (selectedDefinition == null)
            throw new InvalidOperationException("장착할 무기가 선택되지 않음");

        pendingSetLoadoutRequestId = weaponGateway.RequestSetLoadout(slot, selectedDefinition.Weapon.ItemUID);

        weaponPanel.SetLoadoutPending(true);
    }

    private void HandleLoadoutslotRemoveRequested(WeaponSlot slot)
    {
        if (pendingSetLoadoutRequestId != 0)
            return;

        pendingSetLoadoutRequestId = weaponGateway.RequestClearLoadout(slot);

        weaponPanel.SetLoadoutPending(true);
    }

    /// <summary>
    /// 무기 Replica 변경을 전체 UI에 반영
    /// </summary>
    /// <param name="snapshot">변경된 무기 상태</param>
    private void HandleWeaponStateChanged(PlayerWeaponSnapshot snapshot)
    {
        RefreshAll();
    }

    /// <summary>
    /// Inventory 변경을 해금 가능 상태에 반영
    /// </summary>
    /// <param name="changeSet">변경된 Inventory 상태</param>
    private void HandleInventoryChanged(InventoryChangeSet changeSet)
    {
        RefreshUnlockViews();
    }

    /// <summary>
    /// 해금 요청 완료 후 Button 상태 복구
    /// </summary>
    /// <param name="requestId">완료된 요청 식별자</param>
    /// <param name="result">해금 처리 결과</param>
    private void HandleUnlockCompleted(int requestId, PlayerWeaponOperationResult result)
    {
        if (requestId != pendingUnlockRequestId)
            return;

        pendingUnlockRequestId = 0;

        weaponPanel.SetUnlockPending(false);
        RefreshUnlockViews();
    }

    /// <summary>
    /// Loadout 요청 완료 후 선택 상태 갱신
    /// </summary>
    /// <param name="requestId">완료된 요청 식별자</param>
    /// <param name="result">Loadout 처리 결과</param>
    private void HandleSetLoadoutCompleted(int requestId, PlayerWeaponOperationResult result)
    {
        if (requestId != pendingSetLoadoutRequestId)
            return;

        pendingSetLoadoutRequestId = 0;

        weaponPanel.SetLoadoutPending(false);

        if (result.Success)
            weaponPanel.SetLoadoutSelectionMode(false);

        RefreshAll();
    }

    /// <summary>
    /// Panel과 Replica 및 Gateway 이벤트 구독 시작
    /// </summary>
    public void Enable()
    {
        if (isEnabled)
            return;

        weaponPanel.OnWeaponSelected += HandleWeaponSelected;
        weaponPanel.OnUnlockRequested += HandleUnlockRequested;
        weaponPanel.OnSelectRequested += HandleSelectRequested;
        weaponPanel.OnLoadoutSlotSelected += HandleLoadoutSlotSelected;
        weaponPanel.OnLoadoutSlotRemoveRequested += HandleLoadoutslotRemoveRequested;

        weaponReadAccess.OnWeaponStateChanged += HandleWeaponStateChanged;

        inventoryReadAccess.OnInventoryChange += HandleInventoryChanged;

        weaponGateway.OnUnlockCompleted += HandleUnlockCompleted;
        weaponGateway.OnSetLoadoutCompleted += HandleSetLoadoutCompleted;

        isEnabled = true;

        RefreshAll();
    }

    /// <summary>
    /// Panel과 Replica 및 Gateway 이벤트 구독 해제
    /// </summary>
    public void Disable()
    {
        if (!isEnabled)
            return;

        weaponPanel.OnWeaponSelected -= HandleWeaponSelected;
        weaponPanel.OnUnlockRequested -= HandleUnlockRequested;
        weaponPanel.OnSelectRequested -= HandleSelectRequested;
        weaponPanel.OnLoadoutSlotSelected -= HandleLoadoutSlotSelected;
        weaponPanel.OnLoadoutSlotRemoveRequested -= HandleLoadoutslotRemoveRequested;

        weaponReadAccess.OnWeaponStateChanged -= HandleWeaponStateChanged;

        inventoryReadAccess.OnInventoryChange -= HandleInventoryChanged;

        weaponGateway.OnUnlockCompleted -= HandleUnlockCompleted;
        weaponGateway.OnSetLoadoutCompleted -= HandleSetLoadoutCompleted;

        selectedDefinition = null;

        pendingUnlockRequestId = 0;
        pendingSetLoadoutRequestId = 0;

        isEnabled = false;
    }


    /// <summary>
    /// 현재 Inventory와 무기 상태로 전체 해금 UI 데이터 생성
    /// </summary>
    /// <returns>전체 무기 해금 UI 데이터</returns>
    public IReadOnlyList<WeaponUnlockViewData> GetUnlockViewData()
    {
        InventoryAmountLookup inventoryAmounts = new InventoryAmountLookup(inventoryReadAccess.GetAllSnapshot());

        WeaponUnlockViewData[] viewData = new WeaponUnlockViewData[catalog.Definitions.Count];

        for (int i = 0; i < catalog.Definitions.Count; i++)
        {
            WeaponUnlockDefine definition = catalog.Definitions[i];

            if (definition == null || definition.Weapon == null)
                throw new InvalidOperationException($"Weapon Catalog의 {i}번 설정이 올바르지 않음");

            IReadOnlyList<WeaponRequirementViewData> requirementViewData = CreateRequirementViewData(definition.Requirements, inventoryAmounts);

            bool isUnlocked = weaponReadAccess.IsWeaponUnlocked(definition.Weapon.ItemUID);

            bool canUnlock = !isUnlocked;

            for (int requirementIndex = 0; requirementIndex < requirementViewData.Count; requirementIndex++)
            {
                if (requirementViewData[requirementIndex].HasEnough)
                    continue;

                canUnlock = false;

                break;
            }

            viewData[i] = new WeaponUnlockViewData(definition, isUnlocked, canUnlock, requirementViewData);
        }

        return Array.AsReadOnly(viewData);
    }

    /// <summary>
    /// 현재 Main과 Reserve 및 해금된 무기 목록 생성
    /// </summary>
    /// <returns>현재 Loadout UI 데이터</returns>
    public WeaponLoadoutViewData GetLoadoutViewData()
    {
        PlayerWeaponSnapshot snapshot = weaponReadAccess.GetSnapshot();

        HoldItemDefine mainWeapon = null;

        if(!string.IsNullOrEmpty(snapshot.MainWeaponId))
            mainWeapon = ResolveWeapon(snapshot.MainWeaponId);

        HoldItemDefine reserveWeapon = null;

        if (!string.IsNullOrEmpty(snapshot.ReserveWeaponId))
            reserveWeapon = ResolveWeapon(snapshot.ReserveWeaponId);

        List<HoldItemDefine> unlockedWeapons = new List<HoldItemDefine>();

        for (int i = 0; i < catalog.Definitions.Count; i++)
        {
            WeaponUnlockDefine definition = catalog.Definitions[i];

            if (weaponReadAccess.IsWeaponUnlocked(definition.Weapon.ItemUID))
            {
                unlockedWeapons.Add(definition.Weapon);
            }
        }

        return new WeaponLoadoutViewData(mainWeapon, reserveWeapon, unlockedWeapons);
    }

    /// <summary>
    /// Weapon Category 필터 변경
    /// </summary>
    /// <param name="value">선택한 Weapon Category</param>
    public void Select(WeaponCategory value)
    {
        if (!Enum.IsDefined(typeof(WeaponCategory), value))
            throw new ArgumentOutOfRangeException(nameof(value));

        selectedCategory = value;
        selectedDefinition = FindFirstDefinition(selectedCategory);

        weaponPanel.SetLoadoutSelectionMode(false);
        weaponPanel.HideWeaponInfo();

        RefreshUnlockViews();
    }
}