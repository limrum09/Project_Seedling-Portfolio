using System;
using UnityEngine;

/// <summary>
/// 클라이언트의 소모품 사용 의도를 권한 영역으로 전달
/// </summary>
public interface IConsumableUseCommandGateway
{
    event Action<int, ConsumableUseResult> OnUseCompleted;

    /// <summary>
    /// 지정한 종류의 소모품 사용을 요청
    /// </summary>
    /// <param name="type">사용할 소모품 종류</param>
    /// <returns>요청 식별자</returns>
    int RequestUse(ItemConsumableType type);
}

/// <summary>
/// 소모품 사용 요청 식별자와 완료 결과 전달 방식을 정의
/// 구체적인 Local 또는 Network 전달 방식은 하위 클래스에서 구현
/// </summary>
public abstract class ConsumableUseGateway : MonoBehaviour, IConsumableUseCommandGateway
{
    private int nextRequestId = 1;

    public event Action<int, ConsumableUseResult> OnUseCompleted;

    /// <summary>
    /// 다음 소모품 사용 요청 식별자를 생성
    /// </summary>
    /// <returns>생성된 요청 식별자</returns>
    private int CreateRequestId()
    {
        int requestId = nextRequestId;

        if (nextRequestId == int.MaxValue)
            nextRequestId = 1;
        else
            nextRequestId++;

        return requestId;
    }

    /// <summary>
    /// 구체적인 전달 방식으로 소모품 사용 요청
    /// </summary>
    /// <param name="requestId">요청 식별자</param>
    /// <param name="type">사용할 소모품 종류</param>
    protected abstract void SendUseRequest(int requestId, ItemConsumableType type);

    /// <summary>
    /// 소모품 사용 완료 결과를 요청자에게 전달
    /// </summary>
    /// <param name="requestId">완료된 요청 식별자</param>
    /// <param name="result">소모품 사용 결과</param>
    protected void NotifyUseCompleted(int requestId, ConsumableUseResult result)
    {
        if (requestId <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestId));

        if (result.Success && result.Reason != ConsumableUseStopReason.None)
            throw new InvalidOperationException("성공한 소모품 사용 결과에 중단 사유가 있음");

        if (!result.Success && result.Reason == ConsumableUseStopReason.None)
            throw new InvalidOperationException("실패한 소모품 사용 결과에 중단 사유가 없음");

        OnUseCompleted?.Invoke(requestId, result);
    }

    /// <summary>
    /// 지정한 종류의 소모품 사용을 요청
    /// </summary>
    /// <param name="type">사용할 소모품 종류</param>
    /// <returns>요청 식별자</returns>
    public int RequestUse(ItemConsumableType type)
    {
        int requestId = CreateRequestId();

        SendUseRequest(requestId, type);

        return requestId;
    }
}
