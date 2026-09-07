using System;
using UnityEngine;

/// <summary>
/// 화면 Pointer 위치에서 World 상호작용 대상을 검색
/// 입력 처리와 실제 Interact 실행은 담당하지 않음
/// </summary>
public sealed class PlayerPointerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField]
    private LayerMask interactionMask;
    [SerializeField, Min(8f)]
    private float interactionDistance = 10f;

    [Header("Hover")]
    [SerializeField]
    private LayerMask hoverMask;
    [SerializeField, Min(10f)]
    private float hoverDistance = 15f;

    private Camera worldCamera;

    /// <summary>
    /// Pointer Raycast에 사용할 World Camera 설정
    /// </summary>
    /// <param name="camera">Player가 사용하는 World Camera</param>
    public void SetCamera(Camera camera)
    {
        if (camera == null)
            throw new ArgumentNullException(nameof(camera));

        worldCamera = camera;
    }

    /// <summary>
    /// 지정한 화면 좌표에서 사용할 수 있는 상호작용 대상 검색
    /// </summary>
    /// <param name="screenPosition">화면 Pointer 좌표</param>
    /// <param name="interactable">검색된 상호작용 대상</param>
    /// <returns>사용 가능한 대상을 찾았으면 true</returns>
    public bool TryGetInteractable(Vector2 screenPosition, out IPlayerInteractable interactable)
    {
        if (worldCamera == null)
            throw new InvalidOperationException("PlayerPointerInteraction에 World Camera가 설정되지 않음");

        Ray ray = worldCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionMask, QueryTriggerInteraction.Collide))
        {
            interactable = null;
            return false;
        }

        MonoBehaviour[] behaviours = hit.collider.GetComponentsInParent<MonoBehaviour>();

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (!(behaviours[i] is IPlayerInteractable found))
                continue;

            if (!found.CanInteraction)
                continue;

            interactable = found;
            return true;
        }

        interactable = null;
        return false;
    }

    /// <summary>
    /// 지정한 화면 좌표에서 Pointer Hover 대상 검색
    /// </summary>
    /// <param name="screenPosition">화면 Pointer 좌표</param>
    /// <param name="hoverTarget">검색된 Hover 대상</param>
    /// <returns>Hover 대상을 찾았으면 true</returns>
    public bool TryGetHoverTarget(Vector2 screenPosition, out IWorldHoverTarget hoverTarget)
    {
        if (worldCamera == null)
            throw new InvalidOperationException("PlayerPointerInteraction에 World Camera가 연결되지 않음");

        Ray ray = worldCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, hoverDistance, hoverMask, QueryTriggerInteraction.Ignore))
        {
            hoverTarget = null;
            return false;
        }

        MonoBehaviour[] behaviours = hit.collider.GetComponentsInParent<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is not IWorldHoverTarget found)
                continue;

            hoverTarget = found;
            return true;
        }

        hoverTarget = null;
        return false;
    }
}