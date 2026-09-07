using UnityEngine;

public class HitImpactEffect : PoolableBehaviour
{
    [SerializeField]
    private GameObject visualRoot;
    [SerializeField]
    private float surfaceOffset = 0.01f;
    [SerializeField]
    private float lifeTime = 0.25f;

    private float remainingLifeTime = 0f;
    private bool isActive;

    private void Awake()
    {
        Hide();
    }

    private void OnDisable()
    {
        remainingLifeTime = 0f;
        isActive = false;
    }

    private void Update()
    {
        if (!isActive)
            return;

        remainingLifeTime -= Time.deltaTime;

        if (remainingLifeTime > 0f)
            return;

        isActive = false;
        ReleaseToPool();
    }

    /// <summary>
    /// 표면에서 약간 떨어진 위치와 방향으로 Transfrom 배치 
    /// </summary>
    /// <param name="point"></param>
    /// <param name="normal"></param>
    private void ApplyPos(Vector3 point, Vector3 normal)
    {
        Vector3 pos = point + normal * surfaceOffset;
        Quaternion rotate = Quaternion.LookRotation(normal);

        transform.SetPositionAndRotation(pos, rotate);
    }

    /// <summary>
    /// 피격 지점과 표면 방향에 맞춰 이펙트 표시
    /// </summary>
    /// <param name="point">공격이 충돌한 지점</param>
    /// <param name="normal">충돌한 표면이 향하는 방향</param>
    public void Show(Vector3 point, Vector3 normal)
    {
        ApplyPos(point, normal);

        visualRoot.SetActive(true);
    }

    /// <summary>
    /// 이펙트 숨기기
    /// </summary>
    public void Hide()
    {
        remainingLifeTime = 0f;
        isActive = false;

        visualRoot.SetActive(false);
    }

    /// <summary>
    /// Projectile 충돌용 이펙트 일정 시간 재생
    /// </summary>
    /// <param name="point"></param>
    /// <param name="normal"></param>
    public void Play(Vector3 point, Vector3 normal)
    {
        Show(point, normal);

        remainingLifeTime = lifeTime;
        isActive = true;
    }

    public override void ResetPoolState()
    {
        Hide();
    }
}
