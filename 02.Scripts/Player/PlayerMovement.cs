using UnityEngine;

using System;

/// <summary>
/// Player 이동, 점프, 중력과 Jetpack 이동 계산
/// 입력 장치를 직접 확인하지 않음
/// </summary>
[RequireComponent (typeof(CharacterController))]
public sealed class PlayerMovement : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] 
    private CharacterController controller;
    [SerializeField]
    private PlayerFacing facing;
    [SerializeField]
    private JetpackService jetpackService;
    [SerializeField]
    private PlayerStats stats;

    [Header("Values")]
    [SerializeField]
    private float groundCheckDistance = 0.8f;
    [SerializeField]
    private LayerMask groundMask;

    [Header("Gravity")]
    [SerializeField]
    private float gravity = -20f;
    [SerializeField]
    private float riseGravityMultiplier = 0.65f;
    [SerializeField]
    private float apexVelocityRange = 0.5f;
    [SerializeField]
    private float apexGravityMultiplier = 0.2f;
    [SerializeField]
    private float fallGravityMultiplier = 0.5f;
    [SerializeField]
    private float maxFallSpeed = 9f;

    private Transform cameraTransform;
    private float multiplier = 1f;
    private float verticalVelocity;
    private bool stopPlayer;
    private bool isGameOver;

    public bool IsJetPackAcitve => jetpackService.IsActive;
    public float VerticalVelocity => verticalVelocity;
    public Vector2 AirMoveDirection { get; private set; }
    public bool IsCheckGournd { get; private set; }
    public bool JumpStartedThisFrame { get; private set; }
    public bool IsLanding { get; private set; }
    public float GroundDistance { get; private set; }

    /// <summary>
    /// Player 이동 상태와 필수 CharacterController 초기화
    /// </summary>
    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        isGameOver = false;
        stopPlayer = false;
    }

    /// <summary>
    /// 시작 위치의 지면 상태 확인
    /// </summary>
    private void Start()
    {
        IsCheckGournd = CheckGround();
    }

    /// <summary>
    /// 현재 Frame의 이동, 점프와 달리기 입력 처리
    /// </summary>
    /// <param name="moveInput">이동 방향 입력</param>
    /// <param name="jumpPressed">이번 Frame 점프 시작 여부</param>
    /// <param name="jumpHeld">점프 유지 여부</param>
    /// <param name="sprintHeld">달리기 유지 여부</param>
    public void Tick(Vector2 moveInput, bool jumpPressed, bool jumpHeld, bool sprintHeld)
    {
        JumpStartedThisFrame = false;
        IsLanding = false;

        if (isGameOver)
            return;

        if (stopPlayer)
            return;

        CheckPlayerJump(jumpPressed, jumpHeld);
        PlayerMove(moveInput, sprintHeld);
    }

    /// <summary>
    /// CharacterController와 지면 SphereCast를 이용해 접지 상태 확인
    /// </summary>
    /// <returns>이동 가능한 지면에 닿아 있으면 true</returns>
    private bool CheckGround()
    {
        GroundDistance = float.PositiveInfinity;

        if (verticalVelocity > 0)
            return false;

        if (controller.isGrounded)
        {
            GroundDistance = 0f;
            return true;
        }

        if (jetpackService.IsActive)
            return false;

        Vector3 center = transform.TransformPoint(controller.center);

        Vector3 checkSphereCenter = center - Vector3.up * (controller.height * 0.5f - controller.radius);

        float radius = controller.radius * 0.6f;
        float startOffset = 0.05f;

        bool foundGround = Physics.SphereCast(checkSphereCenter + Vector3.up * startOffset, radius, Vector3.down, out RaycastHit hit, groundCheckDistance + startOffset, groundMask, QueryTriggerInteraction.Ignore);

        if (!foundGround)
            return false;

        GroundDistance = hit.distance;

        float groundAndgle = Vector3.Angle(hit.normal, Vector3.up);

        return groundAndgle <= controller.slopeLimit;
    }

    /// <summary>
    /// 점프와 Jetpack 입력에 따라 수직 이동 상태 갱신
    /// </summary>
    /// <param name="jumpPressed">이번 Frame 점프 시작 여부</param>
    /// <param name="jumpHeld">점프 유지 여부</param>
    private void CheckPlayerJump(bool jumpPressed, bool jumpHeld)
    {
        bool wasGround = IsCheckGournd;

        IsCheckGournd = CheckGround();
        IsLanding = !wasGround && IsCheckGournd;

        if (IsCheckGournd)
        {
            jetpackService.TryUseJetPack(false, Time.deltaTime);

            if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }   

            if (jumpPressed)
            {
                float riseGravity = gravity * riseGravityMultiplier;
                verticalVelocity = Mathf.Sqrt(stats.JumpHeight * -2f * riseGravity);

                IsCheckGournd = false;
                JumpStartedThisFrame = true;
                IsLanding = false;
            }

            return;
        }

        bool isRequestJetpack = jumpHeld && verticalVelocity <= jetpackService.RiseSpeed;
        bool isJetPackActived = jetpackService.TryUseJetPack(isRequestJetpack, Time.deltaTime, multiplier);

        if (isJetPackActived)
        {
            verticalVelocity = Mathf.MoveTowards(verticalVelocity, jetpackService.RiseSpeed, jetpackService.RiseAcceleration* Time.deltaTime);
        }
        else
        {
            ApplyGravity();
        }
    }

    /// <summary>
    /// 현재 수직 속도 구간에 맞는 중력 적용
    /// </summary>
    private void ApplyGravity()
    {
        float gravityMutiplier;

        if (Mathf.Abs(verticalVelocity) <= apexVelocityRange)
            gravityMutiplier = apexGravityMultiplier;
        else if (verticalVelocity > 0f)
            gravityMutiplier = riseGravityMultiplier;
        else
            gravityMutiplier = fallGravityMultiplier;

        verticalVelocity += gravity * gravityMutiplier * Time.deltaTime;
        verticalVelocity = Mathf.Max(verticalVelocity, -maxFallSpeed);
    }

    /// <summary>
    /// 이동 방향과 달리기 입력을 실제 Player 이동에 적용
    /// </summary>
    /// <param name="moveInput">이동 방향 입력</param>
    /// <param name="sprintHeld">달리기 유지 여부</param>
    private void PlayerMove(Vector2 moveInput, bool sprintHeld)
    {
        float horizontal = moveInput.x;
        float vertical = moveInput.y;

        AirMoveDirection = moveInput;

        bool isMoveInput = Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f;
        float speed = sprintHeld ? stats.RunSpeed : stats.WalkSpeed;

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = cameraForward * vertical + cameraRight * horizontal;
        moveDirection.y = 0f;
        moveDirection.Normalize();

        facing.SetMovementDirection(horizontal, vertical, cameraForward);

        Vector3 velocity = moveDirection * speed;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);

        if (!isMoveInput)
        {
            multiplier = 1;
        }
        else
        {
            multiplier = sprintHeld ? 2.5f : 1.5f;
        }
    }

    /// <summary>
    /// 지면 감지 SphereCast 범위를 Scene Gizmo로 표시
    /// </summary>
    private void OnDrawGizmos()
    {
        CharacterController targetController = controller;

        if (targetController == null)
            targetController = GetComponent<CharacterController>();

        Vector3 center =
            transform.TransformPoint(targetController.center);

        Vector3 checkSphereCenter =
            center - Vector3.up *
            (targetController.height * 0.5f - targetController.radius);

        float radius = targetController.radius * 0.6f;
        float startOffset = 0.05f;

        Vector3 castOrigin =
            checkSphereCenter + Vector3.up * startOffset;

        float castDistance =
            groundCheckDistance + startOffset;

        Vector3 castEnd =
            castOrigin + Vector3.down * castDistance;

        bool foundGround = Physics.SphereCast(
            castOrigin,
            radius,
            Vector3.down,
            out RaycastHit hit,
            castDistance,
            groundMask,
            QueryTriggerInteraction.Ignore);

        Gizmos.color = foundGround
            ? Color.green
            : Color.red;

        // SphereCast 시작과 끝
        Gizmos.DrawWireSphere(castOrigin, radius);
        Gizmos.DrawWireSphere(castEnd, radius);

        // Sphere가 이동하는 범위
        Vector3 right = transform.right * radius;
        Vector3 forward = transform.forward * radius;

        Gizmos.DrawLine(
            castOrigin + right,
            castEnd + right);

        Gizmos.DrawLine(
            castOrigin - right,
            castEnd - right);

        Gizmos.DrawLine(
            castOrigin + forward,
            castEnd + forward);

        Gizmos.DrawLine(
            castOrigin - forward,
            castEnd - forward);

        // 실제 감지 위치
        if (foundGround)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(hit.point, 0.05f);
        }
    }

    /// <summary>
    /// Player 이동 처리 정지 여부 설정
    /// </summary>
    /// <param name="isStop">이동 처리 정지 여부</param>
    public void SetStopPlayer(bool isStop)
    {
        if (stopPlayer == isStop)
            return;

        stopPlayer = isStop;

        if (!stopPlayer)
            return;

        multiplier = 1f;
        verticalVelocity = 0f;

        AirMoveDirection = Vector2.zero;
        JumpStartedThisFrame = false;
        IsLanding = false;

        jetpackService.TryUseJetPack(false, 0f);
    }

    /// <summary>
    /// Player 이동 방향 계산에 사용할 World Camera 설정
    /// </summary>
    /// <param name="worldCamera">사용할 World Camera</param>
    public void SetCamera(Camera worldCamera)
    {
        if (worldCamera == null)
            throw new ArgumentNullException(nameof(worldCamera));

        cameraTransform = worldCamera.transform;
    }
}
