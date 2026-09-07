using System;
using UnityEngine;

/// <summary>
/// 탈것 운전석의 위치, 상호작용과 현재 탑승자 점유 상태 관리
/// Player 부착과 조작 권한 변경은 직접 처리하지 않음
/// </summary>
public sealed class VehicleSeat : MonoBehaviour, IPlayerInteractable
{
    [Header("Service")]
    [SerializeField]
    private VehicleMountService mountService;

    [Header("Position")]
    [SerializeField]
    private Transform driverSeat;
    [SerializeField]
    private Transform exitPoint;
    [SerializeField]
    private Transform cameraTarget;

    [Header("Player")]
    [SerializeField]
    private bool hidePlayerOnMount = true;

    private PlayerVehicleAttachment occupant;
    private PlayerInteraction registeredInteraction;
    private int interactionEnabledFrame;

    public bool CanInteraction => isActiveAndEnabled && occupant == null && Time.frameCount >= interactionEnabledFrame;
    public Transform DriverSeat => driverSeat;
    public Transform ExitPoint => exitPoint;
    public Transform CameraTarget => cameraTarget;
    public bool HidePlayerOnMount => hidePlayerOnMount;
    public PlayerVehicleAttachment Occupant => occupant;

    /// <summary>
    /// Player가 운전석 상호작용 범위에 들어오면 등록
    /// </summary>
    /// <param name="other">범위에 들어온 Collider</param>
    private void OnTriggerEnter(Collider other)
    {
        PlayerInteraction interaction = other.GetComponentInParent<PlayerInteraction>();

        if (interaction == null)
            return;

        registeredInteraction = interaction;
        registeredInteraction.RegistInteract(this);
    }

    /// <summary>
    /// Player가 운전석 상호작용 범위에서 나가면 등록 해제
    /// </summary>
    /// <param name="other">범위에서 나간 Collider</param>
    private void OnTriggerExit(Collider other)
    {
        PlayerInteraction interaction = other.GetComponentInParent<PlayerInteraction>();

        if (interaction == null || !ReferenceEquals(registeredInteraction, interaction))
            return;

        registeredInteraction.UnRegisterInteract(this);
        registeredInteraction = null;
    }

    /// <summary>
    /// 운전석이 비활성화될 때 남아 있는 Player 상호작용 등록 해제
    /// </summary>
    private void OnDisable()
    {
        if (registeredInteraction == null)
            return;

        registeredInteraction.UnRegisterInteract(this);
        registeredInteraction = null;
    }

    /// <summary>
    /// 상호작용한 Player의 탑승을 VehicleMountService에 요청
    /// </summary>
    /// <param name="context">상호작용한 Player 정보</param>
    public void Interact(PlayerInteractionContext context)
    {
        if (!context.PlayerObject.TryGetComponent(out Player player))
            throw new InvalidOperationException("Vehicle Seat와 상호작용한 Object에 Player가 없음");

        mountService.TryEnter(player);
    }

    /// <summary>
    /// 비어 있는 운전석을 지정한 Player가 점유
    /// </summary>
    /// <param name="attachment">탑승할 Player 부착 상태</param>
    /// <returns>운전석 점유에 성공하면 true</returns>
    public bool TryOccupy(PlayerVehicleAttachment attachment)
    {
        if (attachment == null)
            throw new ArgumentNullException(nameof(attachment));

        if (!CanInteraction)
            return false;

        occupant = attachment;
        return true;
    }

    /// <summary>
    /// 현재 Player의 운전석 점유 상태 해제
    /// </summary>
    /// <param name="attachment">점유를 해제할 Player 부착 상태</param>
    public void Release(PlayerVehicleAttachment attachment)
    {
        if (!ReferenceEquals(occupant, attachment))
            throw new InvalidOperationException("Vehicle Seat 점유자와 해제 요청 Player가 일치하지 않음");

        occupant = null;

        // 하차 입력이 같은 Frame에 다시 탑승 입력으로 처리되는 것을 방지
        interactionEnabledFrame = Time.frameCount + 1;
    }
}