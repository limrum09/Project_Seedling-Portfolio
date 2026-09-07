using UnityEngine;

/// <summary>
/// Player 상호작용을 지정한 UI Panel 표시 요청으로 변환
/// 구체적인 UI Controller는 참조하지 않음
/// </summary>
public sealed class WorldUIPanelInteractable : MonoBehaviour, IPlayerInteractable
{
    [SerializeField]
    private UIPanelId panelId;
    [SerializeField]
    private bool isAvailable = true;

    public bool CanInteraction =>isActiveAndEnabled && isAvailable;

    /// <summary>
    /// 지정한 UI Panel 표시 요청 전달
    /// </summary>
    /// <param name="context">Player 상호작용 Context</param>
    public void Interact(PlayerInteractionContext context)
    {
        if (!CanInteraction)
            return;

        context.UIOpenRequest.Open(panelId);
    }

    /// <summary>
    /// 현재 World UI 상호작용 가능 여부 설정
    /// </summary>
    /// <param name="available">상호작용 가능 여부</param>
    public void SetAvailable(bool available)
    {
        isAvailable = available;
    }
}