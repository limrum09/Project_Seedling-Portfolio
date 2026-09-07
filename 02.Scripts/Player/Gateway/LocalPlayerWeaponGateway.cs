using System;
using UnityEngine;

/// <summary>
/// 플레이어 무기 요청을 같은 프로세스의 권한 Service에 전달
/// </summary>
public sealed class LocalPlayerWeaponGateway : PlayerWeaponGateway
{
    [SerializeField]
    private LocalResponseDispatcher dispatcher;
    [SerializeField]
    private PlayerWeaponReplica weaponReplica;

    private GameObject player;
    private PlayerWeaponService weaponService;
    private int bindingVersion;

    /// <summary>
    /// 활성화될 때 현재 권한 상태를 적용하고 변경 이벤트 구독
    /// </summary>
    private void OnEnable()
    {
        if (weaponService == null)
            return;

        if (player == null)
            throw new InvalidOperationException("Local Player Weapon Gateway에 Player가 연결되지 않음");

        SubscribeAndApplyFullSnapshot();
    }

    /// <summary>
    /// 비활성화될 때 권한 상태 이벤트 구독 해제
    /// </summary>
    private void OnDisable()
    {
        Unsubscribe();
        bindingVersion++;
    }

    /// <summary>
    /// 현재 권한 상태를 Replica에 적용한 뒤 변경 이벤트 구독
    /// </summary>
    private void SubscribeAndApplyFullSnapshot()
    {
        weaponReplica.ApplyFullSnapshot(weaponService.CreateStateSnapshot());

        weaponService.OnWeaponStateChanged += HandleWeaponStateChanged;
    }

    /// <summary>
    /// 현재 권한 상태 변경 이벤트 구독 해제
    /// </summary>
    private void Unsubscribe()
    {
        if (weaponService == null)
            return;

        weaponService.OnWeaponStateChanged -= HandleWeaponStateChanged;
    }

    /// <summary>
    /// 전달받은 Snapshot이 권한 상태와 배열을 공유하지 않도록 복사
    /// </summary>
    /// <param name="snapshot">복사할 무기 상태</param>
    /// <returns>독립된 해금 목록을 가진 무기 상태</returns>
    private PlayerWeaponSnapshot CopySnapshot(PlayerWeaponSnapshot snapshot)
    {
        return new PlayerWeaponSnapshot(snapshot.MainWeaponId, snapshot.ReserveWeaponId, snapshot.UnlockedWeaponIds);
    }

    /// <summary>
    /// 권한 무기 상태를 공용 응답 Queue에 등록
    /// </summary>
    /// <param name="snapshot">전달할 무기 상태</param>
    private void HandleWeaponStateChanged(PlayerWeaponSnapshot snapshot)
    {
        PlayerWeaponSnapshot copiedSnapshot = CopySnapshot(snapshot);

        int responseVersion = bindingVersion;

        dispatcher.Enqueue(() =>
        {
            if (responseVersion != bindingVersion)
                return;

            weaponReplica.ApplyFullSnapshot(copiedSnapshot);
        });
    }

    /// <summary>
    /// 같은 프로세스의 권한 Service에 무기 해금 요청 전달
    /// </summary>
    /// <param name="requestId">요청 식별자</param>
    /// <param name="weaponId">해금할 Weapon ID</param>
    protected override void SendUnlockRequest(int requestId, string weaponId)
    {
        if (weaponService == null)
            throw new InvalidOperationException("Local Player Weapon Gateway가 Service에 연결되지 않음");

        PlayerWeaponOperationResult result = weaponService.TryUnlockWeapon(weaponId);

        int responseVersion = bindingVersion;

        dispatcher.Enqueue(() =>
        {
            if (responseVersion != bindingVersion)
                return;

            NotifyUnlockCompleted(requestId, result);
        });
    }

    /// <summary>
    /// 같은 프로세스의 권한 Service에 Loadout 변경 요청 전달
    /// </summary>
    /// <param name="requestId">요청 식별자</param>
    /// <param name="station">사용 중인 Loadout Station</param>
    /// <param name="slot">변경할 무기 슬롯</param>
    /// <param name="weaponId">배치할 Weapon ID</param>
    protected override void SendSetLoadoutRequest(int requestId, WeaponSlot slot, string weaponId)
    {
        if (weaponService == null)
            throw new InvalidOperationException("Local Player Weapon Gateway가 Service에 연결되지 않음");

        if (player == null)
            throw new InvalidOperationException("Local Player Weapon Gateway에 Player가 연결되지 않음");

        PlayerWeaponOperationResult result = weaponService.TrySetLoadout(player, slot, weaponId);

        int responseVersion = bindingVersion;

        dispatcher.Enqueue(() =>
        {
            if (responseVersion != bindingVersion)
                return;

            NotifySetLoadoutCompleted(requestId, result);
        });
    }

    protected override void SendClearLoadoutRequest(int requestId, WeaponSlot slot)
    {
        if (weaponService == null)
            throw new InvalidOperationException("Local Player Weapon Gateway가 Service에 연결되지 않음");

        if (player == null)
            throw new InvalidOperationException("Local Player Weapon Gateway에 Player가 연결되지 않음");

        PlayerWeaponOperationResult result = weaponService.TryClearLoadout(player, slot);

        int responseVersion = bindingVersion;

        dispatcher.Enqueue(() =>
        {
            if (responseVersion != bindingVersion)
                return;

            NotifySetLoadoutCompleted(requestId, result);
        });
    }

    /// <summary>
    /// 같은 프로세스의 권한 Service에 무기 스왑 요청 전달
    /// </summary>
    /// <param name="requestId">요청 식별자</param>
    protected override void SendSwapRequest(int requestId)
    {
        if (weaponService == null)
            throw new InvalidOperationException("Local Player Weapon Gateway가 Service에 연결되지 않음");

        PlayerWeaponOperationResult result = weaponService.TrySwapWeapons();

        int responseVersion = bindingVersion;

        dispatcher.Enqueue(() =>
        {
            if (responseVersion != bindingVersion)
                return;

            NotifySwapCompleted(requestId, result);
        });
    }

    /// <summary>
    /// 로컬 Player와 권한 무기 Service를 연결
    /// </summary>
    /// <param name="localPlayer">요청 플레이어</param>
    /// <param name="service">권한 PlayerWeaponService</param>
    public void Bind(GameObject localPlayer, PlayerWeaponService service)
    {
        if (localPlayer == null)
            throw new ArgumentNullException(nameof(localPlayer));

        if (service == null)
            throw new ArgumentNullException(nameof(service));

        Unsubscribe();

        player = localPlayer;
        weaponService = service;
        bindingVersion++;

        if (!isActiveAndEnabled)
            return;

        SubscribeAndApplyFullSnapshot();
    }

    /// <summary>
    /// 로컬 Player와 권한 무기 Service 연결 해제
    /// </summary>
    public void Unbind()
    {
        Unsubscribe();

        player = null;
        weaponService = null;
        bindingVersion++;
    }
}