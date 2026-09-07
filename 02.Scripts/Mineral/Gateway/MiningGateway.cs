using System;
using UnityEngine;

/// <summary>
/// 클라이언트의 채광 의도를 권한 영역으로 전달
/// </summary>
public interface IMiningCommandGateway
{
    event Action<int, MiningResult> OnMiningCompleted;

    /// <summary>
    /// 지정한 광물 타격 처리를 요청
    /// </summary>
    /// <param name="holdItem">채광에 사용한 장비 Define</param>
    /// <param name="mineral">타격한 광물</param>
    /// <param name="hitPoint">광물 타격 지점</param>
    /// <returns>요청 식별자</returns>
    int RequestMine(HoldItemDefine holdItem, MineralRuntime mineral, Vector3 hitPoint);
}

/// <summary>
/// 채광 요청 식별자와 완료 결과 전달 방식을 정의
/// 구체적인 Local 또는 Network 전달 방식은 하위 클래스에서 구현
/// </summary>
public abstract class MiningGateway : MonoBehaviour, IMiningCommandGateway
{
    private int nextRequestId = 1;

    public event Action<int, MiningResult> OnMiningCompleted;

    /// <summary>
    /// 다음 채광 요청 식별자 생성
    /// </summary>
    /// <returns>생성된 요청 식별자</returns>
    private int CreateRequestId()
    {
        int requestId = nextRequestId;

        if (nextRequestId == int.MaxValue)
            nextRequestId = 1;
        else
            nextRequestId++;

        return requestId;
    }

    /// <summary>
    /// 구체적인 전달 방식으로 채광 요청
    /// </summary>
    /// <param name="requestId">요청 식별자</param>
    /// <param name="holdItem">채광에 사용한 장비 Define</param>
    /// <param name="mineral">타격한 광물</param>
    /// <param name="hitPoint">광물 타격 지점</param>
    protected abstract void SendMiningRequest(int requestId, HoldItemDefine holdItem, MineralRuntime mineral, Vector3 hitPoint);

    /// <summary>
    /// 채광 완료 결과를 요청자에게 전달
    /// </summary>
    /// <param name="requestId">완료된 요청 식별자</param>
    /// <param name="result">채광 처리 결과</param>
    protected void NotifyMiningCompleted(int requestId, MiningResult result)
    {
        if (requestId <= 0)
        {
            Debug.LogError("Mining Request ID가 이상함 " + requestId);

            return;
        }

        if (result == null)
        {
            Debug.LogError("Mining Result가 없음");

            return;
        }

        OnMiningCompleted?.Invoke(requestId, result);
    }

    /// <summary>
    /// 적중한 Collider에서 MineralRuntime을 찾아 채광 요청
    /// 광물이 아닌 대상은 처리하지 않음
    /// </summary>
    /// <param name="holdItem">사용한 장비 Define</param>
    /// <param name="target">적중한 Collider</param>
    /// <param name="hitPoint">실제 타격 지점</param>
    public void ReportHit(HoldItemDefine holdItem, Collider target, Vector3 hitPoint)
    {
        if (target == null)
            return;

        MineralRuntime mineral = target.GetComponentInParent<MineralRuntime>();

        if (mineral == null)
            return;

        RequestMine(holdItem, mineral, hitPoint);
    }

    /// <summary>
    /// 지정한 광물 타격 처리를 요청
    /// </summary>
    /// <param name="holdItem">채광에 사용한 장비 Define</param>
    /// <param name="mineral">타격한 광물</param>
    /// <param name="hitPoint">광물 타격 지점</param>
    /// <returns>요청 식별자</returns>
    public int RequestMine(HoldItemDefine holdItem, MineralRuntime mineral, Vector3 hitPoint)
    {
        int requestId = CreateRequestId();

        SendMiningRequest(requestId, holdItem, mineral, hitPoint);

        return requestId;
    }
}
