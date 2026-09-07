using UnityEngine;
using UnityEngine.UI;

public class InventoryDragIcon : MonoBehaviour
{
    [SerializeField]
    private Image icon;
    [SerializeField]
    private RectTransform rectTransform;

    public void Show(Sprite sprite)
    {
        icon.sprite = sprite;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        icon.sprite = null;
        gameObject.SetActive(false);
    }

    public void MoveDragIcon(Vector2 movePos)
    {
        rectTransform.position = movePos;
    }
}
