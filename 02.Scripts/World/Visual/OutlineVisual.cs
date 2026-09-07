using UnityEngine;

/// <summary>
/// 하위 Renderer의 Outline Rendering Layer 표시 상태를 변경
/// Hover나 선택 여부 자체는 판단하지 않음
/// </summary>
public sealed class OutlineVisual : MonoBehaviour
{
    [SerializeField]
    private RenderingLayerMask outlineLayerMask;

    private Renderer[] renderers;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    /// <summary>
    /// 하위 Renderer의 Outline 표시 상태 변경
    /// </summary>
    /// <param name="visible">Outline 표시 여부</param>
    public void SetVisible(bool visible)
    {
        uint mask = outlineLayerMask.value;

        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer == null)
                continue;

            if (visible)
                targetRenderer.renderingLayerMask |= mask;
            else
                targetRenderer.renderingLayerMask &= ~mask;
        }
    }
}