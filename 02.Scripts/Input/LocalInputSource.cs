using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Pointer 화면 위치를 제공하는 입력 접근점
/// </summary>
public interface IPointerInputSource
{
    Vector2 PointerPosition { get; }
}

/// <summary>
/// Player 이동에서 사용하는 입력 상태
/// </summary>
public interface IPlayerMovementInputSource
{
    Vector2 Move { get; }
    bool JumpPressedThisFrame { get; }
    bool JumpHeld { get; }
    bool SprintHeld { get; }
}

/// <summary>
/// Player 기능 요청에서 사용하는 입력 상태
/// </summary>
public interface IPlayerActionInputSource
{
    bool AttackPressedThisFrame { get; }
    bool AttackHeld { get; }
    bool AttackReleasedThisFrame { get; }
    bool InteractPressedThisFrame { get; }
    bool SwapWeaponPressedThisFrame { get; }
    bool UseEnergyPressedThisFrame { get; }
    bool UseFoodPressedThisFrame { get; }
    bool UseOxygenPressedThisFrame { get; }
}

/// <summary>
/// UI 표시와 뒤로 가기에서 사용하는 입력 상태
/// </summary>
public interface IUIInputSource : IPointerInputSource
{
    bool ToggleInventoryPressedThisFrame { get; }
    bool ToggleFurniturePressedThisFrame { get; }
    bool BackPressedThisFrame { get; }
}

/// <summary>
/// Building과 Furniture 배치에서 사용하는 입력 상태
/// </summary>
public interface IPlacementInputSource : IPointerInputSource
{
    bool PrimaryPressedThisFrame { get; }
    bool SecondaryPressedThisFrame { get; }
    bool RotateClockwisePressedThisFrame { get; }
    bool RotateCounterClockwisePressedThisFrame { get; }
    bool ModifierHeld { get; }
    Vector2 CameraMove { get; }
    Vector2 CameraRotate { get; }
    float Zoom { get; }
}

/// <summary>
/// Local Input Action Map 활성 상태를 변경하는 접근점
/// </summary>
public interface IInputMapControl
{
    /// <summary>
    /// Player Action Map 활성 상태 설정
    /// </summary>
    /// <param name="isEnabled">Player Action Map 활성 여부</param>
    void SetPlayerActionsEnabled(bool isEnabled);

    /// <summary>
    /// Placement Action Map 활성 상태 설정
    /// </summary>
    /// <param name="isEnabled">Placement Action Map 활성 여부</param>
    void SetPlacementActionsEnabled(bool isEnabled);
}

/// <summary>
/// Input Action Asset을 런타임 입력 상태로 변환하고 Action Map 활성 상태 관리
/// 게임 기능 요청은 직접 처리하지 않음
/// </summary>
public sealed class LocalInputSource : MonoBehaviour, IPlayerMovementInputSource, IPlayerActionInputSource, IUIInputSource, IPlacementInputSource, IInputMapControl
{
    [SerializeField]
    private InputActionAsset actionAsset;

    private InputActionAsset runtimeActions;

    private InputActionMap playerMap;
    private InputActionMap uiMap;
    private InputActionMap placementMap;

    private InputAction moveAction;
    private InputAction attackAction;
    private InputAction interactAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction swapWeaponAction;
    private InputAction useEnergyAction;
    private InputAction useFoodAction;
    private InputAction useOxygenAction;

    private InputAction pointAction;
    private InputAction toggleInventoryAction;
    private InputAction toggleFurnitureAction;
    private InputAction backAction;

    private InputAction placementPrimaryAction;
    private InputAction placementSecondaryAction;
    private InputAction rotateClockwiseAction;
    private InputAction rotateCounterClockwiseAction;
    private InputAction modifierAction;
    private InputAction cameraMoveAction;
    private InputAction cameraRotateAction;
    private InputAction zoomAction;

    private bool playerActionsEnabled = true;
    private bool placementActionsEnabled;

    public Vector2 Move => moveAction.ReadValue<Vector2>();
    public bool JumpPressedThisFrame => jumpAction.WasPressedThisFrame();
    public bool JumpHeld => jumpAction.IsPressed();
    public bool SprintHeld => sprintAction.IsPressed();

    public bool AttackPressedThisFrame => attackAction.WasPressedThisFrame();
    public bool AttackHeld => attackAction.IsPressed();
    public bool AttackReleasedThisFrame => attackAction.WasReleasedThisFrame();
    public bool InteractPressedThisFrame => interactAction.WasPressedThisFrame();
    public bool SwapWeaponPressedThisFrame => swapWeaponAction.WasPressedThisFrame();
    public bool UseEnergyPressedThisFrame => useEnergyAction.WasPressedThisFrame();
    public bool UseFoodPressedThisFrame => useFoodAction.WasPressedThisFrame();
    public bool UseOxygenPressedThisFrame => useOxygenAction.WasPressedThisFrame();

    public Vector2 PointerPosition => pointAction.ReadValue<Vector2>();
    public bool ToggleInventoryPressedThisFrame => toggleInventoryAction.WasPressedThisFrame();
    public bool ToggleFurniturePressedThisFrame => toggleFurnitureAction.WasPressedThisFrame();
    public bool BackPressedThisFrame => backAction.WasPressedThisFrame();

    public bool PrimaryPressedThisFrame => placementPrimaryAction.WasPressedThisFrame();
    public bool SecondaryPressedThisFrame => placementSecondaryAction.WasPressedThisFrame();
    public bool RotateClockwisePressedThisFrame => rotateClockwiseAction.WasPressedThisFrame();
    public bool RotateCounterClockwisePressedThisFrame => rotateCounterClockwiseAction.WasPressedThisFrame();
    public bool ModifierHeld => modifierAction.IsPressed();
    public Vector2 CameraMove => cameraMoveAction.ReadValue<Vector2>();
    public Vector2 CameraRotate => cameraRotateAction.ReadValue<Vector2>();
    public float Zoom => zoomAction.ReadValue<float>();

    /// <summary>
    /// 런타임 Action Asset을 생성하고 필수 Action 연결
    /// </summary>
    private void Awake()
    {
        if (actionAsset == null)
            throw new InvalidOperationException("Local Input Source에 Input Action Asset이 설정되지 않음");

        runtimeActions = Instantiate(actionAsset);

        playerMap = RequireMap("Player");
        uiMap = RequireMap("UI");
        placementMap = RequireMap("Placement");

        moveAction = RequireAction(playerMap, "Move");
        attackAction = RequireAction(playerMap, "Attack");
        interactAction = RequireAction(playerMap, "Interact");
        jumpAction = RequireAction(playerMap, "Jump");
        sprintAction = RequireAction(playerMap, "Sprint");
        swapWeaponAction = RequireAction(playerMap, "SwapWeapon");
        useEnergyAction = RequireAction(playerMap, "UseEnergy");
        useFoodAction = RequireAction(playerMap, "UseFood");
        useOxygenAction = RequireAction(playerMap, "UseOxygen");

        pointAction = RequireAction(uiMap, "Point");
        toggleInventoryAction = RequireAction(uiMap, "ToggleInventory");
        toggleFurnitureAction = RequireAction(uiMap, "ToggleFurniture");
        backAction = RequireAction(uiMap, "Cancel");

        placementPrimaryAction = RequireAction(placementMap, "Primary");
        placementSecondaryAction = RequireAction(placementMap, "Secondary");
        rotateClockwiseAction = RequireAction(placementMap, "RotateClockwise");
        rotateCounterClockwiseAction = RequireAction(placementMap, "RotateCounterClockwise");
        modifierAction = RequireAction(placementMap, "Modifier");
        cameraMoveAction = RequireAction(placementMap, "CameraMove");
        cameraRotateAction = RequireAction(placementMap, "CameraRotate");
        zoomAction = RequireAction(placementMap, "Zoom");
    }

    /// <summary>
    /// 현재 입력 모드에 맞게 Action Map 활성 상태 적용
    /// </summary>
    private void OnEnable()
    {
        ApplyMapState();
    }

    /// <summary>
    /// Local Input이 비활성화될 때 모든 런타임 Action 비활성화
    /// </summary>
    private void OnDisable()
    {
        if (runtimeActions != null)
            runtimeActions.Disable();
    }

    /// <summary>
    /// 생성한 런타임 Action Asset 제거
    /// </summary>
    private void OnDestroy()
    {
        if (runtimeActions != null)
            Destroy(runtimeActions);
    }

    /// <summary>
    /// Player Action Map 활성 상태 설정
    /// </summary>
    /// <param name="isEnabled">Player Action Map 활성 여부</param>
    public void SetPlayerActionsEnabled(bool isEnabled)
    {
        playerActionsEnabled = isEnabled;

        if (!isActiveAndEnabled)
            return;

        if (playerActionsEnabled)
            playerMap.Enable();
        else
            playerMap.Disable();
    }

    /// <summary>
    /// Placement Action Map 활성 상태 설정
    /// </summary>
    /// <param name="isEnabled">Placement Action Map 활성 여부</param>
    public void SetPlacementActionsEnabled(bool isEnabled)
    {
        placementActionsEnabled = isEnabled;

        if (!isActiveAndEnabled)
            return;

        if (placementActionsEnabled)
            placementMap.Enable();
        else
            placementMap.Disable();
    }

    /// <summary>
    /// 저장된 입력 모드에 맞춰 Action Map 상태 적용
    /// </summary>
    private void ApplyMapState()
    {
        uiMap.Enable();

        if (playerActionsEnabled)
            playerMap.Enable();
        else
            playerMap.Disable();

        if (placementActionsEnabled)
            placementMap.Enable();
        else
            placementMap.Disable();
    }

    /// <summary>
    /// 지정한 이름의 필수 Action Map 반환
    /// </summary>
    /// <param name="mapName">Action Map 이름</param>
    /// <returns>찾은 Action Map</returns>
    private InputActionMap RequireMap(string mapName)
    {
        InputActionMap map = runtimeActions.FindActionMap(mapName, false);

        if (map == null)
            throw new InvalidOperationException($"{mapName} Action Map이 없음");

        return map;
    }

    /// <summary>
    /// 지정한 Map의 필수 Action 반환
    /// </summary>
    /// <param name="map">Action을 찾을 Map</param>
    /// <param name="actionName">Action 이름</param>
    /// <returns>찾은 Input Action</returns>
    private InputAction RequireAction(InputActionMap map, string actionName)
    {
        InputAction action = map.FindAction(actionName, false);

        if (action == null)
            throw new InvalidOperationException($"{map.name}/{actionName} Action이 없음");

        return action;
    }
}
