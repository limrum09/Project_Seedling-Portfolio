public sealed class ItemRequirementViewData
{
    public string DisplayName { get; }
    public int InventoryAmount { get; }
    public int RequiredAmount { get; }
    public bool HasEnough => InventoryAmount >= RequiredAmount;

    public ItemRequirementViewData(string displayName, int inventoryAmount, int requiredAmount)
    {
        DisplayName = displayName;
        InventoryAmount = inventoryAmount;
        RequiredAmount = requiredAmount;
    }
}
