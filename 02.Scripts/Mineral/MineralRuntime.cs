using System;
using UnityEngine;

/// <summary>
/// 월드에 존재하는 광물 하나의 체력과 남은 획득 수량을 보관
/// </summary>
[RequireComponent(typeof(OutlineVisual))]
public class MineralRuntime : MonoBehaviour, IWorldHoverTarget, IWorldInspectable
{
    [SerializeField]
    private MineralDefine define;
    [SerializeField, Min(1f)]
    private float mineralSize;

    private OutlineVisual visual;

    private float maxHealth;
    private float currentHealth;
    private int rewardAmount;
    private bool isInit;
    private bool isDepleted;
    private bool isCompleted;

    public event Action<MineralRuntime> OnCompleted;

    public MineralDefine Define => define;
    public float MineralSize => mineralSize;
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float Defense => define.Defense;
    public ItemDefine RewardItem => define.RewardItem;
    public int RewardAmount => rewardAmount;
    public bool IsDepleted => isDepleted;
    public bool IsCompleted => isCompleted;
    public string DisplayName => define.DisplayName;
    public string Description => mineralSize.ToString() + "등급";

    private void Awake()
    {
        visual = GetComponent<OutlineVisual>();
    }

    private void Start()
    {
        if (isInit)
            return;

        Init(mineralSize);
    }

    /// <summary>
    /// 광물 크기에 따른 획득 수량 계산
    /// </summary>
    /// <param name="size">계산에 사용할 광물 크기</param>
    /// <returns>광물 전체 획득 수량</returns>
    private int CalculateRewardAmount(float size)
    {
        float extraScale = Mathf.Max(0f, size - 1f);

        int extraAmount = Mathf.FloorToInt(extraScale / define.RewardScaleStep);

        return define.BaseRewardAmount + extraAmount;
    }

    /// <summary>
    /// 광물 크기에 따른 최대 체력과 획득 수량 초기화
    /// </summary>
    public void Init(float size)
    {
        mineralSize = Mathf.Max(1f, size);

        transform.localScale = Vector3.one * mineralSize;

        maxHealth = define.BaseHealth * mineralSize;

        currentHealth = maxHealth;

        rewardAmount = CalculateRewardAmount(mineralSize);

        isInit = true;
        isDepleted = false;
        isCompleted = false;

        SetHovered(false);
    }

    /// <summary>
    /// 계산된 피해를 현재 광물 체력에 적용
    /// </summary>
    /// <param name="damage">적용할 피해량</param>
    /// <param name="appliedDamage">실제로 적용된 피해량</param>
    /// <returns>피해가 적용되면 true</returns>
    public bool TryApplyDamage(float damage, out float appliedDamage)
    {
        appliedDamage = 0f;

        if (damage <= 0f)
            return false;

        if (isDepleted || isCompleted)
            return false;

        appliedDamage = Mathf.Min(currentHealth, damage);

        currentHealth -= appliedDamage;

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            isDepleted = true;
        }

        return true;
    }

    /// <summary>
    /// 체력과 남은 보상이 모두 소진된 광물 제거를 시도
    /// </summary>
    /// <returns>이번 호출로 광물 제거가 시작되면 true</returns>
    public bool TryComplete()
    {
        if (isCompleted)
            return false;

        if (!isDepleted)
            return false;

        isCompleted = true;

        OnCompleted?.Invoke(this);
        Destroy(gameObject);

        return true;
    }

    /// <summary>
    /// Pointer Hover에 따른 광물 Outline 표시 상태 변경
    /// </summary>
    /// <param name="hovered">Pointer Hover 여부</param>
    public void SetHovered(bool hovered)
    {
        visual.SetVisible(hovered);
    }
}
