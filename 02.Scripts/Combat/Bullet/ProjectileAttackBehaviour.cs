using UnityEngine;

public class ProjectileAttackBehaviour : MonoBehaviour, IHoldItemAttack
{
    [SerializeField]
    private HoldItemRuntime runtime;
    [SerializeField]
    private Transform bulletSpawnPoint;
    [SerializeField]
    private ProjectileRuntime bulletPrefab;

    public void Execute(HoldItemAttackContext context)
    {
        Vector3 dir = context.TargetPoint - bulletSpawnPoint.position;

        if (dir.sqrMagnitude <= 0.001f)
            return;

        ProjectileRuntime bullet = context.Pool.Get(bulletPrefab, bulletSpawnPoint.position, Quaternion.LookRotation(dir));
        bullet.Init(context.Attacker, dir, runtime.Define, context.HitReporter, context.ImpactPool);        
    }
}
