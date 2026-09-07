using System;
using UnityEngine;

/// <summary>
/// Player의 탈것 부착 전환 상태
/// </summary>
public enum VehicleMountState
{
    None,
    Entering,
    Mounted,
    Exiting
}

/// <summary>
/// Player의 탈것 부착 상태와 CharacterController 및 표시 상태 관리
/// 탑승 가능 여부와 Seat 점유 규칙은 판단하지 않음
/// </summary>
public sealed class PlayerVehicleAttachment : MonoBehaviour
{
    [SerializeField]
    private CharacterController controller;
    [SerializeField]
    private GameObject visualRoot;
    [SerializeField]
    private LayerMask exitCollisionMask = ~0;

    private Transform originalParent;
    private Vector3 originalLocalScale;
    private bool controllerWasEnabled;
    private bool visualRootWasActive;

    public VehicleMountState State { get; private set; }
    public VehicleSeat CurrentSeat { get; private set; }
    public bool IsVehicleMode => State != VehicleMountState.None;

    public event Action<VehicleMountState, VehicleSeat> OnStateChanged;    

    /// <summary>
    /// Player를 Vehicle에서 분리하고 기존 상태 복구
    /// </summary>
    /// <param name="exitPoint">Player가 이동할 하차 위치</param>
    private void Detach(Transform exitPoint)
    {
        SetState(VehicleMountState.Exiting);

        Quaternion exitRotation = GetExitRotation(exitPoint);

        transform.SetParent(originalParent, true);
        transform.SetPositionAndRotation(exitPoint.position, exitRotation);
        transform.localScale = originalLocalScale;

        visualRoot.SetActive(visualRootWasActive);
        controller.enabled = controllerWasEnabled;

        CurrentSeat = null;
        originalParent = null;

        SetState(VehicleMountState.None);
    }

    /// <summary>
    /// 하차 위치의 수평 회전값 생성
    /// </summary>
    /// <param name="exitPoint">회전에 사용할 하차 위치</param>
    /// <returns>기울기를 제거한 하차 회전값</returns>
    private Quaternion GetExitRotation(Transform exitPoint)
    {
        return Quaternion.Euler(0f, exitPoint.eulerAngles.y, 0f);
    }

    /// <summary>
    /// 현재 탑승 전환 상태를 변경 및 전달
    /// </summary>
    /// <param name="state">적용할 탑승 전환 상태</param>
    private void SetState(VehicleMountState state)
    {
        State = state;
        OnStateChanged?.Invoke(State, CurrentSeat);
    }

    /// <summary>
    /// 지정한 Vehicle Seat에 Player 부착
    /// </summary>
    /// <param name="seat">Player를 부착할 Vehicle Seat</param>
    /// <returns>부착 상태 전환에 성공하면 true</returns>
    public bool TryAttach(VehicleSeat seat)
    {
        if (seat == null)
            throw new ArgumentNullException(nameof(seat));

        if (State != VehicleMountState.None)
            return false;

        originalParent = transform.parent;
        originalLocalScale = transform.localScale;
        controllerWasEnabled = controller.enabled;
        visualRootWasActive = visualRoot.activeSelf;

        CurrentSeat = seat;
        SetState(VehicleMountState.Entering);

        controller.enabled = false;

        transform.SetParent(seat.DriverSeat, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = originalLocalScale;

        if (seat.HidePlayerOnMount)
            visualRoot.SetActive(false);

        SetState(VehicleMountState.Mounted);
        return true;
    }

    /// <summary>
    /// 지정한 하차 위치에 Player CharacterController를 배치할 공간이 있는지 확인
    /// </summary>
    /// <param name="exitPoint">확인할 하차 위치</param>
    /// <param name="ignoredRoot">충돌 검사에서 제외할 Vehicle Root</param>
    /// <returns>Player를 배치할 수 있으면 true</returns>
    public bool CanDetachAt(Transform exitPoint, Transform ignoredRoot)
    {
        if (exitPoint == null)
            throw new ArgumentNullException(nameof(exitPoint));

        if (ignoredRoot == null)
            throw new ArgumentNullException(nameof(ignoredRoot));

        Quaternion exitRotation = GetExitRotation(exitPoint);

        Vector3 currentScale = transform.lossyScale;
        Vector3 scaledCenter = Vector3.Scale(controller.center, currentScale);

        float height = controller.height * Mathf.Abs(currentScale.y);
        float radiusScale = Mathf.Max(Mathf.Abs(currentScale.x), Mathf.Abs(currentScale.z));
        float radius = controller.radius * radiusScale;

        radius = Mathf.Max(0.01f, radius - controller.skinWidth);

        float halfHeight = Mathf.Max(height * 0.5f, radius);
        float pointOffset = halfHeight - radius;

        Vector3 center = exitPoint.position + exitRotation * scaledCenter;
        Vector3 up = exitRotation * Vector3.up;

        Vector3 upperPoint = center + up * pointOffset;
        Vector3 lowerPoint = center - up * pointOffset;

        Collider[] overlaps = Physics.OverlapCapsule(upperPoint, lowerPoint, radius, exitCollisionMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider overlap = overlaps[i];

            if (overlap.transform.IsChildOf(transform))
                continue;

            if (overlap.transform.IsChildOf(ignoredRoot))
                continue;

            return false;
        }

        return true;
    }

    /// <summary>
    /// 안전한 하차 위치로 Player 부착 해제
    /// </summary>
    /// <param name="exitPoint">Player가 이동할 하차 위치</param>
    /// <param name="ignoredRoot">충돌 검사에서 제외할 Vehicle Root</param>
    /// <returns>하차에 성공하면 true</returns>
    public bool TryDetach(Transform exitPoint, Transform ignoredRoot)
    {
        if (State != VehicleMountState.Mounted)
            return false;

        if (!CanDetachAt(exitPoint, ignoredRoot))
            return false;

        Detach(exitPoint);
        return true;
    }

    /// <summary>
    /// 위치 충돌 검사 없이 Player 부착 해제
    /// </summary>
    /// <param name="exitPoint">Player가 이동할 하차 위치</param>
    /// <returns>부착 상태를 해제했으면 true</returns>
    public bool ForceDetach(Transform exitPoint)
    {
        if (exitPoint == null)
            throw new ArgumentNullException(nameof(exitPoint));

        if (State == VehicleMountState.None)
            return false;

        Detach(exitPoint);
        return true;
    }
}