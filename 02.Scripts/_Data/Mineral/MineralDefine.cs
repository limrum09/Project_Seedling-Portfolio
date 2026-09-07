using UnityEngine;

/// <summary>
/// 광물 종류별 기본 체력, 방어력과 획득 아이템 설정을 제공
/// </summary>
[CreateAssetMenu(fileName = "_Define", menuName = "Project/Mineral/Mineral Definition")]
public class MineralDefine : ScriptableObject
{
    [Header("Info")]
    [SerializeField]
    private string mineralUID;
    [SerializeField]
    private string displayName;

    [Header("Mineral")]
    [SerializeField, Min(0.1f)]
    private float baseHealth = 10f;
    [SerializeField, Min(0f)]
    private float defense;

    [Header("Reward")]
    [SerializeField]
    private ItemDefine rewardItem;
    [SerializeField, Min(1)]
    private int baseRewardAmount = 1;
    [SerializeField, Min(0.1f)]
    private float rewardScaleStep = 2f;

    public string MineralUID => mineralUID;
    public string DisplayName => displayName;
    public float BaseHealth => baseHealth;
    public float Defense => defense;
    public ItemDefine RewardItem => rewardItem;
    public int BaseRewardAmount => baseRewardAmount;
    public float RewardScaleStep => rewardScaleStep;
}
