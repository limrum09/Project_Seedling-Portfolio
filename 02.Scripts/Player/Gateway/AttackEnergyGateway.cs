using UnityEngine;

public abstract class AttackEnergyGateway : MonoBehaviour
{
    public abstract bool TryConsumeEnergy(float amount);
}
