using UnityEngine;

/// <summary>
/// Runtime 건물의 선택 상태에 따라 하위 Renderer의 Outline Rendering Layer를 변경
/// 건물의 선택 가능 여부를 판단하거나 선택 상태 자체를 관리하지는 않음
/// </summary>
public class BuildingSelectionVisual : MonoBehaviour
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

        foreach(Renderer renderer in renderers)
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
