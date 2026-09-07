using UnityEngine;

/// <summary>
/// Jetpack의 에너지 소비 의도를 권한 영역으로 전달하고 사용 가능 여부를 반환
/// </summary>
public abstract class JetpackEnergyGateway : MonoBehaviour
{
    /// <summary>
    /// Jetpack 사용에 필요한 에너지 소비를 권한 영역에 요청
    /// </summary>
    /// <param name="amount">소비할 에너지</param>
    /// <returns>현재 Frame의 Jetpack 사용이 허용되면 true</returns>
    public abstract bool TryConsumeEnergy(float amount);
}
