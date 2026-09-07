using UnityEngine;

/// <summary>
/// 한 플레이어의 권한 Inventory와 Status 생명 주기를 구성하고 권한 기능 접근점을 제공
/// </summary>
public sealed class PlayerAuthority : MonoBehaviour
{
    [SerializeField]
    private PlayerInventoryAuthority inventoryAuthority;
    [SerializeField]
    private PlayerStatusService statusService;
    [SerializeField]
    private ConsumableUseService consumableUseService;
    [SerializeField]
    private PlayerWeaponService weaponService;

    public InventoryService InventoryService => inventoryAuthority.InventoryService;
    public PlayerStatusService StatusService => statusService;
    public ConsumableUseService ConsumableUseService => consumableUseService;
    public PlayerWeaponService WeaponService => weaponService;
    public IInventoryItemReceiver ItemReceiver => inventoryAuthority.ItemReceiver;
    public IInventoryConsumption Consumption => inventoryAuthority.Consumption;

    /// <summary>
    /// 필수 권한 Service 참조가 연결되어 있는지 Editor에서 확인
    /// </summary>
    private void OnValidate()
    {
        if (inventoryAuthority == null)
            Debug.LogError("Player Authority에 Player Inventory Authority 참조가 없음", this);

        if (statusService == null)
            Debug.LogError("Player Authority에 Player Status Service 참조가 없음", this);

        if (consumableUseService == null)
            Debug.LogError("Player Authority에 Consumable Use Service 참조가 없음", this);

        if(weaponService == null)
            Debug.LogError("Player Authority에 Player Weapon Service 참조가 없음", this);
    }

    /// <summary>
    /// 권한 Inventory와 Status를 초기화하고 ConsumableUseService를 연결
    /// </summary>
    /// <param name="initialStatus">초기 플레이어 자원 상태</param>
    public void Init(PlayerStatusSnapshot initialStatus)
    {
        inventoryAuthority.Init();
        statusService.Init(initialStatus);
        consumableUseService.Bind(inventoryAuthority.InventoryService, statusService);
        weaponService.InitNewPlayer(inventoryAuthority.Consumption);
    }
}
