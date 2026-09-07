using DG.Tweening;
using UnityEditor.ShaderKeywordFilter;
using UnityEngine;
using UnityEngine.Pool;

public class ProjectileRuntime : PoolableBehaviour
{
    [SerializeField]
    private Rigidbody rigid;
    [SerializeField]
    private float moveSpeed = 20f;
    [SerializeField]
    private float lifeTime = 5f;
    [SerializeField]
    private HitImpactEffect impactEffectPrefab;

    private HitImpactEffectPool impactEffectPool;
    private HoldItemDefine define;
    private IHoldItemHitReporter HitReporter;
    private GameObject owner;

    private float remainingLifeTime;
    private bool isActive;

    private float damage;

    private void Update()
    {
        if (!isActive)
            return;

        remainingLifeTime -= Time.deltaTime;

        if (remainingLifeTime > 0)
            return;

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        isActive = false;
        ReleaseToPool();
    }   

    private void OnCollisionEnter(Collision collision)
    {
        if(!isActive) 
            return;

        if (collision.gameObject == owner)
            return;

        ContactPoint contact = collision.GetContact(0);

        HitReporter.ReportHit(define, collision.collider, contact.point);

        HitImpactEffect impactEffect = impactEffectPool.Get(impactEffectPrefab, contact.point, Quaternion.LookRotation(contact.normal));
        impactEffect.Play(contact.point, contact.normal);

        ReturnToPool();
    }

    public void Init(GameObject getOwner, Vector3 dir, HoldItemDefine getDefine, IHoldItemHitReporter getHitReporter, HitImpactEffectPool getImpactEffectPool)
    {
        owner = getOwner;
        define = getDefine;
        HitReporter = getHitReporter;
        impactEffectPool = getImpactEffectPool;

        remainingLifeTime = lifeTime;
        isActive = true;

        rigid.linearVelocity = dir.normalized * moveSpeed;
    }

    public override void ResetPoolState()
    {
        isActive = false;
        remainingLifeTime = 0f;

        owner = null;
        define = null;
        HitReporter = null;
        impactEffectPool = null;

        rigid.linearVelocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;
    }
}
