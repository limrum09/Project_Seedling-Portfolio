using System;
using UnityEngine;

/// <summary>
/// 같은 프로세스의 권한 PlayerStatusService 상태를 다음 Frame에 Replica로 전달
/// </summary>
public sealed class LocalPlayerStatusBridge : PlayerStatusBridge
{
    [SerializeField]
    private LocalResponseDispatcher dispatcher;

    private PlayerStatusService statusService;
    private int bindingVersion;

    private void OnEnable()
    {
        if (statusService == null)
            return;

        SubscribeAndApplyFullSnapshot();
    }

    private void OnDisable()
    {
        Unsubscribe();
        bindingVersion++;
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        if (dispatcher == null)
            Debug.LogError("Local Player Status Bridge에 Local Response Dispatcher 참조가 없음", this);
    }

    /// <summary>
    /// 현재 권한 상태 전체를 적용한 뒤 변경 이벤트를 구독
    /// </summary>
    private void SubscribeAndApplyFullSnapshot()
    {
        ApplyFullSnapshot(statusService.CreateStateSnapshot());
        statusService.OnStatusChanged += HandleStatusChanged;
    }

    /// <summary>
    /// 현재 권한 상태의 변경 이벤트 구독을 해제.
    /// </summary>
    private void Unsubscribe()
    {
        if (statusService == null)
            return;

        statusService.OnStatusChanged -= HandleStatusChanged;
    }

    /// <summary>
    /// 권한 상태 변경값을 공용 응답 Queue에 등록
    /// </summary>
    /// <param name="changeSet">전달할 플레이어 자원 변경값</param>
    private void HandleStatusChanged(PlayerStatusChangeSet changeSet)
    {
        int responseVersion = bindingVersion;

        dispatcher.Enqueue(() =>
        {
            if (responseVersion != bindingVersion)
                return;

            ApplyChangeSet(changeSet);
        });
    }

    /// <summary>
    /// 같은 프로세스의 권한 PlayerStatusService를 연결
    /// </summary>
    /// <param name="service">연결할 권한 PlayerStatusService</param>
    public void Bind(PlayerStatusService service)
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));

        Unsubscribe();

        statusService = service;
        bindingVersion++;

        if (!isActiveAndEnabled)
            return;

        SubscribeAndApplyFullSnapshot();
    }

    public void Unbind()
    {
        Unsubscribe();

        statusService = null;
        bindingVersion++;
    }
}
