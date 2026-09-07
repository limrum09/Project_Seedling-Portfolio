using System;
using UnityEngine;

/// <summary>
/// 클라이언트가 인벤토리 표시를 위해 읽을 수 있는 상태를 제공
/// 인벤토리 상태를 직접 변경하지 않음
/// </summary>
public interface IInventoryReadAccess
{
    event Action<InventoryChangeSet> OnInventoryChange;

    int InvenSlotCount { get; }
    float CurrentInventoryWeight { get; }
    float NormalInventoryWeight { get; }
    float MaximumInventoryWeight { get; }

    InventorySlotSnapshot[] GetAllSnapshot();
}

/// <summary>
/// 클라이언트의 인벤토리 변경 의도를 권한 영역으로 전달
/// </summary>
public interface IInventoryCommandGateWay
{
    event Action<int, InventoryOperationResult> OnMoveSlotCompleted;
    event Action<int, InventoryOperationResult> OnDiscardSlotCompleted;

    /// <summary>
    /// 두 인벤토리 슬롯의 아이템 이동을 요청
    /// </summary>
    /// <param name="firstIndex">이동할 슬롯</param>
    /// <param name="secondIndex">도착할 슬롯</param>
    int RequestMoveSlot(int firstIndex, int secondIndex);

    /// <summary>
    /// 지정한 슬롯의 아이템 버리기 요청
    /// </summary>
    /// <param name="index">버릴 슬롯</param>
    int RequestDiscardSlot(int index);
}

/// <summary>
/// 인벤토리 읽기와 변경 요청에 대한 클라이언트 측 공통 접근점을 정의
/// 구체적인 Local 또는 Network 전달 방식은 하위 클래스에서 구현
/// </summary>
public abstract class InventoryGateway : MonoBehaviour, IInventoryCommandGateWay 
{
    private int nextRequestId = 1;

    public event Action<int, InventoryOperationResult> OnMoveSlotCompleted;
    public event Action<int, InventoryOperationResult> OnDiscardSlotCompleted;

    private int CreateRequestId()
    {
        int requestId = nextRequestId;

        if (nextRequestId == int.MaxValue)
            nextRequestId = 1;
        else
            nextRequestId++;

        return requestId;
    }

    private void ValidateRequestID(int requestId)
    {
        if (requestId <= 0)
        {
            Debug.LogError("Inventory Request ID가 이상함 " + requestId);
            return;
        }
    }

    private void ValidateOperationResult(InventoryOperationResult result)
    {
        if (result.Success && result.Reason != InventoryStopReason.None)
        {
            Debug.LogError("성공했지만 실패 사유가 있음");
            return;
        }
            

        if (!result.Success && result.Reason == InventoryStopReason.None)
        {
            Debug.LogError("실패했지만 사유가 없음");
            return;
        }   
    }

    protected abstract void SendMoveSlotRequest(int requestId, int firstIndex, int secondIndex);

    protected abstract void SendDiscardSlotRequest(int requestId, int index);

    protected void NotifyMoveSlotCompleted(int requestId, InventoryOperationResult result)
    {
        ValidateRequestID(requestId);
        ValidateOperationResult(result);

        OnMoveSlotCompleted?.Invoke(requestId, result);
    }

    protected void NotifyDiscardSlotCompleted(int requestId, InventoryOperationResult result)
    {
        ValidateRequestID(requestId);
        ValidateOperationResult(result);

        OnDiscardSlotCompleted?.Invoke(requestId, result);
    }

    public int RequestMoveSlot(int firstIndex, int secondIndex)
    {
        int requestId = CreateRequestId();

        SendMoveSlotRequest(requestId, firstIndex, secondIndex);

        return requestId;
    }

    public int RequestDiscardSlot(int index)
    {
        int requestId = CreateRequestId();

        SendDiscardSlotRequest(requestId, index);

        return requestId;
    }    
}
