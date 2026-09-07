using System.Collections.Generic;

public class InventoryPresenter
{
    private Dictionary<int, int> pendingMoveTarget = new Dictionary<int, int>();

    private IInventoryReadAccess inventoryReadAccess;
    private IInventoryCommandGateWay inventoryGateWay;
    private InventoryPanel inventoryPanel;
    private HotkeyPanel hotkeyPanel;

    private bool isInit;

    public InventoryPresenter(IInventoryReadAccess readAccess, IInventoryCommandGateWay gateWay, InventoryPanel getPanel, HotkeyPanel gethotkeyPanel)
    {
        inventoryReadAccess = readAccess;
        inventoryGateWay = gateWay;
        inventoryPanel = getPanel;
        hotkeyPanel = gethotkeyPanel;

        isInit = false;
    }

    private void InventoryChange(InventoryChangeSet changeSet)
    {
        inventoryPanel.SetSlot(changeSet);

        InventorySlotSnapshot[] snapshots = inventoryReadAccess.GetAllSnapshot();

        RefreshHotkeyAmounts(snapshots);
    }

    private void RefreshCurrentState()
    {
        InventorySlotSnapshot[] snapshots = inventoryReadAccess.GetAllSnapshot();

        InventoryChangeSet changeSet = new InventoryChangeSet(inventoryReadAccess.GetAllSnapshot(), inventoryReadAccess.CurrentInventoryWeight);

        inventoryPanel.SetSlot(changeSet);
        RefreshHotkeyAmounts(snapshots);
    }

    private void RefreshHotkeyAmounts(IReadOnlyList<InventorySlotSnapshot> snapshots)
    {
        Dictionary<string, int> amounts = new Dictionary<string, int>();

        foreach(InventorySlotSnapshot snapshot in snapshots)
        {
            ItemDefine item = snapshot.Item;

            if(item == null)
                continue;

            if(item.Category != ItemCategory.Consumable)
                continue;

            if (!amounts.TryAdd(item.ItemUID, snapshot.ItemAmount))
                amounts[item.ItemUID] += snapshot.ItemAmount;
        }

        hotkeyPanel.SetAmounts(amounts);
    }

    private void MoveSlotRequest(int firstIndex, int secondIndex)
    {
        int clientID = inventoryGateWay.RequestMoveSlot(firstIndex, secondIndex);

        pendingMoveTarget.Add(clientID, secondIndex);
    }

    private void DiscardSlotReuest(int index)
    {
        inventoryGateWay.RequestDiscardSlot(index);
    }

    private void MoveSlotCompleted(int clientID, InventoryOperationResult result)
    {
        if (!pendingMoveTarget.TryGetValue(clientID, out int destinationIndex))
            return;

        inventoryPanel.ShowSlotItemInfo(destinationIndex);

        pendingMoveTarget.Remove(clientID);
    }

    public void Init()
    {
        if (isInit)
            return;

        isInit = true;

        inventoryReadAccess.OnInventoryChange += InventoryChange;

        inventoryPanel.OnSlotMoveRequested += MoveSlotRequest;
        inventoryPanel.OnSlotDiscardRequested += DiscardSlotReuest;

        inventoryPanel.InitSlots(inventoryReadAccess.InvenSlotCount);
        inventoryPanel.InitInventory(inventoryReadAccess.NormalInventoryWeight, inventoryReadAccess.MaximumInventoryWeight);

        inventoryGateWay.OnMoveSlotCompleted += MoveSlotCompleted;

        hotkeyPanel.Init();
        RefreshCurrentState();
        inventoryPanel.SetVisible(false);
    }

    public void Show()
    {
        inventoryPanel.SetVisible(true);
    }

    public void Hide()
    {
        inventoryPanel.SetVisible(false);
    }

    public void Dispose()
    {
        inventoryReadAccess.OnInventoryChange -= InventoryChange;

        inventoryPanel.OnSlotMoveRequested -= MoveSlotRequest;
        inventoryPanel.OnSlotDiscardRequested -= DiscardSlotReuest;

        inventoryGateWay.OnMoveSlotCompleted -= MoveSlotCompleted;
    }
}
