using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 로컬 권한 영역에서 발생한 응답을 다음 Frame에 FIFO 순서로 전달
/// </summary>
public sealed class LocalResponseDispatcher : MonoBehaviour
{
    /// <summary>
    /// 응답과 응답이 등록된 Frame을 함께 보관
    /// </summary>
    private readonly struct LocalResponse
    {
        public Action Response { get; }
        public int EnqueuedFrame { get; }

        public LocalResponse(Action response, int enqueuedFrame)
        {
            Response = response;
            EnqueuedFrame = enqueuedFrame;
        }
    }

    private readonly Queue<LocalResponse> responses = new Queue<LocalResponse>();

    /// <summary>
    /// 현재 Frame 시작 전에 등록된 응답을 FIFO 순서로 처리
    /// 처리 중 새로 등록된 응답은 다음 Frame에 처리
    /// </summary>
    private void Update()
    {
        while (responses.Count > 0)
        {
            LocalResponse response = responses.Peek();

            if (response.EnqueuedFrame >= Time.frameCount)
                break;

            responses.Dequeue().Response.Invoke();
        }
    }

    private void OnDisable()
    {
        responses.Clear();
    }

    /// <summary>
    /// 다음 Update에서 처리할 응답을 등록
    /// </summary>
    /// <param name="response">처리할 응답</param>
    public void Enqueue(Action response)
    {
        if (response == null)
            throw new ArgumentNullException(nameof(response));

        responses.Enqueue(new LocalResponse(response, Time.frameCount));
    }
}
