using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 로컬 Player 입력을 각 기능의 요청 접근점으로 전달
/// 현재 조작 권한에 따라 전투, World 상호작용과 일반 단축키 처리 분리
/// </summary>
public class PlayerInput : MonoBehaviour, IWorldInspectionSource
{
    [SerializeField]
    private PlayerCombat combat;
    [SerializeField]
    private PlayerInteraction interaction;
    [SerializeField]
    private PlayerPointerInteraction pointerInteraction;
    [SerializeField]
    private ConsumableUseGateway consumableGateway;
    [SerializeField]
    private PlayerWeaponGateway weaponGateway;

    private IPlayerActionInputSource actionInput;
    private IUIInputSource uiInput;
    private IUIOpenRequest uiOpenRequest;

    private IWorldHoverTarget currentHoverTarget;
    private PlayerControlPermissions controlPermissions = PlayerControlPermissions.All;

    public IWorldInspectable CurrentInspectionTarget => currentHoverTarget as IWorldInspectable;

    public event Action<IWorldInspectable> OnInspectionTargetChanged;

    /// <summary>
    /// UI 입력을 우선 처리한 뒤 현재 조작 권한에 맞는 Player 입력 전달
    /// </summary>
    private void Update()
    {
        if (TryHandleUIInput())
            return;

        if (HasPermission(PlayerControlPermissions.WorldInteraction))
            UpdatePointerHover();
        else
            SetPointerHoverTarget(null);

        if (HasPermission(PlayerControlPermissions.WorldInteraction) && TryHandlePointerInteraction())
            return;

        if (HasPermission(PlayerControlPermissions.Combat))
            combat.Tick(uiInput.PointerPosition, actionInput.AttackPressedThisFrame, actionInput.AttackHeld, actionInput.AttackReleasedThisFrame);

        if (HasPermission(PlayerControlPermissions.Hotkeys))
            HandleGameplayHotkeys();
    }

    /// <summary>
    /// Player Input이 비활성화될 때 공격과 Pointer Hover 상태 해제
    /// </summary>
    private void OnDisable()
    {
        combat.CancelAttackInput();
        SetPointerHoverTarget(null);
    }

    /// <summary>
    /// Player Action과 UI 입력 접근점 연결
    /// </summary>
    /// <param name="getActionInput">Player Action 입력 접근점</param>
    /// <param name="getUIInput">UI 입력 접근점</param>
    public void BindInput(IPlayerActionInputSource getActionInput, IUIInputSource getUIInput)
    {
        if (getActionInput == null)
            throw new ArgumentNullException(nameof(getActionInput));

        if (getUIInput == null)
            throw new ArgumentNullException(nameof(getUIInput));

        actionInput = getActionInput;
        uiInput = getUIInput;
    }

    /// <summary>
    /// UI Panel 요청 접근점 연결
    /// </summary>
    /// <param name="request">UI Panel 요청 접근점</param>
    public void BindUIOpenRequest(IUIOpenRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        uiOpenRequest = request;
    }

    /// <summary>
    /// 현재 Player 조작 권한 설정
    /// 제거된 권한에 해당하는 진행 중 입력은 즉시 정리
    /// </summary>
    /// <param name="permissions">적용할 Player 조작 권한</param>
    public void SetControlPermissions(PlayerControlPermissions permissions)
    {
        bool hadCombatPermission = HasPermission(PlayerControlPermissions.Combat);

        controlPermissions = permissions;

        if (hadCombatPermission && !HasPermission(PlayerControlPermissions.Combat))
            combat.CancelAttackInput();

        if (!HasPermission(PlayerControlPermissions.WorldInteraction))
            SetPointerHoverTarget(null);
    }

    /// <summary>
    /// 지정한 Player 조작 권한이 현재 허용되어 있는지 확인
    /// </summary>
    /// <param name="permission">확인할 Player 조작 권한</param>
    /// <returns>현재 허용된 권한이면 true</returns>
    private bool HasPermission(PlayerControlPermissions permission)
    {
        return (controlPermissions & permission) != 0;
    }

    /// <summary>
    /// Inventory Toggle과 뒤로 가기 입력 처리
    /// </summary>
    /// <returns>UI 입력을 처리했으면 true</returns>
    private bool TryHandleUIInput()
    {
        bool backRequested = uiInput.BackPressedThisFrame;
        bool inventoryRequested = uiInput.ToggleInventoryPressedThisFrame;
        bool furnitureRequested = uiInput.ToggleFurniturePressedThisFrame;

        if (!backRequested && !inventoryRequested && !furnitureRequested)
            return false;

        if (uiOpenRequest == null)
            throw new InvalidOperationException("Player Input에 UI 요청 접근점이 연결되지 않음");

        combat.CancelAttackInput();

        if (backRequested)
        {
            uiOpenRequest.Back();
            return true;
        }

        if (furnitureRequested)
        {
            if (HasPermission(PlayerControlPermissions.Movement))
                uiOpenRequest.Toggle(UIPanelId.Furniture);

            return true;
        }

        uiOpenRequest.Toggle(UIPanelId.Inventory);

        return true;
    }

    /// <summary>
    /// Pointer Primary 입력을 UI 또는 World 상호작용으로 처리
    /// </summary>
    /// <returns>Pointer 입력을 소비했으면 true</returns>
    private bool TryHandlePointerInteraction()
    {
        if (!actionInput.AttackPressedThisFrame)
            return false;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            combat.CancelAttackInput();
            return true;
        }

        Vector2 screenPosition = uiInput.PointerPosition;

        if (!pointerInteraction.TryGetInteractable(screenPosition, out IPlayerInteractable interactable))
            return false;

        if (!interaction.TryInteract(interactable))
            return false;

        combat.CancelAttackInput();

        return true;
    }

    /// <summary>
    /// 상호작용, 무기 교체와 소모품 입력 처리
    /// </summary>
    private void HandleGameplayHotkeys()
    {
        if (actionInput.InteractPressedThisFrame)
        {
            interaction.TryInteractRegistered();
            return;
        }

        if (actionInput.SwapWeaponPressedThisFrame)
        {
            weaponGateway.RequestSwapWeapons();
            return;
        }

        if (actionInput.UseEnergyPressedThisFrame)
        {
            consumableGateway.RequestUse(ItemConsumableType.Energy);
            return;
        }

        if (actionInput.UseFoodPressedThisFrame)
        {
            consumableGateway.RequestUse(ItemConsumableType.Food);
            return;
        }

        if (actionInput.UseOxygenPressedThisFrame)
            consumableGateway.RequestUse(ItemConsumableType.Oxygen);
    }

    /// <summary>
    /// 현재 Pointer 위치를 이용해 Hover 대상 갱신
    /// </summary>
    private void UpdatePointerHover()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            SetPointerHoverTarget(null);
            return;
        }

        pointerInteraction.TryGetHoverTarget(uiInput.PointerPosition, out IWorldHoverTarget hoverTarget);

        SetPointerHoverTarget(hoverTarget);
    }

    /// <summary>
    /// 기존 Hover를 해제하고 새로운 Hover 대상 설정
    /// </summary>
    /// <param name="nextTarget">새로운 Hover 대상</param>
    private void SetPointerHoverTarget(IWorldHoverTarget nextTarget)
    {
        if (ReferenceEquals(currentHoverTarget, nextTarget))
            return;

        if (currentHoverTarget is MonoBehaviour currentBehaviour && currentBehaviour != null)
            currentHoverTarget.SetHovered(false);

        currentHoverTarget = nextTarget;

        if (currentHoverTarget is MonoBehaviour nextBehaviour && nextBehaviour != null)
            currentHoverTarget.SetHovered(true);

        OnInspectionTargetChanged?.Invoke(CurrentInspectionTarget);
    }
}
