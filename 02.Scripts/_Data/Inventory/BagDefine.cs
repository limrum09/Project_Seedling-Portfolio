using UnityEngine;

[CreateAssetMenu(fileName = "_BagDefine", menuName = "Project/Inventory/Bag Define")]
public class BagDefine : ScriptableObject
{
    [SerializeField]
    private string bagUID;
    [SerializeField]
    private string displayName;
    [SerializeField, Min(20)]
    private int slotCount = 20;
    [SerializeField, Min(30f)]
    private float maxCarryWeight = 30f;

    public string BagUID => bagUID;
    public string DisplayName => displayName;
    public int SlotCount => slotCount;
    public float MaxCarryWeight => maxCarryWeight;
}
