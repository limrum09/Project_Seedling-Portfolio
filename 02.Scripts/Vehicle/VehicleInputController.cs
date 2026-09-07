using System;
using UnityEngine;

/// <summary>
/// 탑승한 Player의 기존 입력을 VehicleMovement와 하차 요청으로 전달
/// </summary>
public sealed class VehicleInputController : MonoBehaviour
{
    [SerializeField]
    private VehicleMovement movement;
    [SerializeField]
    private VehicleMountService mountService;

    private IPlayerMovementInputSource movementInput;
    private IPlayerActionInputSource actionInput;
    private bool isBound;
    private int exitEnabledFrame;

    /// <summary>
    /// 탑승 중인 Player 입력을 Vehicle에 전달
    /// </summary>
    private void Update()
    {
        if (!isBound)
            return;

        if (Time.frameCount >= exitEnabledFrame && actionInput.InteractPressedThisFrame)
        {
            if (mountService.TryExit())
                return;
        }

        Vector2 input = movementInput.Move;

        if (input.sqrMagnitude > 0.01f)
            Debug.Log($"Vehicle input {input}");

        movement.SetInput(input, movementInput.SprintHeld, movementInput.JumpHeld);
    }

    /// <summary>
    /// Vehicle Input Controller가 비활성화될 때 입력 연결 해제
    /// </summary>
    private void OnDisable()
    {
        Unbind();
    }

    /// <summary>
    /// 탑승한 Player의 이동과 기능 입력 연결
    /// </summary>
    /// <param name="getMovementInput">Player 이동 입력 접근점</param>
    /// <param name="getActionInput">Player 기능 입력 접근점</param>
    public void Bind(IPlayerMovementInputSource getMovementInput, IPlayerActionInputSource getActionInput)
    {
        if (getMovementInput == null)
            throw new ArgumentNullException(nameof(getMovementInput));

        if (getActionInput == null)
            throw new ArgumentNullException(nameof(getActionInput));

        Unbind();

        movementInput = getMovementInput;
        actionInput = getActionInput;
        isBound = true;

        // 즉시 하차 방지
        exitEnabledFrame = Time.frameCount + 1;
    }

    /// <summary>
    /// 현재 Player 입력 연결을 해제하고 남은 이동 입력 제거
    /// </summary>
    public void Unbind()
    {
        movement.ClearInput();

        movementInput = null;
        actionInput = null;
        isBound = false;
        exitEnabledFrame = 0;
    }
}