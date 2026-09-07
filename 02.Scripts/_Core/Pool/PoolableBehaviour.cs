using System;
using UnityEngine;

/// <summary>
/// Unity ObjectPool로 반환할 수 있는 Runtime Component의 공통 기반
/// </summary>
public abstract class PoolableBehaviour : MonoBehaviour
{
    private Action releaseAction;

    /// <summary>
    /// 이 인스턴스를 소유한 Pool의 반환을 연결
    /// </summary>
    /// <param name="getReleaseAction">인스턴스를 Pool에 반환할 동작</param>
    public void BindRelease(Action getReleaseAction)
    {
        releaseAction = getReleaseAction;
    }

    /// <summary>
    /// Pool 반환 전에 Runtime 상태를 초기화
    /// </summary>
    public abstract void ResetPoolState();

    /// <summary>
    /// 현재 인스톤스를 소유 Pool에 반환
    /// </summary>
    protected void ReleaseToPool()
    {
        releaseAction?.Invoke();
    }
}
