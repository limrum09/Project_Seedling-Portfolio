using UnityEngine;

/// <summary>
/// Player 구성 요소와 외부 연결에 필요한 접근점을 제공
/// 게임 규칙과 권한 상태 변경은 직접 처리하지 않음
/// </summary>
public class Player : MonoBehaviour
{
    [Header("Components")]
    [SerializeField]
    private PlayerMovement movement;
    [SerializeField]
    private PlayerMovementInput movementInput;
    [SerializeField]
    private LocalInputSource inputSource;
    [SerializeField]
    private PlayerStats stats;
    [SerializeField]
    private PlayerStatus status;
    [SerializeField]
    private PlayerFacing facing;
    [SerializeField]
    private PlayerAnimation playerAnim;
    [SerializeField]
    private PlayerCombat combatInput;
    [SerializeField]
    private PlayerWeaponReplica weaponReplica;
    [SerializeField]
    private PlayerInteraction playerInteraction;
    [SerializeField]
    private PlayerInput playerInput;
    [SerializeField]
    private PlayerPointerInteraction pointerInteraction;
    [SerializeField]
    private PlayerVehicleAttachment vehicleAttachment;

    [Header("Service")]
    [SerializeField]
    private HoldItemEquipmentService equipmentService;
    [SerializeField]
    private HoldItemAttackService attackService;
    [SerializeField]
    private InventoryReplica inventoryReplica;

    [Header("GateWays")]
    [SerializeField]
    private InventoryGateway inventoryGateway;
    [SerializeField]
    private MiningGateway miningGateway;
    [SerializeField]
    private PlayerWeaponGateway weaponGateway;
    [SerializeField]
    private ConsumableUseGateway consumableGateway;
    [SerializeField]
    private JetpackEnergyGateway jetpackEnergyGateway;

    [Header("Bridges")]
    [SerializeField]
    private PlayerStatusBridge statusBridge;

    [Header("Objects")]
    [SerializeField]
    private Transform cameraTarget;

    public PlayerStats Stats => stats;
    public IPlayerWeaponReadAccess WeaponReadAccess => weaponReplica;
    public IPlayerStatusReadAccess Status => status;
    public IInventoryReadAccess InvenReadAccess => inventoryReplica;
    public IInventoryCommandGateWay InventoryCommandGateway => inventoryGateway;
    public IWorldInspectionSource InspectionSource => playerInput;
    public InventoryGateway InvenGateway => inventoryGateway;
    public MiningGateway MinerGateWay => miningGateway;
    public PlayerWeaponGateway WeaponGateway => weaponGateway;
    public AttackEnergyGateway EnergyGateway => attackService.EnergyGateway;
    public PlayerStatusBridge StatusBridge => statusBridge;
    public ConsumableUseGateway ConsumableGateway => consumableGateway;
    public JetpackEnergyGateway JetpackEnergyGateway => jetpackEnergyGateway;
    public Transform CameraTarget => cameraTarget;
    public PlayerVehicleAttachment VehicleAttachment => vehicleAttachment;
    public IPlayerMovementInputSource MovementInputSource => inputSource;
    public IPlayerActionInputSource ActionInputSource => inputSource;
    public IPlacementInputSource PlacementInputSource => inputSource;
    public IPointerInputSource PointerInputSource => inputSource;
    public IInputMapControl InputMapControl => inputSource;

    /// <summary>
    /// Player 내부 입력 소비자에 Local Input Source 연결
    /// </summary>
    private void Awake()
    {
        movementInput.BindInput(inputSource);
        playerInput.BindInput(inputSource, inputSource);
    }

    /// <summary>
    /// Player 기능 이벤트 연결
    /// </summary>
    private void OnEnable()
    {
        equipmentService.OnHoldItemChanged += playerAnim.SetHoldItem;
        attackService.OnAttackSuccess += HandleAttackSuccess;
        attackService.OnHit += miningGateway.ReportHit;
        facing.OnAimLockChanged += playerAnim.SetAiming;
    }

    /// <summary>
    /// Player 기능 이벤트 연결 해제
    /// </summary>
    private void OnDisable()
    {
        equipmentService.OnHoldItemChanged -= playerAnim.SetHoldItem;
        attackService.OnAttackSuccess -= HandleAttackSuccess;
        attackService.OnHit -= miningGateway.ReportHit;
        facing.OnAimLockChanged -= playerAnim.SetAiming;
    }

    /// <summary>
    /// 공격 성공 결과를 Player Animation에 전달
    /// </summary>
    /// <param name="item">공격에 사용한 장비</param>
    private void HandleAttackSuccess(HoldItemRuntime item)
    {
        playerAnim.PlayerAttack();
    }

    /// <summary>
    /// World 상호작용에서 사용할 UI 표시 요청 접근점 연결
    /// </summary>
    /// <param name="uiOpenRequest">UI 표시 요청 접근점</param>
    public void BindInteractionUI(IUIOpenRequest uiOpenRequest)
    {
        playerInteraction.BindUIOpenRequest(uiOpenRequest);
        playerInput.BindUIOpenRequest(uiOpenRequest);
    }

    /// <summary>
    /// Player 이동과 전투 입력에 사용할 World Camera를 설정
    /// </summary>
    /// <param name="worldCamera">사용할 World Camera</param>
    public void SetCamera(Camera worldCamera)
    {
        movement.SetCamera(worldCamera);
        combatInput.SetCamera(worldCamera);
        pointerInteraction.SetCamera(worldCamera);
    }

    /// <summary>
    /// 현재 Player 조작 권한 설정
    /// </summary>
    /// <param name="permissions">적용할 Player 조작 권한</param>
    public void SetControlPermissions(PlayerControlPermissions permissions)
    {
        bool movementEnabled = (permissions & PlayerControlPermissions.Movement) != 0;

        movement.SetStopPlayer(!movementEnabled);
        playerInput.SetControlPermissions(permissions);
    }

    /// <summary>
    /// Player 공격에 사용될 Scene Runtime Pool 연결
    /// </summary>
    /// <param name="projectilePool"></param>
    /// <param name="hitImpactEffectPool"></param>
    public void BindAttackPool(ProjectilePool projectilePool, HitImpactEffectPool hitImpactEffectPool)
    {
        attackService.BindPool(projectilePool, hitImpactEffectPool);
    }

    /// <summary>
    /// 지정한 조준점을 향해 단발 공격을 시도
    /// </summary>
    /// <param name="aimPoint">요청한 조준점</param>
    /// <returns>공격을 실행했으면 true</returns>
    public bool TryAttack(Vector3 aimPoint)
    {
        facing.TryResolveAimPoint(aimPoint, out Vector3 resolveAimPoint);

        facing.SetAimPoint(resolveAimPoint);

        return attackService.TryAttack(gameObject, resolveAimPoint);
    }

    /// <summary>
    /// 지정한 조준점을 향해 연속 공격을 시작
    /// </summary>
    /// <param name="aimPoint">요청한 조준점</param>
    /// <returns>연속 공격을 시작했으면 true</returns>
    public bool BeginAttack(Vector3 aimPoint)
    {
        facing.TryResolveAimPoint(aimPoint, out Vector3 resolveAimPoint);

        facing.SetAimPoint(resolveAimPoint);

        return attackService.BeginAttack(gameObject, resolveAimPoint);
    }

    /// <summary>
    /// 지정한 조준점을 향한 연속 공격을 유지
    /// </summary>
    /// <param name="aimPoint">요청한 조준점</param>
    /// <returns>연속 공격을 유지했으면 true</returns>
    public bool HoldAttack(Vector3 aimPoint)
    {
        facing.TryResolveAimPoint(aimPoint, out Vector3 resolveAimPoint);

        facing.SetAimPoint(resolveAimPoint);

        return attackService.HoldAttack(gameObject, resolveAimPoint);
    }

    /// <summary>
    /// 현재 연속 공격을 종료
    /// </summary>
    public void EndAttack()
    {
        attackService.EndAttack();
    }

    /// <summary>
    /// 지정한 장비 착용을 시도
    /// </summary>
    /// <param name="item">착용할 장비</param>
    /// <returns>장비를 착용했으면 true</returns>
    public bool TryEquip(HoldItemRuntime item)
    {
        return equipmentService.TryEquip(item);
    }

    /// <summary>
    /// 현재 장비의 착용을 해제
    /// </summary>
    public void UnEquip()
    {
        equipmentService.UnEquip();
    }
}
