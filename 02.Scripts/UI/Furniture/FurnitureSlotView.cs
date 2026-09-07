using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FurnitureSlotView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    private GameObject overlay;
    [SerializeField]
    private FurnitureDefine define;
    [SerializeField]
    private Image icon;

    private bool canClick;

    public event Action<FurnitureDefine> OnClickSlot;
    public event Action<FurnitureDefine, RectTransform> OnEnterPointer;
    public event Action OnExitPointer;

    public FurnitureDefine Define => define;
    private void OnDestroy()
    {
        OnClickSlot = null;
        OnEnterPointer = null;
        OnExitPointer = null;
    }

    private void Awake()
    {
        icon.sprite = define.Icon;
        canClick = false;
    }

    public void SetVisible(bool value)
    {
        gameObject.SetActive(value);
    }

    public void SetCanConstruct(bool canConstruct)
    {
        overlay.SetActive(!canConstruct);
        canClick = canConstruct;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!canClick)
            return;

        OnClickSlot?.Invoke(define);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        RectTransform rect = (RectTransform)transform;
        OnEnterPointer?.Invoke(define, rect);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnExitPointer?.Invoke();
    }
}
