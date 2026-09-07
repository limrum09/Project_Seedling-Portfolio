using UnityEngine;

public class TempInventoryButton : MonoBehaviour
{
    [SerializeField]
    private LocalPlayerSpawner spawner;

    private IInventoryItemReceiver inventoryItemReceiver => spawner.CurrentAuthority.ItemReceiver;

    public void AddInvenItem(ItemDefine itemDefine)
    {
        inventoryItemReceiver.TryAdd(itemDefine, 1);
    }
}
