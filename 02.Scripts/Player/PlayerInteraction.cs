using System;
using UnityEngine;

/// <summary>
/// 플레이어가 World 오브젝트와 상호작용할 때 전달하는 로컬 Context
/// </summary>
public readonly struct PlayerInteractionContext
{
    public GameObject PlayerObject { get; }
    public IUIOpenRequest UIOpenRequest { get; }

    public PlayerInteractionContext(GameObject playerObject, IUIOpenRequest uiOpenRequest)
    {
        if (playerObject == null)
            throw new ArgumentNullException(nameof(playerObject));

        if (uiOpenRequest == null)
            throw new ArgumentNullException(nameof(uiOpenRequest));

        PlayerObject = playerObject;
        UIOpenRequest = uiOpenRequest;
    }
}

/// <summary>
/// Player가 사용할 수 있는 World 상호작용 대상 계약
/// </summary>
public interface  IPlayerInteractable
{
    bool CanInteraction { get; }

    public void Interact(PlayerInteractionContext context);
}

public class PlayerInteraction : MonoBehaviour
{
    private IPlayerInteractable registeredInteractable;
    private IUIOpenRequest uiOpenRequest;

    public void BindUIOpenRequest(IUIOpenRequest request)
    {
        if(request == null)
            throw new ArgumentNullException(nameof(request));

        uiOpenRequest = request;
    }

    public void RegistInteract(IPlayerInteractable interact)
    {
        if (interact == null || !interact.CanInteraction)
            return;

        registeredInteractable = interact;
    }

    public void UnRegisterInteract(IPlayerInteractable interact)
    {
        if (ReferenceEquals(registeredInteractable, interact))
            registeredInteractable = null;
    }

    public bool TryInteract(IPlayerInteractable interactable)
    {
        if (interactable == null || !interactable.CanInteraction)
            return false;

        if (uiOpenRequest == null)
            throw new InvalidOperationException("PlayerInteraction에 UI Open Request가 연결되지 않음");

        PlayerInteractionContext context = new PlayerInteractionContext(gameObject, uiOpenRequest);

        interactable.Interact(context);

        return true;
    }

    /// <summary>
    /// Trigger를 통해 현재 등록된 대상과 상호작용 시도
    /// </summary>
    /// <returns>상호작용을 실행했으면 true</returns>
    public bool TryInteractRegistered()
    {
        return TryInteract(registeredInteractable);
    }
}
