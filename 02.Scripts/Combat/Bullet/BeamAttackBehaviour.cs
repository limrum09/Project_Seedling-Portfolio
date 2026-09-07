using UnityEngine;

public class BeamAttackBehaviour : MonoBehaviour, IContinuousHoldItemAttack
{
    [SerializeField]
    private HoldItemRuntime runtime;
    [SerializeField]
    private Transform bulletSpawnPoint;
    [SerializeField]
    private LineRenderer line;
    [SerializeField]
    private LayerMask hitMask;
    [SerializeField]
    private HitImpactEffect impactEffect;

    private Collider currentHitTarget;
    private Vector3 currentHitPoint;

    private void OnDisable()
    {
        ClearHitState();
    }

    private void ClearHitState()
    {
        currentHitTarget = null;
        currentHitPoint = default;

        impactEffect.Hide();
    }

    public void Begin(HoldItemAttackContext context)
    {
        line.enabled = true;
        UpdateBeam(context);
    }

    public void UpdateBeam(HoldItemAttackContext context)
    {
        Vector3 origin = bulletSpawnPoint.position;
        Vector3 dir = context.TargetPoint - origin;

        if (dir.sqrMagnitude <= 0.001f)
        {
            line.SetPosition(0, origin);
            line.SetPosition(1, origin);

            ClearHitState();
            return;
        }
            

        dir.Normalize();

        Vector3 endPoint = origin + dir * runtime.Define.AttackRange;

        currentHitTarget = null;

        if(Physics.Raycast(origin, dir, out RaycastHit hit, runtime.Define.AttackRange, hitMask, QueryTriggerInteraction.Ignore))
        {
            endPoint = hit.point;
            currentHitTarget = hit.collider;
            currentHitPoint = hit.point;
            impactEffect.Show(hit.point, hit.normal);
        }
        else
        {
            ClearHitState();
        }

            line.SetPosition(0, origin);
        line.SetPosition(1, endPoint);
    }

    public void ApplyDamageTick(HoldItemAttackContext context)
    {
        if (currentHitTarget == null)
        {
            return;
        }
            
        context.HitReporter.ReportHit(runtime.Define, currentHitTarget, currentHitPoint);
    }    

    public void End()
    {
        ClearHitState();
        line.enabled = false;
    }
}
