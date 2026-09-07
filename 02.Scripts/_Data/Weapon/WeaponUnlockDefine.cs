using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 무기 하나와 해당 무기의 해금 조건을 정의
/// </summary>
[CreateAssetMenu(fileName = "_WeaponUnlockDefine", menuName = "Project/Weapon/Weapon Unlock Define")]
public sealed class WeaponUnlockDefine : ScriptableObject
{
    [SerializeField]
    private HoldItemDefine weapon;
    [SerializeField]
    private List<ItemRequirement> requirements = new List<ItemRequirement>();

    public HoldItemDefine Weapon => weapon;
    public IReadOnlyList<ItemRequirement> Requirements => requirements;

    /// <summary>
    /// 무기와 해금 요구 재료 설정을 Editor에서 확인
    /// </summary>
    private void OnValidate()
    {
        if (weapon == null)
        {
            Debug.LogError($"{name}에 Weapon이 없음", this);
        }
        else
        {
            if (weapon.Category != ItemCategory.Weapon)
            {
                Debug.LogError($"{weapon.name}의 Category가 Weapon이 아님", this);
            }

            if (string.IsNullOrWhiteSpace(weapon.ItemUID))
            {
                Debug.LogError($"{weapon.name}의 Item UID가 없음", this);
            }
        }

        if (requirements == null)
        {
            Debug.LogError($"{name}에 Requirements 목록이 없음", this);

            return;
        }

        for (int i = 0; i < requirements.Count; i++)
        {
            ItemRequirement requirement = requirements[i];

            if (requirement == null)
            {
                Debug.LogError($"{name}의 {i}번 Requirement가 없음", this);

                continue;
            }

            if (requirement.Item == null)
            {
                Debug.LogError($"{name}의 {i}번 요구 아이템이 없음", this);
            }

            if (requirement.Amount <= 0)
            {
                Debug.LogError($"{name}의 {i}번 요구 수량이 0 이하임", this);
            }
        }
    }
}