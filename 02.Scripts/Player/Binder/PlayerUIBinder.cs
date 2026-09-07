using UnityEngine;

/// <summary>
/// Local Player 접근점을 UI Controller와 Presenter에 연결
/// </summary>
public class PlayerUIBinder : MonoBehaviour
{
    [Header("UI Router")]
    [SerializeField]
    private UIPanelRouter router;

    [Header("UI Controller")]
    [SerializeField]
    private InventoryUIController inventoryUICtr;
    [SerializeField]
    private FurnitureUIController furnitureUICtr;
    [SerializeField]
    private BuildingUIController buildingUICtr;
    [SerializeField]
    private WeaponUIController weaponUICtr;
    [SerializeField]
    private PlayerHUDController hudController;
    [SerializeField]
    private WorldHoverTooltipController worldHoverTooltipController;
    [SerializeField]
    private HotkeyPanel hotkeyPanel;
    [SerializeField]
    private InteractionButtons interactionButtons;

    /// <summary>
    /// Local Player와 Placement 접근점을 UI에 연결
    /// </summary>
    /// <param name="player">연결할 Local Player</param>
    /// <param name="buildAccess">Building 접근점</param>
    /// <param name="furnitureAccess">Furniture 접근점</param>
    public void Bind(Player player, IBuildingPlacementAccess buildAccess, IFurniturePlacementAccess furnitureAccess)
    {
        player.BindInteractionUI(router);

        inventoryUICtr.Bind(player.InvenReadAccess, player.InvenGateway, hotkeyPanel);
        hudController.Bind(player.Status);
        worldHoverTooltipController.Bind(player.InspectionSource, player.PointerInputSource);
        interactionButtons.Bind(buildAccess, furnitureAccess);

        buildingUICtr.Bind(buildAccess, player.InvenReadAccess);
        furnitureUICtr.Bind(furnitureAccess, player.InvenReadAccess);
        weaponUICtr.Bind(player.WeaponReadAccess, player.WeaponGateway, player.InvenReadAccess);
    }
}
