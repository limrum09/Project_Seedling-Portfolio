using System;
using UnityEngine;

/// <summary>
/// 로컬 Player와 같은 프로세스에서 동작할 권한 영역을 생성하고 Local 전달 계층 연결
/// </summary>
public sealed class LocalPlayerSpawner : PlayerSpawner
{
    [SerializeField]
    private Player playerPrefab;
    [SerializeField]
    private PlayerAuthority authorityPrefab;
    [SerializeField]
    private Transform spawnPoint;
    [SerializeField]
    private MiningService  miningService;

    [Header("Pools")]
    [SerializeField]
    private ProjectilePool projectilePool;
    [SerializeField]
    private HitImpactEffectPool hitImpactEffectPool;

    public PlayerAuthority CurrentAuthority { get; private set; }


    /// <summary>
    /// PlayerStats의 최대값으로 권한 PlayerStatus 초기 상태를 생성
    /// </summary>
    /// <param name="stats">초기 최대 자원값을 제공할 PlayerStats</param>
    /// <returns>모든 자원이 최대치인 초기 상태</returns>
    private PlayerStatusSnapshot CreateInitialStatus(PlayerStats stats)
    {
        if (stats == null)
            throw new ArgumentNullException(nameof(stats));

        return new PlayerStatusSnapshot(stats.MaxEnergy, stats.MaxHP, stats.MaxFood, stats.MaxOxygen, stats.MaxEnergy, stats.MaxHP, stats.MaxFood, stats.MaxOxygen);
    }

    /// <summary>
    /// Player와 로컬 권한 영역을 생성하고 모든 Local 전달 계층을 연결
    /// </summary>
    public override void BeginSpawn()
    {
        Player player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        player.BindAttackPool(projectilePool, hitImpactEffectPool);

        CurrentAuthority = Instantiate(authorityPrefab, transform);
        CurrentAuthority.Init(CreateInitialStatus(player.Stats));

        LocalInventoryGateway inventoryGateway = player.InvenGateway as LocalInventoryGateway;
        LocalMiningGateway miningGateway = player.MinerGateWay as LocalMiningGateway;
        LocalPlayerWeaponGateway weaponGateway = player.WeaponGateway as LocalPlayerWeaponGateway;
        LocalAttackEnergyGateway energyGateway = player.EnergyGateway as LocalAttackEnergyGateway;
        LocalPlayerStatusBridge statusBridge = player.StatusBridge as LocalPlayerStatusBridge;
        LocalConsumableUseGateway consumableGateway = player.ConsumableGateway as LocalConsumableUseGateway;
        LocalJetpackEnergyGateway jetpackGateway = player.JetpackEnergyGateway as LocalJetpackEnergyGateway;

        if (inventoryGateway == null)
            throw new InvalidOperationException("Local Player에 Local Inventory Gateway가 연결되지 않음");

        if (miningGateway == null)
            throw new InvalidOperationException("Local Player에 Local Mining Gateway가 연결되지 않음");

        if(weaponGateway == null)
            throw new InvalidOperationException("Local Player에 Local Player Weapon Gateway가 연결되지 않음");

        if(energyGateway == null)
            throw new InvalidOperationException("Local Player에 Local Attack Energy Gateway가 연결되지 않음");

        if (statusBridge == null)
            throw new InvalidOperationException("Local Player에 Local Player Status Bridge가 연결되지 않음");

        if (consumableGateway == null)
            throw new InvalidOperationException("Local Player에 Local Consumable Use Gateway가 연결되지 않음");

        if (jetpackGateway == null)
            throw new InvalidOperationException("Local Player에 Local Jetpack Energy Gateway가 연결되지 않음");

        inventoryGateway.Bind(CurrentAuthority.InventoryService);
        miningGateway.Bind(player.gameObject, miningService, CurrentAuthority.ItemReceiver);
        weaponGateway.Bind(player.gameObject, CurrentAuthority.WeaponService);
        energyGateway.bind(CurrentAuthority.StatusService);
        statusBridge.Bind(CurrentAuthority.StatusService);
        consumableGateway.Bind(CurrentAuthority.ConsumableUseService);
        jetpackGateway.Bind(CurrentAuthority.StatusService);

        NotifyLocalPlayerReady(player);
    }
}
