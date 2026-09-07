using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 권한 영역에서 플레이어 무기 해금과 Loadout 규칙을 처리
/// </summary>
public sealed class PlayerWeaponService : MonoBehaviour
{
    [SerializeField]
    private WeaponCatalog catalog;
    [SerializeField]
    private WeaponUnlockDefine defaultWeapon;

    private readonly HashSet<string> unlockedWeaponIds = new HashSet<string>(StringComparer.Ordinal);

    private IInventoryConsumption inventoryConsumption;

    private string mainWeaponId;
    private string reserveWeaponId;
    private bool isInitialized;

    public event Action<PlayerWeaponSnapshot> OnWeaponStateChanged;

    public bool IsInitialized => isInitialized;


    /// <summary>
    /// Service가 초기화되었는지 확인
    /// </summary>
    private void EnsureInitialized()
    {
        if (!isInitialized)
            throw new InvalidOperationException("Player Weapon Service가 초기화되지 않음");
    }

    /// <summary>
    /// 현재 상태 변경을 Snapshot으로 전달
    /// </summary>
    private void NotifyWeaponStateChanged()
    {
        OnWeaponStateChanged?.Invoke(CreateStateSnapshot());
    }

    /// <summary>
    /// 현재 해금 상태를 Catalog 순서로 복사
    /// </summary>
    /// <returns>Catalog 순서의 해금 Weapon ID 목록</returns>
    private string[] CreateOrderedUnlockedWeaponIds()
    {
        List<string> orderedIds = new List<string>();

        for (int i = 0; i < catalog.Definitions.Count; i++)
        {
            WeaponUnlockDefine definition = catalog.Definitions[i];
            string weaponId = definition.Weapon.ItemUID;

            if (unlockedWeaponIds.Contains(weaponId))
                orderedIds.Add(weaponId);
        }

        return orderedIds.ToArray();
    }

    /// <summary>
    /// 불러온 Snapshot의 Weapon ID가 현재 Catalog에 존재하는지 확인
    /// </summary>
    /// <param name="snapshot">확인할 무기 상태</param>
    /// <returns>Snapshot에서 불러온 Weapon ID 집합</returns>
    private HashSet<string> ValidateLoadedWeaponIds(
        PlayerWeaponSnapshot snapshot)
    {
        if (snapshot.UnlockedWeaponIds == null)
            throw new ArgumentException("불러온 Weapon Snapshot에 해금 목록이 없음", nameof(snapshot));

        HashSet<string> loadedIds = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < snapshot.UnlockedWeaponIds.Count; i++)
        {
            string weaponId = snapshot.UnlockedWeaponIds[i];

            if (!catalog.Contains(weaponId))
                throw new ArgumentException($"저장된 Weapon ID가 Catalog에 없음: {weaponId}", nameof(snapshot));

            loadedIds.Add(weaponId);
        }

        string defaultWeaponId = defaultWeapon.Weapon.ItemUID;

        if (!loadedIds.Contains(defaultWeaponId))
            throw new ArgumentException("저장된 상태에 Default Weapon 해금 정보가 없음", nameof(snapshot));

        return loadedIds;
    }

    /// <summary>
    /// 지정한 Weapon Slot 값이 유효한지 확인
    /// </summary>
    /// <param name="slot">확인할 Weapon Slot</param>
    /// <returns>Main 또는 Reserve면 true</returns>
    private bool IsValidSlot(WeaponSlot slot)
    {
        return slot == WeaponSlot.Main || slot == WeaponSlot.Reserve;
    }

    /// <summary>
    /// Main Weapon과 Reserve Weapon ID를 교환
    /// </summary>
    private void SwapWeaponIds()
    {
        string previousMainWeaponId = mainWeaponId;

        mainWeaponId = reserveWeaponId;
        reserveWeaponId = previousMainWeaponId;
    }

    /// <summary>
    /// 신규 플레이어의 기본 무기 상태를 초기화
    /// </summary>
    /// <param name="consumption">권한 Inventory 재료 소비 접근점</param>
    public void InitNewPlayer(IInventoryConsumption consumption)
    {
        if (isInitialized)
            throw new InvalidOperationException("Player Weapon Service가 이미 초기화됨");

        if (consumption == null)
            throw new ArgumentNullException(nameof(consumption));

        inventoryConsumption = consumption;

        unlockedWeaponIds.Clear();

        mainWeaponId = defaultWeapon.Weapon.ItemUID;
        reserveWeaponId = string.Empty;

        unlockedWeaponIds.Add(mainWeaponId);

        isInitialized = true;
    }

    /// <summary>
    /// 저장된 무기 상태로 플레이어 권한 상태를 초기화
    /// </summary>
    /// <param name="consumption">권한 Inventory 재료 소비 접근점</param>
    /// <param name="snapshot">불러온 플레이어 무기 상태</param>
    public void InitFromSnapshot(IInventoryConsumption consumption, PlayerWeaponSnapshot snapshot)
    {
        if (isInitialized)
            throw new InvalidOperationException("Player Weapon Service가 이미 초기화됨");

        if (consumption == null)
            throw new ArgumentNullException(nameof(consumption));

        HashSet<string> loadedIds = ValidateLoadedWeaponIds(snapshot);

        inventoryConsumption = consumption;

        unlockedWeaponIds.Clear();

        foreach (string weaponId in loadedIds)
            unlockedWeaponIds.Add(weaponId);

        mainWeaponId = snapshot.MainWeaponId;
        reserveWeaponId = snapshot.ReserveWeaponId;

        isInitialized = true;
    }

    /// <summary>
    /// 현재 Service 연결과 권한 상태를 초기화
    /// </summary>
    public void Unbind()
    {
        inventoryConsumption = null;

        unlockedWeaponIds.Clear();

        mainWeaponId = string.Empty;
        reserveWeaponId = string.Empty;

        isInitialized = false;
    }

    /// <summary>
    /// 현재 플레이어 무기 전체 상태를 생성
    /// </summary>
    /// <returns>현재 플레이어 무기 상태</returns>
    public PlayerWeaponSnapshot CreateStateSnapshot()
    {
        EnsureInitialized();

        return new PlayerWeaponSnapshot(mainWeaponId, reserveWeaponId, CreateOrderedUnlockedWeaponIds());
    }

    /// <summary>
    /// 지정한 무기를 재료를 소비해 해금
    /// </summary>
    /// <param name="weaponId">해금할 Weapon ID</param>
    /// <returns>무기 해금 결과</returns>
    public PlayerWeaponOperationResult TryUnlockWeapon(string weaponId)
    {
        EnsureInitialized();

        if (!catalog.TryGetUnlockDefine(weaponId, out WeaponUnlockDefine definition))
            return PlayerWeaponOperationResult.Failed(PlayerWeaponStopReason.InvalidWeapon);

        if (unlockedWeaponIds.Contains(weaponId))
            return PlayerWeaponOperationResult.Failed(PlayerWeaponStopReason.AlreadyUnlocked);

        if (!inventoryConsumption.TryConsume(definition.Requirements))
            return PlayerWeaponOperationResult.Failed(PlayerWeaponStopReason.NotEnoughMaterials);

        unlockedWeaponIds.Add(weaponId);

        NotifyWeaponStateChanged();

        return PlayerWeaponOperationResult.Succeeded();
    }

    /// <summary>
    /// 지정한 Station에서 해금된 무기를 Loadout 슬롯에 배치
    /// 반대 슬롯에 이미 배치된 무기를 선택하면 두 슬롯을 교환
    /// </summary>
    /// <param name="player">권한 영역에서 식별된 요청 플레이어</param>
    /// <param name="station">사용 중인 Loadout Station</param>
    /// <param name="slot">변경할 무기 슬롯</param>
    /// <param name="weaponId">배치할 Weapon ID</param>
    /// <returns>Loadout 변경 결과</returns>
    public PlayerWeaponOperationResult TrySetLoadout(GameObject player, WeaponSlot slot, string weaponId)
    {
        EnsureInitialized();

        if (player == null)
            throw new ArgumentNullException(nameof(player));

        if (!IsValidSlot(slot))
            return PlayerWeaponOperationResult.Failed(PlayerWeaponStopReason.InvalidSlot);

        if (!catalog.Contains(weaponId))
            return PlayerWeaponOperationResult.Failed(PlayerWeaponStopReason.InvalidWeapon);

        if (!unlockedWeaponIds.Contains(weaponId))
            return PlayerWeaponOperationResult.Failed(PlayerWeaponStopReason.NotUnlocked);

        if (slot == WeaponSlot.Main)
        {
            if (string.Equals(mainWeaponId, weaponId, StringComparison.Ordinal))
                return PlayerWeaponOperationResult.Failed(PlayerWeaponStopReason.AlreadyEquipped);

            if (string.Equals(reserveWeaponId,weaponId,StringComparison.Ordinal))
                SwapWeaponIds();
            else
                mainWeaponId = weaponId;
        }
        else
        {
            if (string.Equals(reserveWeaponId, weaponId, StringComparison.Ordinal))
                return PlayerWeaponOperationResult.Failed(PlayerWeaponStopReason.AlreadyEquipped);

            if (string.Equals(mainWeaponId, weaponId, StringComparison.Ordinal))
                SwapWeaponIds();
            else
                reserveWeaponId = weaponId;
        }

        NotifyWeaponStateChanged();

        return PlayerWeaponOperationResult.Succeeded();
    }

    public PlayerWeaponOperationResult TryClearLoadout(GameObject player, WeaponSlot slot)
    {
        EnsureInitialized();

        if (player == null)
            throw new ArgumentNullException(nameof(player));

        if (!IsValidSlot(slot))
            return PlayerWeaponOperationResult.Failed(PlayerWeaponStopReason.InvalidSlot);

        if (slot == WeaponSlot.Main)
        {
            if (string.IsNullOrEmpty(mainWeaponId))
                return PlayerWeaponOperationResult.Failed(PlayerWeaponStopReason.AlreadyEmpty);

            mainWeaponId = string.Empty;
        }
        else
        {
            if (string.IsNullOrEmpty(reserveWeaponId))
                return PlayerWeaponOperationResult.Failed(PlayerWeaponStopReason.AlreadyEmpty);

            reserveWeaponId = string.Empty;
        }

        NotifyWeaponStateChanged();
        return PlayerWeaponOperationResult.Succeeded();
    }

    /// <summary>
    /// Main Weapon과 Reserve Weapon을 교환
    /// </summary>
    /// <returns>무기 스왑 결과</returns>
    public PlayerWeaponOperationResult TrySwapWeapons()
    {
        EnsureInitialized();

        if (string.IsNullOrEmpty(mainWeaponId) && string.IsNullOrEmpty(reserveWeaponId))
            return PlayerWeaponOperationResult.Failed(PlayerWeaponStopReason.NoEquippedWeapon);

        SwapWeaponIds();
        NotifyWeaponStateChanged();

        return PlayerWeaponOperationResult.Succeeded();
    }
}