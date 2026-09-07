using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Inventory 요청을 같은 프로세스의 권한 Service에 전달하고 상태와 결과를 다음 Frame에 반환
/// </summary>
public sealed class LocalInventoryGateway : InventoryGateway
{
    [SerializeField]
    private LocalResponseDispatcher dispatcher;
    [FormerlySerializedAs("InventoryReplica")]
    [SerializeField]
    private InventoryReplica inventoryReplica;

    private InventoryService inventoryService;
    private int bindingVersion;

    private void OnEnable()
    {
        if (inventoryService == null)
            return;

        SubscribeAndApplyFullSnapshot();
    }

    private void OnDisable()
    {
        Unsubscribe();
        bindingVersion++;
    }

    private void OnValidate()
    {
        if (dispatcher == null)
            Debug.LogError("Local Inventory Gateway에 Local Response Dispatcher 참조가 없음", this);

        if (inventoryReplica == null)
            Debug.LogError("Local Inventory Gateway에 Inventory Replica 참조가 없음", this);
    }

    /// <summary>
    /// 현재 권한 Inventory 전체 상태를 적용한 뒤 변경 이벤트를 구독
    /// </summary>
    private void SubscribeAndApplyFullSnapshot()
    {
        inventoryReplica.ApplyFullSnapshot(inventoryService.CreateStateSnapshot());
        inventoryService.OnInventoryChange += HandleInventoryChange;
    }

    /// <summary>
    /// 현재 권한 Inventory 변경 이벤트 구독 해제
    /// </summary>
    private void Unsubscribe()
    {
        if (inventoryService == null)
            return;

        inventoryService.OnInventoryChange -= HandleInventoryChange;
    }

    /// <summary>
    /// 권한 영역의 변경 목록과 클라이언트 전달 목록이 같은 배열을 공유하지 않도록 복사
    /// </summary>
    /// <param name="changeSet">복사할 Inventory 변경 상태</param>
    /// <returns>독립된 슬롯 배열을 가진 Inventory 변경 상태</returns>
    private InventoryChangeSet CopyChangeSet(InventoryChangeSet changeSet)
    {
        if (changeSet.Snapshots == null)
            throw new ArgumentException("Inventory Change Set에 슬롯 목록이 없음", nameof(changeSet));

        InventorySlotSnapshot[] copiedSlots = new InventorySlotSnapshot[changeSet.Snapshots.Count];

        for (int i = 0; i < copiedSlots.Length; i++)
        {
            copiedSlots[i] = changeSet.Snapshots[i];
        }

        return new InventoryChangeSet(copiedSlots, changeSet.CurrentInventoryWeight);
    }

    /// <summary>
    /// 권한 Inventory 변경 상태를 공용 응답 Queue에 등록
    /// </summary>
    /// <param name="changeSet">전달할 Inventory 변경 상태</param>
    private void HandleInventoryChange(InventoryChangeSet changeSet)
    {
        InventoryChangeSet copiedChangeSet = CopyChangeSet(changeSet);
        int responseVersion = bindingVersion;

        dispatcher.Enqueue(() =>
        {
            if (responseVersion != bindingVersion)
                return;

            inventoryReplica.ApplyChangeSet(copiedChangeSet);
        });
    }

    /// <summary>
    /// 같은 프로세스의 권한 InventoryService에 슬롯 버리기 요청을 전달
    /// </summary>
    /// <param name="requestId">요청 식별자</param>
    /// <param name="index">버릴 슬롯 인덱스</param>
    protected override void SendDiscardSlotRequest(int requestId, int index)
    {
        if (inventoryService == null)
            throw new InvalidOperationException("Local Inventory Gateway가 Inventory Service에 연결되지 않음");

        InventoryOperationResult result = inventoryService.TryDiscardSlot(index);
        int responseVersion = bindingVersion;

        dispatcher.Enqueue(() =>
        {
            if (responseVersion != bindingVersion)
                return;

            NotifyDiscardSlotCompleted(requestId, result);
        });
    }

    /// <summary>
    /// 같은 프로세스의 권한 InventoryService에 슬롯 이동 요청을 전달
    /// </summary>
    /// <param name="requestId">요청 식별자</param>
    /// <param name="firstIndex">이동을 시작할 슬롯 인덱스</param>
    /// <param name="secondIndex">도착할 슬롯 인덱스</param>
    protected override void SendMoveSlotRequest(int requestId, int firstIndex, int secondIndex)
    {
        if (inventoryService == null)
            throw new InvalidOperationException("Local Inventory Gateway가 Inventory Service에 연결되지 않음");

        InventoryOperationResult result = inventoryService.TryMoveSlot(firstIndex, secondIndex);
        int responseVersion = bindingVersion;

        dispatcher.Enqueue(() =>
        {
            if (responseVersion != bindingVersion)
                return;

            NotifyMoveSlotCompleted(requestId, result);
        });
    }

    public void Bind(InventoryService service)
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));

        Unsubscribe();

        inventoryService = service;
        bindingVersion++;

        if (!isActiveAndEnabled)
            return;

        SubscribeAndApplyFullSnapshot();
    }

    public void Unbind()
    {
        Unsubscribe();

        inventoryService = null;
        bindingVersion++;
    }
}
