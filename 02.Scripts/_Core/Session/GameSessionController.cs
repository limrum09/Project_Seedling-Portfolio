using UnityEngine;

/// <summary>
/// 게임 Session 시작 상태를 관리하고 Player 생성 시작을 요청
/// 구체적인 싱글 또는 멀티 생성 방식은 판단하지 않음
/// </summary>
public class GameSessionController : MonoBehaviour
{
    [SerializeField]
    private SceneWorldInitializer worldInit;
    [SerializeField]
    private PlayerSpawner playerSpawner;
    [SerializeField]
    private ProductionTickSystem productionTick;
    [SerializeField]
    private bool beginOnStart = true;

    private bool isStarted;

    public bool IsStarted => isStarted;

    private void Awake()
    {
        productionTick.enabled = false;
    }

    private void Start()
    {
        if (beginOnStart)
            BeginSession();
    }

    /// <summary>
    /// Session을 시작하고 PlayerSpawn에 생성을 요철
    /// </summary>
    /// <returns>이번 호출로 Session이 시작 되면 true</returns>
    public bool BeginSession()
    {
        if (isStarted)
            return false;

        worldInit.Init();
        
        playerSpawner.BeginSpawn();

        productionTick.enabled = true;

        isStarted = true;

        return true;
    }
}
