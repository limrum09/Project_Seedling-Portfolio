using System.Collections.Generic;
using UnityEngine;

public class HotkeyPanel : MonoBehaviour
{
    [SerializeField]
    private List<HotkeySlotView> hotkeySlots = new List<HotkeySlotView>();

    public void Init()
    {
        foreach (var slot in hotkeySlots)
        {
            slot.Init();
        }
    }

    public void SetAmounts(IReadOnlyDictionary<string, int> amounts)
    {
        foreach(var slot in hotkeySlots)
        {
            amounts.TryGetValue(slot.ItemUID, out int amount);
            slot.SetAmount(amount);
        }
    }
}
