using UnityEngine;

/// <summary>
/// Vehicle의 실제 이동 방향을 Animator 상태값으로 전달
/// </summary>
/// <summary>
/// Vehicle의 이동 방향과 접지 상태를 선택한 Animator 파라미터에 전달
/// 실제 이동과 접지 판정은 VehicleMovement에서 담당
/// </summary>
public sealed class VehicleAnimation : MonoBehaviour
{
    private static readonly int moveDirectionHash = Animator.StringToHash("MoveDirection");
    private static readonly int isGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int groundStateHash = Animator.StringToHash("Base Layer.Ground");
    private static readonly int flyingStateHash = Animator.StringToHash("Base Layer.Flying");

    [SerializeField]
    private VehicleMovement movement;
    [SerializeField]
    private Animator anim;
    [SerializeField, Min(0f)]
    private float movementThreshold = 0.05f;
    [SerializeField]
    private bool isAnim = true;

    [Header("Animator Parameters")]
    [SerializeField]
    private bool useMoveDirection = true;
    [SerializeField]
    private bool useGroundState;

    private bool groundAnimationInitialized;

    /// <summary>
    /// 활성화 시 현재 접지 상태에 맞는 초기 자세 설정을 준비
    /// </summary>
    private void OnEnable()
    {
        groundAnimationInitialized = false;
    }

    /// <summary>
    /// 사용하도록 설정한 Animator 파라미터에 현재 Vehicle 상태를 전달
    /// </summary>
    private void Update()
    {
        if (!isAnim)
        {
            groundAnimationInitialized = false;
            return;
        }

        if (useMoveDirection)
            anim.SetFloat(moveDirectionHash, GetMoveDirection());

        if (useGroundState)
            UpdateGroundAnimation();
    }

    /// <summary>
    /// 현재 Vehicle 속도에서 전진, 정지 또는 후진 방향 반환
    /// </summary>
    /// <returns>전진은 1, 정지는 0, 후진은 -1</returns>
    private float GetMoveDirection()
    {
        float speed = movement.ForwardSpeed;

        if (speed > movementThreshold)
            return 1f;

        if (speed < -movementThreshold)
            return -1f;

        return 0f;
    }

    /// <summary>
    /// 접지 상태를 Animator에 전달
    /// 최초 연결 또는 재활성화 시 현재 접지 상태에 맞는 유지 상태에서 시작
    /// 이미 공중에 있으면 이륙 전환을 재생하지 않고 Flying으로 진입
    /// </summary>
    private void UpdateGroundAnimation()
    {
        if (!movement.HasGroundState)
        {
            groundAnimationInitialized = false;
            return;
        }

        bool grounded = movement.IsGrounded;

        anim.SetBool(isGroundedHash, grounded);

        if (groundAnimationInitialized)
            return;

        groundAnimationInitialized = true;

        if (grounded)
            anim.Play(groundStateHash, 0, 0f);
        else
            anim.Play(flyingStateHash, 0, 0f);
    }
}