using System;
using UnityEngine;

public class LocalAttackEnergyGateway : AttackEnergyGateway
{
    private PlayerStatusService statusService;

    public void bind(PlayerStatusService service)
    {
        statusService = service;
    }

    public void UnBind()
    {
        statusService = null;
    }

    public override bool TryConsumeEnergy(float amount)
    {
        if (statusService == null)
            throw new InvalidOperationException("Local Attack Energy Gateway가 Player Status Service에 연결되지 않음");

        return statusService.TryConsumeEnergy(amount);
    }
}
