using System;
using UnityEngine;

/// <summary>
/// Vehicle Seat 점유, Player 부착과 Vehicle 입력 연결 순서를 조율
/// </summary>
public sealed class VehicleMountService : MonoBehaviour
{
    [SerializeField]
    private VehicleSeat seat;
    [SerializeField]
    private VehicleInputController inputController;
    [SerializeField]
    private Transform vehicleRoot;

    /// <summary>
    /// Vehicle이 비활성화될 때 현재 탑승자를 강제로 하차
    /// </summary>
    private void OnDisable()
    {
        ForceExit();
    }

    /// <summary>
    /// 지정한 Player의 현재 Vehicle 탑승 시도
    /// </summary>
    /// <param name="player">탑승할 Player</param>
    /// <returns>탑승에 성공하면 true</returns>
    public bool TryEnter(Player player)
    {
        if (player == null)
            throw new ArgumentNullException(nameof(player));

        PlayerVehicleAttachment attachment = player.VehicleAttachment;

        if (attachment == null)
            throw new InvalidOperationException("Player에 Player Vehicle Attachment가 연결되지 않음");

        if (attachment.IsVehicleMode)
            return false;

        if (!seat.TryOccupy(attachment))
            return false;

        if (!attachment.TryAttach(seat))
        {
            seat.Release(attachment);
            return false;
        }

        inputController.Bind(player.MovementInputSource, player.ActionInputSource);
        return true;
    }

    /// <summary>
    /// 현재 Vehicle 탑승자의 안전한 하차 시도
    /// </summary>
    /// <returns>하차에 성공하면 true</returns>
    public bool TryExit()
    {
        PlayerVehicleAttachment attachment = seat.Occupant;

        if (attachment == null)
            return false;

        if (!attachment.TryDetach(seat.ExitPoint, vehicleRoot))
            return false;

        inputController.Unbind();
        seat.Release(attachment);

        return true;
    }

    /// <summary>
    /// 현재 Vehicle 탑승자를 충돌 검사 없이 강제로 하차
    /// </summary>
    public void ForceExit()
    {
        PlayerVehicleAttachment attachment = seat.Occupant;

        if (attachment == null)
        {
            inputController.Unbind();
            return;
        }

        inputController.Unbind();
        attachment.ForceDetach(seat.ExitPoint);
        seat.Release(attachment);
    }
}