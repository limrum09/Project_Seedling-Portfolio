using System;

/// <summary>
/// 같은 프로세스의 권한 PlayerStatusService에 Jetpack 에너지 소비를 전달
/// </summary>
public sealed class LocalJetpackEnergyGateway : JetpackEnergyGateway
{
    private PlayerStatusService statusService;

    /// <summary>
    /// 같은 프로세스의 권한 PlayerStatusService를 연결
    /// </summary>
    /// <param name="service">연결할 권한 PlayerStatusService</param>
    public void Bind(PlayerStatusService service)
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));

        statusService = service;
    }

    /// <summary>
    /// 현재 권한 PlayerStatusService 연결을 해제
    /// </summary>
    public void Unbind()
    {
        statusService = null;
    }

    /// <summary>
    /// 권한 PlayerStatusService에 Jetpack 에너지 소비를 요청
    /// </summary>
    /// <param name="amount">소비할 에너지</param>
    /// <returns>에너지를 전부 소비했으면 true</returns>
    public override bool TryConsumeEnergy(float amount)
    {
        if (statusService == null)
            throw new InvalidOperationException("Local Jetpack Energy Gateway가 Player Status Service에 연결되지 않음");

        return statusService.TryConsumeEnergy(amount);
    }
}
