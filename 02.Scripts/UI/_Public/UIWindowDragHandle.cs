using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class UIWindowDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField]
    private RectTransform targetRect;
    [SerializeField]
    private bool keepInsideParent = true;
    [SerializeField]
    private bool bringToFront = true;

    private readonly Vector3[] targetCorners =new Vector3[4];

    private RectTransform rootRect;
    private Vector2 pointerOffset;
    private bool isDragging;

    private void Awake()
    {
        if (targetRect == null)
            return;

        rootRect = targetRect.parent as RectTransform;

        if (rootRect == null)
            return;
    }

    private void ClampInsideRoot()
    {
        targetRect.GetWorldCorners(targetCorners);

        Vector2 minium = rootRect.InverseTransformPoint(targetCorners[0]);
        Vector2 maxium = rootRect.InverseTransformPoint(targetCorners[2]);

        Rect rootBounds = rootRect.rect;
        Vector2 correction = Vector2.zero;

        if (minium.x < rootBounds.xMin)
            correction.x += rootBounds.xMin - minium.x;
        else if (maxium.x > rootBounds.xMax)
            correction.x -= maxium.x - rootBounds.xMax;

        if (minium.y < rootBounds.yMin)
            correction.y += rootBounds.yMin - minium.y;
        else if(maxium.y > rootBounds.yMax)
            correction.y -= maxium.y - rootBounds.yMax;

        targetRect.anchoredPosition += correction;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, eventData.position, eventData.pressEventCamera, out Vector2 pointerPos))
            return;

        pointerOffset = targetRect.anchoredPosition - pointerPos;
        isDragging = true;

        if (bringToFront)
            targetRect.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, eventData.position, eventData.pressEventCamera, out Vector2 pointerPos))
            return;

        targetRect.anchoredPosition = pointerPos + pointerOffset;

        if (keepInsideParent)
            ClampInsideRoot();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        isDragging = false;
    }
}
