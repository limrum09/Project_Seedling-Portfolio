using TMPro;
using UnityEngine;

public class ItemRemoveCheckPanel : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI contentText;
    [SerializeField]
    private GameObject panel;
    
    public void SetText(string itemDisplayName, int removeAmount, float itemWeight)
    {
        float totalWeight = removeAmount * itemWeight;

        contentText.text = $"{itemDisplayName} {removeAmount}개\n{itemWeight} X {removeAmount} : {totalWeight}kg\n버리시겠습니까?";
    }

    public void Show()
    {
        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
    }
}
