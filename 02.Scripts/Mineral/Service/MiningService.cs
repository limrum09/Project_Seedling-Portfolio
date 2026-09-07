using System;
using System.Collections.Generic;
using UnityEngine;

public class MiningService : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float minimumDamage;

    private readonly Dictionary<GameObject, float> nextMiningTimes = new Dictionary<GameObject, float>();

    /// <summary>
    /// 비활성화될 때 플레이어별 채광 쿨다운 상태 제거
    /// </summary>
    private void OnDisable()
    {
        nextMiningTimes.Clear();
    }

    /// <summary>
    /// 플레이어 위치와 실제 타격 지점 사이의 사거리 초과 여부 확인
    /// </summary>
    /// <param name="miner">채광을 요청한 플레이어</param>
    /// <param name="hitPoint">광물 타격 지점</param>
    /// <param name="attackRange">사용한 무기의 공격 사거리</param>
    /// <returns>공격 사거리를 초과하면 true</returns>
    private bool IsOutOfRange(GameObject miner, Vector3 hitPoint, float attackRange)
    {
        float distanceSqr = (miner.transform.position - hitPoint).sqrMagnitude;

        return distanceSqr > attackRange * attackRange;
    }

    /// <summary>
    /// 무기 공격력과 광물 방어력으로 실제 피해량 계산
    /// 공격력이 부족하면 설정된 최소 피해량 사용
    /// </summary>
    /// <param name="attackPower">무기 공격력</param>
    /// <param name="defense">광물 방어력</param>
    /// <returns>광물에 적용할 피해량</returns>
    private float CalculateDamage(float attackPower, float defense)
    {
        float damage = attackPower - defense;

        if (damage > 0f)
            return damage;

        return minimumDamage;
    }

    /// <summary>
    /// 체력이 소진된 광물 보상을 Inventory에 지급하고 추가되지 않은 수량은 폐기
    /// </summary>
    /// <param name="itemReceiver">보상을 받을 Inventory 접근점</param>
    /// <param name="mineral">보상을 제공할 광물</param>
    /// <param name="damageAmount">이번 요청에서 적용된 피해량</param>
    /// <returns>Inventory 지급 결과가 포함된 채굴 결과</returns>
    private MiningResult GiveReward(IInventoryItemReceiver itemReceiver, MineralRuntime mineral, float damageAmount)
    {
        ItemDefine rewardItem = mineral.RewardItem;

        int requestAmount = mineral.RewardAmount;

        InventoryAddResult inventoryResult = itemReceiver.TryAdd(rewardItem, requestAmount);

        bool completed = mineral.TryComplete();

        return MiningResult.Rewarded(damageAmount, rewardItem, inventoryResult, completed);
    }

    /// <summary>
    /// 플레이어 무기로 지정한 광물 채광을 시도
    /// </summary>
    /// <param name="miner">채광을 요청한 플레이어</param>
    /// <param name="itemReceiver">보상을 받을 Inventory 접근점</param>
    /// <param name="holdItem">채광에 사용한 장비 Define</param>
    /// <param name="mineral">타격한 광물</param>
    /// <param name="hitPoint">광물 타격 지점</param>
    /// <returns>피해와 Inventory 보상 처리 결과</returns>
    public MiningResult TryMine(GameObject miner, IInventoryItemReceiver itemReceiver, HoldItemDefine holdItem, MineralRuntime mineral, Vector3 hitPoint)
    {
        if (itemReceiver == null)
            throw new ArgumentNullException(nameof(itemReceiver));

        if (miner == null)
            return MiningResult.Failed(MiningStopReason.InvalidGatherer);

        if (mineral == null)
            return MiningResult.Failed(MiningStopReason.InvalidMineral);

        if (holdItem == null)
            return MiningResult.Failed(MiningStopReason.InvalidHoldItem);

        if (mineral.IsCompleted)
            return MiningResult.Failed(MiningStopReason.MineralCompleted);

        if (!holdItem.CanMine)
            return MiningResult.Failed(MiningStopReason.MiningNotAllowed);

        if (IsOutOfRange(miner, hitPoint, holdItem.AttackRange))
            return MiningResult.Failed(MiningStopReason.OutOfRange);

        if (nextMiningTimes.TryGetValue(miner, out float nextMiningTime) && Time.time < nextMiningTime)
            return MiningResult.Failed(MiningStopReason.Cooldown);

        nextMiningTimes[miner] = Time.time + holdItem.AttackCooldown;

        float appliedDamage = 0f;

        if (!mineral.IsDepleted)
        {
            float damage = CalculateDamage(holdItem.ItemDamage, mineral.Defense);

            if (damage <= 0f)
                return MiningResult.Failed(MiningStopReason.AttackPowerTooLow);

            if (!mineral.TryApplyDamage(damage, out appliedDamage))
                return MiningResult.Failed(MiningStopReason.MineralCompleted);

            if (!mineral.IsDepleted)
                return MiningResult.Damaged(appliedDamage);
        }

        return GiveReward(itemReceiver, mineral, appliedDamage);
    }
}
