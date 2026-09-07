using UnityEngine;
using System;

/// <summary>
/// Player 공격 입력 상태를 공격 요청으로 변환
/// 실제 공격 규칙은 Player가 연결한 Service에서 처리
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [SerializeField]
    private Player player;

    [Header("Player Aim")]
    [SerializeField]
    private Transform aimTransform;
    [SerializeField]
    private LayerMask aimMask;
    [SerializeField]
    private float aimDistance = 100f;

    private Camera worldCamera;
    private bool isAttakStart;

    /// <summary>
    /// 화면 Pointer 위치로 공격 조준점 계산
    /// </summary>
    /// <param name="screenPosition">현재 Pointer 화면 위치</param>
    /// <param name="aimPoint">계산된 조준점</param>
    /// <returns>조준점을 계산했으면 true</returns>
    private bool GetAimPoint(Vector2 screenPosition, out Vector3 aimPoint)
    {
        aimPoint = Vector3.zero;

        Ray ray = worldCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, aimDistance, aimMask, QueryTriggerInteraction.Ignore))
        {
            aimPoint = hit.point;
            return true;
        }

        aimPoint = ray.GetPoint(aimDistance);
        return true;
    }

    /// <summary>
    /// 공격 조준에 사용할 World Camera 설정
    /// </summary>
    /// <param name="getCamera">사용할 World Camera</param>
    public void SetCamera(Camera getCamera)
    {
        if (getCamera == null)
            throw new ArgumentNullException(nameof(getCamera));

        worldCamera = getCamera;
    }

    /// <summary>
    /// Pointer 상호작용이 공격 입력을 소비했을 때 현재 공격 종료
    /// </summary>
    public void CancelAttackInput()
    {
        if (!isAttakStart)
            return;

        player.EndAttack();
        isAttakStart = false;
    }

    /// <summary>
    /// 현재 공격 입력 상태를 Player 공격 요청으로 변환
    /// </summary>
    /// <param name="pointerPosition">현재 Pointer 화면 위치</param>
    /// <param name="pressed">이번 Frame 공격 시작 여부</param>
    /// <param name="held">공격 유지 여부</param>
    /// <param name="released">이번 Frame 공격 해제 여부</param>
    public void Tick(Vector2 pointerPosition, bool pressed, bool held, bool released)
    {
        if (released)
        {
            player.EndAttack();
            isAttakStart = false;
            return;
        }

        if (!held)
            return;

        if (!GetAimPoint(pointerPosition, out Vector3 aimPoint))
            return;

        if (pressed)
        {
            isAttakStart = player.BeginAttack(aimPoint);
            return;
        }

        if (!isAttakStart)
            return;

        isAttakStart = player.HoldAttack(aimPoint);
    }
}
