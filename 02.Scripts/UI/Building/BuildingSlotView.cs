using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildingSlotView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    private GameObject overlay;
    [SerializeField]
    private BuildingDefine define;
    [SerializeField]
    private Image icon;

    private bool canClick;

    public event Action<BuildingDefine> OnClickSlot;
    public event Action<BuildingDefine, RectTransform> OnEnterPointer;
    public event Action OnExitPointer;

    public BuildingDefine Define => define;

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