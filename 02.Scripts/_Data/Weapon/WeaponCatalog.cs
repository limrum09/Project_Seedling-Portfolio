using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임에서 사용할 무기 해금 정의 목록을 관리
/// </summary>
[CreateAssetMenu(fileName = "_WeaponCatalog", menuName = "Project/Weapon/Weapon Catalog")]
public sealed class WeaponCatalog : ScriptableObject
{
    [SerializeField]
    private List<WeaponUnlockDefine> definitions = new List<WeaponUnlockDefine>();

    public IReadOnlyList<WeaponUnlockDefine> Definitions => definitions;

    /// <summary>
    /// Catalog의 빈 항목과 중복 Weapon ID를 Editor에서 확인
    /// </summary>
    private void OnValidate()
    {
        if (definitions == null)
        {
            Debug.LogError($"{name}에 Definitions 목록이 없음", this);

            return;
        }

        HashSet<string> weaponIds = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < definitions.Count; i++)
        {
            WeaponUnlockDefine definition = definitions[i];

            if (definition == null)
            {
                Debug.LogError($"{name}의 {i}번 Unlock Define이 없음", this);

                continue;
            }

            HoldItemDefine weapon = definition.Weapon;

            if (weapon == null || string.IsNullOrWhiteSpace(weapon.ItemUID))
            {
                continue;
            }

            if (!weaponIds.Add(weapon.ItemUID))
            {
                Debug.LogError($"{name}에 Weapon ID가 중복됨: {weapon.ItemUID}", this);
            }
        }
    }

    /// <summary>
    /// 지정한 Weapon ID에 해당하는 해금 정의를 검색
    /// </summary>
    /// <param name="weaponId">검색할 Weapon ID</param>
    /// <param name="definition">검색된 무기 해금 정의</param>
    /// <returns>해당 정의를 찾았으면 true</returns>
    public bool TryGetUnlockDefine(string weaponId, out WeaponUnlockDefine definition)
    {
        definition = null;

        if (string.IsNullOrWhiteSpace(weaponId))
            return false;

        for (int i = 0; i < definitions.Count; i++)
        {
            WeaponUnlockDefine current = definitions[i];

            if (current == null || current.Weapon == null)
                continue;

            if (!string.Equals(current.Weapon.ItemUID, weaponId, StringComparison.Ordinal))
                continue;

            definition = current;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 지정한 Weapon ID가 Catalog에 등록되어 있는지 확인
    /// </summary>
    /// <param name="weaponId">확인할 Weapon ID</param>
    /// <returns>등록된 Weapon ID면 true</returns>
    public bool Contains(string weaponId)
    {
        return TryGetUnlockDefine(weaponId, out _);
    }
}