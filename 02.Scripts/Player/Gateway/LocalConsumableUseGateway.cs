using System;
using UnityEngine;

/// <summary>
/// 소모품 사용 요청을 같은 프로세스의 권한 Service에 전달하고 완료 결과를 다음 Frame에 반환
/// </summary>
public sealed class LocalConsumableUseGateway : ConsumableUseGateway
{
    [SerializeField]
    private LocalResponseDispatcher dispatcher;

    private ConsumableUseService consumableUseService;
    private int bindingVersion;

    /// <summary>
    /// 비활성화될 때 대기 중인 완료 응답 세대를 무효화
    /// </summary>
    private void OnDisable()
    {
        bindingVersion++;
    }

    private void OnValidate()
    {
        if (dispatcher == null)
            Debug.LogError("Local Consumable Use Gateway에 Local Response Dispatcher 참조가 없음", this);
    }

    /// <summary>
    /// 같은 프로세스의 권한 Service에 소모품 사용 요청을 전달
    /// Inventory와 Status 변경 이벤트가 먼저 Queue에 등록되고 완료 결과가 마지막에 등록
    /// </summary>
    /// <param name="requestId">요청 식별자</param>
    /// <param name="type">사용할 소모품 종류</param>
    protected override void SendUseRequest(int requestId, ItemConsumableType type)
    {
        if (consumableUseService == null)
            throw new InvalidOperationException("Local Consumable Use Gateway가 Consumable Use Service에 연결되지 않음");

        ConsumableUseResult result = consumableUseService.TryUse(type);
        int responseVersion = bindingVersion;

        dispatcher.Enqueue(() =>
        {
            if (responseVersion != bindingVersion)
                return;

            NotifyUseCompleted(requestId, result);
        });
    }

    /// <summary>
    /// 같은 프로세스의 권한 ConsumableUseService를 연결
    /// <param name="service">연결할 권한 ConsumableUseService</param>
    public void Bind(ConsumableUseService service)
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));

        consumableUseService = service;
        bindingVersion++;
    }

    /// <summary>
    /// 현재 권한 ConsumableUseService 연결 해제
    /// </summary>
    public void Unbind()
    {
        consumableUseService = null;
        bindingVersion++;
    }
}
