using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HotkeySlotView : MonoBehaviour
{
    [SerializeField]
    private ItemDefine item;
    [SerializeField]
    private Image icon;
    [SerializeField]
    private TextMeshProUGUI keyText;
    [SerializeField]
    private TextMeshProUGUI amountText;

    public string ItemUID => item.ItemUID;

    public void Init()
    {
        icon.sprite = item.Icon;
        icon.enabled = item.Icon != null;

        SetAmount(0);
    }

    public void SetAmount(int amount)
    {
        amountText.text = Mathf.Max(0, amount).ToString();
    }
}
