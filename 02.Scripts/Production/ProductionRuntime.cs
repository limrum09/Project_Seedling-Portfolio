using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 설치물이 보유한 생산 상태의 공통 조회 계약
/// </summary>
public interface IProductionSource
{
    /// <summary>
    /// 설치물의 생산 상태를 반환
    /// 생산 기능이 없는 설치물은 null
    /// </summary>
    ProductionRuntime Production { get; }
}

/// <summary>
/// 생산 항목 하나의 경과 시간과 현재 보관량을 관리
/// </summary>
/// <summary>
/// 생산 항목 하나의 경과 시간과 현재 보관량을 관리
/// 생산 또는 회수가 완료된 뒤 최종 보관량의 변경을 알림
/// </summary>
public sealed class ProductionOutputRuntime
{
    private readonly ProductionOutputDefine define;

    private float elapsedSeconds;
    private int storedAmount;

    public ProductionOutputDefine Define => define;
    public ItemDefine OutputItem => define.OutputItem;
    public float ElapsedSeconds => elapsedSeconds;
    public int StoredAmount => storedAmount;

    public bool IsFull => storedAmount >= define.MaxStoredAmount;
    public event Action OnStoredAmountChanged;

    internal ProductionOutputRuntime(ProductionOutputDefine getDefine)
    {
        define = getDefine;
    }

    /// <summary>
    /// 경과 시간을 반영하고 완료된 생산 주기만큼 아이템을 보관
    /// 최대 보관량에 도달하면 남은 경과 시간은 누적하지 않음
    /// 이번 처리로 보관량이 변경되면 마지막에 한 번 알림
    /// </summary>
    /// <param name="deltaTime">이번 처리에서 경과한 시간</param>
    internal void Advance(float deltaTime)
    {
        if (IsFull)
            return;

        int previousAmount = storedAmount;

        elapsedSeconds += deltaTime;

        while (elapsedSeconds >= define.IntervalSeconds && !IsFull)
        {
            elapsedSeconds -= define.IntervalSeconds;

            int remainingCapacity = define.MaxStoredAmount - storedAmount;
            int producedAmount = Mathf.Min(define.OutputAmount, remainingCapacity);

            storedAmount += producedAmount;
        }

        if (IsFull)
            elapsedSeconds = 0f;

        NotifyStoredAmountChanged(previousAmount);
    }

    /// <summary>
    /// 현재 보관량은 유지하고 생산 경과 시간만 초기화
    /// </summary>
    internal void ResetProgress()
    {
        elapsedSeconds = 0f;
    }

    /// <summary>
    /// 이번 회수에서 Inventory에 들어가지 않은 수량을 다시 보관
    /// 최종 보관량의 변경 알림은 회수 Service에서 복원 후 요청
    /// </summary>
    /// <param name="amount">이번 회수에서 Inventory에 들어가지 않은 수량</param>
    internal void RestoreStoredAmount(int amount)
    {
        storedAmount += amount;
    }

    /// <summary>
    /// 처리 전 보관량과 현재 보관량이 다르면 변경을 알림
    /// </summary>
    /// <param name="previousAmount">이번 처리를 시작하기 전 보관량</param>
    internal void NotifyStoredAmountChanged(int previousAmount)
    {
        if (storedAmount == previousAmount)
            return;

        OnStoredAmountChanged?.Invoke();
    }



    /// <summary>
    /// 현재 보관된 수량을 반환하고 보관량을 비움
    /// 진행 중인 생산 시간은 유지
    /// 회수 도중의 임시 상태이므로 이 메서드에서는 변경을 알리지 않음
    /// </summary>
    /// <returns>회수하기 전 보관되어 있던 수량</returns>
    public int TakeStoredAmount()
    {
        int amount = storedAmount;

        storedAmount = 0;

        return amount;
    }
}

/// <summary>
/// 설치물 하나가 사용하는 모든 생산 항목의 Runtime 상태를 관리
/// 반복 호출 시점과 시설 등록은 담당하지 않음
/// </summary>
public sealed class ProductionRuntime
{
    private readonly ProductionDefine define;
    private readonly List<ProductionOutputRuntime> outputs = new List<ProductionOutputRuntime>();

    public ProductionDefine Define => define;
    public IReadOnlyList<ProductionOutputRuntime> Outputs => outputs;

    public bool HasStoredItems
    {
        get
        {
            foreach (ProductionOutputRuntime output in outputs)
            {
                if (output.StoredAmount > 0)
                    return true;
            }

            return false;
        }
    }

    public ProductionRuntime(ProductionDefine getDefine)
    {
        if (getDefine == null)
            throw new ArgumentNullException(nameof(getDefine));

        define = getDefine;

        HashSet<string> outputItemIds = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < define.Outputs.Count; i++)
        {
            ProductionOutputDefine output = define.Outputs[i];

            outputs.Add(new ProductionOutputRuntime(output));
        }
    }

    /// <summary>
    /// 모든 생산 항목에 경과 시간을 전달
    /// </summary>
    /// <param name="deltaTime">이번 처리에서 경과한 시간</param>
    public void Advance(float deltaTime)
    {
        if (deltaTime < 0f)
            throw new ArgumentOutOfRangeException(nameof(deltaTime));

        if (deltaTime == 0f)
            return;

        foreach (ProductionOutputRuntime output in outputs)
        {
            output.Advance(deltaTime);
        }
    }

    /// <summary>
    /// 모든 생산 항목의 경과 시간을 초기화
    /// 현재 보관량은 유지
    /// </summary>
    public void ResetProgress()
    {
        foreach (ProductionOutputRuntime output in outputs)
        {
            output.ResetProgress();
        }
    }
}