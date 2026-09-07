using System;

/// <summary>
/// 플레이어가 정보를 조회할 수 있는 World 오브젝트 계약
/// 구체적인 UI 표시 방식은 정의하지 않음
/// </summary>
public interface IWorldInspectable
{
    /// <summary>
    /// World 오브젝트의 표시 이름
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// World 오브젝트의 간단한 설명
    /// </summary>
    string Description { get; }
}

/// <summary>
/// 현재 Pointer Hover 중인 조회 가능 World 오브젝트를 제공
/// </summary>
public interface IWorldInspectionSource
{
    /// <summary>
    /// 현재 조회 대상이 변경됐을 때 발생
    /// 조회 가능한 대상이 없으면 null 전달
    /// </summary>
    event Action<IWorldInspectable> OnInspectionTargetChanged;

    /// <summary>
    /// 현재 Pointer Hover 중인 조회 가능 대상
    /// </summary>
    IWorldInspectable CurrentInspectionTarget { get; }
}