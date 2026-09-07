using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InvenSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    [SerializeField]
    private ItemDefine item;
    [SerializeField]
    private RectTransform rect;
    [SerializeField]
    private Image icon;
    [SerializeField]
    private TextMeshProUGUI amountText;

    private int amount;
    private int index;

    public event Action<int> OnEnterPointer;
    public event Action OnExitPointer;
    public event Action OnCancelPointer;
    public event Action<int> OnDragStart;
    public event Action<Vector2> OnDragMove;
    public event Action<int> OnDragEnd;

    public ItemDefine SlotItem => item;
    public RectTransform Rect => rect;
    public int Index => index;
    public int Amount => amount;


    private void OnDestroy()
    {
        OnEnterPointer = null;
        OnExitPointer = null;
        OnCancelPointer = null;
        OnDragStart = null;
        OnDragMove = null;
        OnDragEnd = null;
    }

    public void SetSnapshot(InventorySlotSnapshot snapshot)
    {
        item = snapshot.Item;
        amount = snapshot.ItemAmount;
        index = snapshot.Index;

        icon.sprite = item != null ? item.Icon : null;
        icon.gameObject.SetActive(item != null);

        amountText.text = amount != 0 ? amount.ToString() : string.Empty;
    }

    public void Clear()
    {
        item = null;
        amount = 0;
        icon.sprite = null;

        icon.gameObject.SetActive(false);
        amountText.text = string.Empty;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnEnterPointer?.Invoke(index);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnExitPointer?.Invoke();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        OnDragStart?.Invoke(index);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        var mouseTarget = eventData.pointerCurrentRaycast.gameObject;

        InvenSlotView slotView = mouseTarget != null ? mouseTarget.GetComponent<InvenSlotView>() : null;

        if(slotView != null)
        {
            OnDragEnd?.Invoke(slotView.Index);
            return;
        }

        if (eventData.pointerCurrentRaycast.module is UnityEngine.UI.GraphicRaycaster)
        {
            OnCancelPointer?.Invoke();
            return;
        }

        OnDragEnd?.Invoke(-1);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 mousePointer = eventData.position;

        OnDragMove?.Invoke(mousePointer);
    }
}
