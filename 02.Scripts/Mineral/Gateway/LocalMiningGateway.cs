using System;
using UnityEngine;

/// <summary>
/// 채광 요청을 같은 프로세스의 권한 Service에 전달하고 완료 결과를 다음 Frame에 반환
/// </summary>
public sealed class LocalMiningGateway : MiningGateway
{
    [SerializeField]
    private LocalResponseDispatcher dispatcher;

    private GameObject miner;
    private MiningService miningService;
    private IInventoryItemReceiver itemReceiver;
    private int bindingVersion;

    /// <summary>
    /// 비활성화될 때 대기 중인 완료 응답 세대를 무효화
    /// </summary>
    private void OnDisable()
    {
        bindingVersion++;
    }

    /// <summary>
    /// 같은 프로세스의 권한 Mining Service에 채광 요청을 전달
    /// Inventory 변경 이벤트가 먼저 Queue에 등록되고 완료 결과가 마지막에 등록
    /// </summary>
    /// <param name="requestId">요청 식별자</param>
    /// <param name="holdItem">채광에 사용한 장비 Define</param>
    /// <param name="mineral">타격한 광물</param>
    /// <param name="hitPoint">광물 타격 지점</param>
    protected override void SendMiningRequest(int requestId, HoldItemDefine holdItem, MineralRuntime mineral, Vector3 hitPoint)
    {
        if (miningService == null)
            throw new InvalidOperationException("Local Mining Gateway가 Mining Service에 연결되지 않음");

        if (itemReceiver == null)
            throw new InvalidOperationException("Local Mining Gateway가 Inventory Item Receiver에 연결되지 않음");

        MiningResult result = miningService.TryMine(miner, itemReceiver, holdItem, mineral, hitPoint);

        int responseVersion = bindingVersion;

        dispatcher.Enqueue(() =>
        {
            if (responseVersion != bindingVersion)
                return;

            NotifyMiningCompleted(requestId, result);
        });
    }

    /// <summary>
    /// 같은 프로세스의 권한 Mining Service와 플레이어 Inventory 접근점을 연결
    /// </summary>
    /// <param name="service">연결할 권한 Mining Service</param>
    /// <param name="receiver">보상을 받을 Inventory 접근점</param>
    public void Bind(GameObject getMiner, MiningService service, IInventoryItemReceiver receiver)
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));

        if (receiver == null)
            throw new ArgumentNullException(nameof(receiver));

        miner = getMiner;
        miningService = service;
        itemReceiver = receiver;
        bindingVersion++;
    }

    /// <summary>
    /// 현재 Mining Service와 Inventory 접근점 연결 해제
    /// </summary>
    public void Unbind()
    {
        miner = null;
        miningService = null;
        itemReceiver = null;
        bindingVersion++;
    }
}