using UnityEngine;

/// <summary>
/// World Pointer Hover 상태를 전달받는 대상
/// </summary>
public interface IWorldHoverTarget
{
    /// <summary>
    /// Pointer Hover 상태 변경
    /// </summary>
    /// <param name="hovered">Pointer Hover 여부</param>
    void SetHovered(bool hovered);
}
