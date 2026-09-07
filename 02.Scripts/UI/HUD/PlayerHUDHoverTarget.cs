using UnityEngine;
using UnityEngine.EventSystems;

public enum PlayerHUDResourceType
{
    None,
    Energy,
    HP,
    Food,
    Oxygen
}

public class PlayerHUDHoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField]
    private PlayerHUDResourceType playerHUDResourceType;
    [SerializeField]
    private PlayerHUDController hudController;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        hudController.MoveResourceToPrimarySlot(playerHUDResourceType);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hudController.ShowStatusInfo(playerHUDResourceType);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hudController.HideStatusInfo();
    }
}
