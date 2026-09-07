using System;
using UnityEngine;

/// <summary>
/// 탈것이 지원하는 선택 기능
/// </summary>
[Flags]
public enum VehicleCapability
{
    None = 0,
    Boost = 1 << 0,
    Jump = 1 << 1,
    VerticalThrust = 1 << 2
}

/// <summary>
/// 탈것의 정적 이동 설정과 지원 기능 정의
/// </summary>
[CreateAssetMenu(fileName = "_VehicleDefine", menuName = "Project/Vehicle/Vehicle Define")]
public sealed class VehicleDefine : ScriptableObject
{
    [Header("Speed")]
    [SerializeField, Min(0f)]
    private float maxForwardSpeed = 12f;
    [SerializeField, Min(0f)]
    private float maxReverseSpeed = 5f;
    [SerializeField, Min(0f)]
    private float acceleration = 8f;
    [SerializeField, Min(0f)]
    private float brakeAcceleration = 14f;
    [SerializeField, Min(0f)]
    private float naturalDeceleration = 4f;
    [SerializeField, Min(0f)]
    private float turnSpeed = 80f;

    [Header("Vertical Thrust")]
    [SerializeField, Min(0f)]
    private float maxRiseSpeed = 8f;
    [SerializeField, Min(0f)]
    private float verticalThrustAcceleration = 20f;

    [Header("Capability")]
    [SerializeField]
    private VehicleCapability capabilities;
    [SerializeField, Min(1f)]
    private float boostSpeedMultiplier = 1.5f;

    public float MaxForwardSpeed => maxForwardSpeed;
    public float MaxReverseSpeed => maxReverseSpeed;
    public float Acceleration => acceleration;
    public float BrakeAcceleration => brakeAcceleration;
    public float NaturalDeceleration => naturalDeceleration;
    public float TurnSpeed => turnSpeed;
    public float MaxRiseSpeed => maxRiseSpeed;
    public float VerticalThrustAcceleration => verticalThrustAcceleration;
    public float BoostSpeedMultiplier => boostSpeedMultiplier;

    /// <summary>
    /// 지정한 탈것 기능 지원 여부 확인
    /// </summary>
    /// <param name="capability">확인할 탈것 기능</param>
    /// <returns>해당 기능을 지원하면 true</returns>
    public bool HasCapability(VehicleCapability capability)
    {
        return (capabilities & capability) != 0;
    }
}