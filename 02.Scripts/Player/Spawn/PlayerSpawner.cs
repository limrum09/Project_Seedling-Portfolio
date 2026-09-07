using System;
using UnityEngine;

/// <summary>
/// 게임 방식과 관계없이 Player 생성 시작과 로컬 Player 준비 결과를 제공
/// 구체적인 생성 방식은 하위 클래스에서 구현
/// </summary>
public abstract class PlayerSpawner : MonoBehaviour
{
    public event Action<Player> OnLocalPlayerReady;

    public Player CurrentLocalPlayer { get; private set; }

    /// <summary>
    /// Player 생성 시작
    /// </summary>
    public abstract void BeginSpawn();

    /// <summary>
    /// 로컬에 저작할 Player가 준비되었음을 전달
    /// </summary>
    /// <param name="player">준비된 Local Player</param>
    protected void NotifyLocalPlayerReady(Player player)
    {
        if (player == null)
            return;

        CurrentLocalPlayer = player;
        OnLocalPlayerReady?.Invoke(CurrentLocalPlayer);
    }
}
