using UnityEngine;

/// <summary>
/// 한 플레이어의 권한 Inventory 생명 주기와 권환 기능 접근을 제공
/// </summary>
[RequireComponent(typeof(InventoryService))]
public class PlayerInventoryAuthority : MonoBehaviour
{
    [SerializeField]
    private InventoryService inventoryService;

    public InventoryService InventoryService => inventoryService;
    public IInventoryItemReceiver ItemReceiver => inventoryService;
    public IInventoryConsumption Consumption => inventoryService;

    public void Init()
    {
        inventoryService.Init();
    }
}
