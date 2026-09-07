using UnityEngine;

/// <summary>
/// 탈것의 입력 상태를 속도와 회전으로 변환하고 Rigidbody에 적용
/// 입력 장치와 탑승 상태는 직접 확인하지 않음
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class VehicleMovement : MonoBehaviour
{
    [SerializeField]
    private VehicleDefine define;

    [Header("Ground")]
    [SerializeField]
    private bool checkGroundState;
    [SerializeField]
    private Transform groundCheck;
    [SerializeField]
    private LayerMask groundLayers;

    [SerializeField, Min(0.01f)]
    private float groundCheckRadius = 0.2f;
    [SerializeField, Min(0.01f)]
    private float groundProbeHeight = 0.25f;
    [SerializeField, Min(0f)]
    private float groundCheckDistance = 0.1f;
    [SerializeField, Range(0f, 89f)]
    private float maxGroundAngle = 60f;
    [SerializeField, Min(0f)]
    private float groundStateDelay = 0.1f;


    private Rigidbody rigid;
    private Vector2 moveInput;
    private float groundChangeTime;
    private bool boostHeld;
    private bool verticalThrustHeld;
    private bool isGrounded;
    private bool hasGroundState;

    public VehicleDefine Define => define;
    public float ForwardSpeed => Vector3.Dot(rigid.linearVelocity, transform.forward);
    public bool GroundCheckEnabled => checkGroundState;
    public bool HasGroundState => hasGroundState;
    public bool IsGrounded => isGrounded;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        UpdateGroundState();

        ApplyForwardMovement();
        ApplyRotation();
        ApplyVerticalThrust();
    }

    private void OnDisable()
    {
        ClearInput();

        hasGroundState = false;
        groundChangeTime = 0f;
    }

    /// <summary>
    /// 고정된 접지 기준점 아래에서 지면을 검사
    /// Trigger와 접지 경사 기준을 넘는 면은 제외
    /// </summary>
    /// <returns>허용 거리 안에 접지 가능한 면이 있으면 true</returns>
    private bool CheckGround()
    {
        // 시작 구가 지면과 겹치지 않도록 기준점보다 위에서 검사
        Vector3 origin = groundCheck.position + Vector3.up * (groundCheckRadius + groundProbeHeight);

        float distance = groundProbeHeight + groundCheckDistance;

        if (!Physics.SphereCast(origin, groundCheckRadius, Vector3.down, out RaycastHit hit, distance, groundLayers, QueryTriggerInteraction.Ignore))
            return false;

        float minimumGroundDot = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);

        return Vector3.Dot(hit.normal, Vector3.up) >= minimumGroundDot;
    }

    /// <summary>
    /// 최초 검사 결과는 즉시 설정하고 이후에는 변경된 판정이 일정 시간 유지될 때 반영
    /// 짧은 접촉 끊김이나 접촉 반복으로 전환 애니메이션이 재시작되는 현상을 완화
    /// </summary>
    private void UpdateGroundState()
    {
        if (!checkGroundState)
        {
            hasGroundState = false;
            groundChangeTime = 0f;
            return;
        }

        bool nextGrounded = CheckGround();

        if (!hasGroundState)
        {
            isGrounded = nextGrounded;
            hasGroundState = true;
            groundChangeTime = 0f;
            return;
        }

        if (nextGrounded == isGrounded)
        {
            groundChangeTime = 0f;
            return;
        }

        groundChangeTime += Time.fixedDeltaTime;

        if (groundChangeTime < groundStateDelay)
            return;

        isGrounded = nextGrounded;
        groundChangeTime = 0f;
    }

    /// <summary>
    /// 전후 입력에 맞는 목표 속도 계산
    /// </summary>
    /// <returns>현재 입력의 목표 전진 속도</returns>
    private float GetTargetSpeed()
    {
        float throttle = moveInput.y;

        if (Mathf.Abs(throttle) <= 0.01f)
            return 0f;

        if (throttle < 0f)
            return throttle * define.MaxReverseSpeed;

        float maximumSpeed = define.MaxForwardSpeed;

        if (boostHeld && define.HasCapability(VehicleCapability.Boost))
            maximumSpeed *= define.BoostSpeedMultiplier;

        return throttle * maximumSpeed;
    }

    /// <summary>
    /// 현재 속도와 목표 속도에 맞는 속도 변화량 선택
    /// </summary>
    /// <param name="currentSpeed">현재 전진 속도</param>
    /// <param name="targetSpeed">목표 전진 속도</param>
    /// <returns>초당 속도 변화량</returns>
    private float GetSpeedChangeRate(float currentSpeed, float targetSpeed)
    {
        if (Mathf.Abs(moveInput.y) <= 0.01f)
            return define.NaturalDeceleration;

        bool directionChanged = currentSpeed * targetSpeed < 0f;
        bool slowingDown = Mathf.Abs(targetSpeed) < Mathf.Abs(currentSpeed);

        if (directionChanged || slowingDown)
            return define.BrakeAcceleration;

        return define.Acceleration;
    }

    /// <summary>
    /// 현재 전진 속도를 목표 속도로 변경하고 Rigidbody에 적용
    /// </summary>
    private void ApplyForwardMovement()
    {
        float currentSpeed = ForwardSpeed;
        float targetSpeed = GetTargetSpeed();
        float changeRate = GetSpeedChangeRate(currentSpeed, targetSpeed);

        float nextSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, changeRate * Time.fixedDeltaTime);

        Vector3 verticalVelocity = Vector3.Project(rigid.linearVelocity, Vector3.up);

        rigid.linearVelocity = transform.forward * nextSpeed + verticalVelocity;
    }

    /// <summary>
    /// Vertical Thrust를 지원하는 탈것에 상승 가속 적용
    /// </summary>
    private void ApplyVerticalThrust()
    {
        if (!define.HasCapability(VehicleCapability.VerticalThrust))
            return;

        if (!verticalThrustHeld)
            return;

        float riseSpeed = Vector3.Dot(rigid.linearVelocity, Vector3.up);

        if (riseSpeed >= define.MaxRiseSpeed)
            return;

        float remainingAcceleration = (define.MaxRiseSpeed - riseSpeed) / Time.fixedDeltaTime;
        float acceleration = Mathf.Min(define.VerticalThrustAcceleration, remainingAcceleration);

        rigid.AddForce(Vector3.up * acceleration, ForceMode.Acceleration);
    }

    /// <summary>
    /// 현재 이동 방향과 속도에 맞게 탈것 회전 적용
    /// </summary>
    private void ApplyRotation()
    {
        float steer = moveInput.x;
        float currentSpeed = ForwardSpeed;

        if (Mathf.Abs(steer) <= 0.01f || Mathf.Abs(currentSpeed) <= 0.01f)
            return;

        float maximumSpeed = Mathf.Max(0.01f, define.MaxForwardSpeed);
        float speedRatio = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maximumSpeed);
        float movementDirection = currentSpeed >= 0f ? 1f : -1f;

        float turnAmount = steer * define.TurnSpeed * speedRatio * movementDirection * Time.fixedDeltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turnAmount, 0f);

        rigid.MoveRotation(rigid.rotation * turnRotation);
    }

    /// <summary>
    /// 현재 Frame의 탈것 이동 입력 저장
    /// </summary>
    /// <param name="input">좌우 회전과 전후 이동 입력</param>
    /// <param name="isBoostHeld">Boost 입력 유지 여부</param>
    public void SetInput(Vector2 input, bool isBoostHeld, bool isVerticalThrustHeld)
    {
        moveInput = Vector2.ClampMagnitude(input, 1f);
        boostHeld = isBoostHeld;
        verticalThrustHeld = isVerticalThrustHeld;
    }

    /// <summary>
    /// 현재 탈것 이동 입력 제거
    /// </summary>
    public void ClearInput()
    {
        moveInput = Vector2.zero;
        boostHeld = false;
        verticalThrustHeld = false;
    }
}