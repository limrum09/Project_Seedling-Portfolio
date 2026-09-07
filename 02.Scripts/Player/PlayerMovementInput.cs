using System;
using UnityEngine;

/// <summary>
/// Local Input 상태를 PlayerMovement의 이동 처리로 전달
/// 이동, 중력 또는 Jetpack 규칙은 직접 처리하지 않음
/// </summary>
public sealed class PlayerMovementInput : MonoBehaviour
{
    [SerializeField]
    private PlayerMovement movement;

    private IPlayerMovementInputSource inputSource;

    /// <summary>
    /// Player 이동 입력 접근점 연결
    /// </summary>
    /// <param name="source">Player 이동 입력 접근점</param>
    public void BindInput(IPlayerMovementInputSource source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        inputSource = source;
    }

    /// <summary>
    /// 현재 Frame의 이동 입력을 PlayerMovement에 전달
    /// </summary>
    private void Update()
    {
        movement.Tick(inputSource.Move, inputSource.JumpPressedThisFrame, inputSource.JumpHeld, inputSource.SprintHeld);
    }
}
