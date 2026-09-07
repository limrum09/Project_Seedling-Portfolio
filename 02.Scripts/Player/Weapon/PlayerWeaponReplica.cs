using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 사용할 수 있는 무기 슬롯 종류
/// </summary>
public enum WeaponSlot
{
    Main,
    Reserve
}

/// <summary>
/// 플레이어의 해금 무기와 현재 Loadout 전체 상태
/// </summary>
public readonly struct PlayerWeaponSnapshot
{
    private readonly IReadOnlyList<string> unlockedWeaponIds;

    public string MainWeaponId { get; }
    public string ReserveWeaponId { get; }
    public IReadOnlyList<string> UnlockedWeaponIds => unlockedWeaponIds;

    public PlayerWeaponSnapshot(string mainWeaponId, string reserveWeaponId, IReadOnlyList<string> getUnlockedWeaponIds)
    {
        if (getUnlockedWeaponIds == null)
            throw new ArgumentNullException(nameof(getUnlockedWeaponIds));

        string normalizedMainId = mainWeaponId ?? string.Empty;
        string normalizedReserveId = reserveWeaponId ?? string.Empty;

        string[] copiedIds = new string[getUnlockedWeaponIds.Count];

        HashSet<string> uniqueIds = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < getUnlockedWeaponIds.Count; i++)
        {
            string weaponId = getUnlockedWeaponIds[i];

            if (string.IsNullOrWhiteSpace(weaponId))
                throw new ArgumentException("해금된 Weapon ID 목록에 빈 값이 있음", nameof(getUnlockedWeaponIds));

            if (!uniqueIds.Add(weaponId))
                throw new ArgumentException($"해금된 Weapon ID가 중복됨: {weaponId}", nameof(getUnlockedWeaponIds));

            copiedIds[i] = weaponId;
        }

        if (!string.IsNullOrEmpty(normalizedMainId) && !uniqueIds.Contains(normalizedMainId))
            throw new ArgumentException("Main Weapon이 해금 목록에 없음", nameof(normalizedMainId));

        if (!string.IsNullOrEmpty(normalizedReserveId))
        {
            if (!uniqueIds.Contains(normalizedReserveId))
                throw new ArgumentException("Reserve Weapon이 해금 목록에 없음", nameof(reserveWeaponId));

            if (string.Equals(normalizedMainId, normalizedReserveId, StringComparison.Ordinal))
                throw new ArgumentException("Main Weapon과 Reserve Weapon이 같음", nameof(reserveWeaponId));
        }

        MainWeaponId = normalizedMainId;
        ReserveWeaponId = normalizedReserveId;
        unlockedWeaponIds = Array.AsReadOnly(copiedIds);
    }
}

/// <summary>
/// 클라이언트가 플레이어 무기 상태를 읽기 위한 접근점
/// </summary>
public interface IPlayerWeaponReadAccess
{
    event Action<PlayerWeaponSnapshot> OnWeaponStateChanged;

    bool IsInitialized { get; }
    string MainWeaponId { get; }
    string ReserveWeaponId { get; }

    /// <summary>
    /// 현재 플레이어의 전체 무기 상태를 반환
    /// </summary>
    /// <returns>현재 무기 상태 Snapshot</returns>
    PlayerWeaponSnapshot GetSnapshot();

    /// <summary>
    /// 지정한 무기가 해금되어 있는지 확인
    /// </summary>
    /// <param name="weaponId">확인할 Weapon ID</param>
    /// <returns>해금된 무기면 true</returns>
    bool IsWeaponUnlocked(string weaponId);
}

/// <summary>
/// 권한 영역에서 전달받은 플레이어 무기 상태를 클라이언트에 보관
/// </summary>
public sealed class PlayerWeaponReplica : MonoBehaviour, IPlayerWeaponReadAccess
{
    private string mainWeaponId;
    private string reserveWeaponId;

    private string[] unlockedWeaponIds = Array.Empty<string>();

    private HashSet<string> unlockedWeaponIdSet = new HashSet<string>(StringComparer.Ordinal);

    public event Action<PlayerWeaponSnapshot> OnWeaponStateChanged;

    public bool IsInitialized { get; private set; }

    public string MainWeaponId
    {
        get
        {
            EnsureInitialized();
            return mainWeaponId;
        }
    }

    public string ReserveWeaponId
    {
        get
        {
            EnsureInitialized();
            return reserveWeaponId;
        }
    }

    /// <summary>
    /// Replica가 전체 Snapshot을 적용했는지 확인
    /// </summary>
    private void EnsureInitialized()
    {
        if (!IsInitialized)
            throw new InvalidOperationException("Player Weapon Replica가 초기화되지 않음");
    }

    /// <summary>
    /// 현재 플레이어의 전체 무기 상태를 반환
    /// </summary>
    /// <returns>현재 무기 상태 Snapshot</returns>
    public PlayerWeaponSnapshot GetSnapshot()
    {
        EnsureInitialized();

        return new PlayerWeaponSnapshot(mainWeaponId, reserveWeaponId, unlockedWeaponIds);
    }

    /// <summary>
    /// 지정한 무기가 해금되어 있는지 확인
    /// </summary>
    /// <param name="weaponId">확인할 Weapon ID</param>
    /// <returns>해금된 무기면 true</returns>
    public bool IsWeaponUnlocked(string weaponId)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(weaponId))
            return false;

        return unlockedWeaponIdSet.Contains(weaponId);
    }

    /// <summary>
    /// 권한 영역에서 전달받은 전체 무기 상태를 적용
    /// </summary>
    /// <param name="snapshot">적용할 전체 무기 상태</param>
    public void ApplyFullSnapshot(PlayerWeaponSnapshot snapshot)
    {
        // readonly struct의 default 값은 생성자 검사를 거치지 않음
        if (snapshot.UnlockedWeaponIds == null)
            throw new ArgumentException("초기화되지 않은 Weapon Snapshot", nameof(snapshot));

        string[] nextUnlockedIds = new string[snapshot.UnlockedWeaponIds.Count];

        HashSet<string> nextUnlockedIdSet = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < snapshot.UnlockedWeaponIds.Count; i++)
        {
            string weaponId = snapshot.UnlockedWeaponIds[i];

            nextUnlockedIds[i] = weaponId;
            nextUnlockedIdSet.Add(weaponId);
        }

        // 완성된 다음 상태를 먼저 만든 뒤 한 번에 교체
        mainWeaponId = snapshot.MainWeaponId;
        reserveWeaponId = snapshot.ReserveWeaponId;
        unlockedWeaponIds = nextUnlockedIds;
        unlockedWeaponIdSet = nextUnlockedIdSet;

        IsInitialized = true;

        // 전달받은 Snapshot은 이미 불변이므로 다시 복사하지 않음
        OnWeaponStateChanged?.Invoke(snapshot);
    }
}