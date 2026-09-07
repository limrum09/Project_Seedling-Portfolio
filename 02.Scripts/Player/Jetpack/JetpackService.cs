using UnityEngine;

/// <summary>
/// Jetpack 사용 입력에 따라 에너지 소비를 요청하고 이동용 활성 상태를 제공
/// </summary>
public sealed class JetpackService : MonoBehaviour
{
    [Header("Components")]
    [SerializeField]
    private JetpackEnergyGateway energyGateway;

    [Header("Jetpack")]
    [SerializeField, Min(0f)]
    private float energyConsumPerSecond = 10f;
    [SerializeField, Min(0f)]
    private float riseSpeed = 1f;
    [SerializeField, Min(0f)]
    private float riseAcceleration = 4f;

    public bool IsActive { get; private set; }
    public float RiseSpeed => riseSpeed;
    public float RiseAcceleration => riseAcceleration;

    /// <summary>
    /// 필수 JetpackEnergyGateway 참조가 연결되어 있는지 Editor에서 확인
    /// </summary>
    private void OnValidate()
    {
        if (energyGateway == null)
            Debug.LogError("Jetpack Service에 Jetpack Energy Gateway 참조가 없음", this);
    }

    /// <summary>
    /// 현재 입력에 필요한 에너지를 소비하고 Jetpack 활성 여부를 결정
    /// </summary>
    /// <param name="isRequest">Jetpack 사용 입력 여부</param>
    /// <param name="deltaTime">현재 Frame 경과 시간</param>
    /// <param name="multipulier">에너지 소비 배율</param>
    /// <returns>현재 Frame에 Jetpack을 사용할 수 있으면 true</returns>
    public bool TryUseJetPack(bool isRequest, float deltaTime, float multipulier = 1f)
    {
        IsActive = false;

        if (!isRequest)
            return false;

        float requiredEnergy = energyConsumPerSecond * deltaTime * multipulier;

        if (!energyGateway.TryConsumeEnergy(requiredEnergy))
            return false;

        IsActive = true;

        return true;
    }
}
