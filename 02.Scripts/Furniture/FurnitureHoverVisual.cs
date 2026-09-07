using UnityEngine;

public class FurnitureHoverVisual : MonoBehaviour
{
    [SerializeField]
    private Renderer[] renderers;

    [SerializeField]
    private RenderingLayerMask outlineLayerMask;

    private void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }

    /// <summary>
    /// 하위 Renderer에 선택 Outline Layer를 추가 또는 제거
    /// </summary>
    /// <param name="selected">선택 Outline 표시 여부</param>
    public void SetSelected(bool selected)
    {
        uint mask = outlineLayerMask.value;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;


            if (selected)
                renderer.renderingLayerMask |= mask;
            else
                renderer.renderingLayerMask &= ~mask;
        }
    }
}
