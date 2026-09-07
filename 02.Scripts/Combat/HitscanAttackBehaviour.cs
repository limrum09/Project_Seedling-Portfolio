using UnityEngine;
using UnityEngine.Serialization;

public class HitscanAttackBehaviour : MonoBehaviour, IHoldItemAttack
{
    [SerializeField]
    private HoldItemRuntime runtime;
    [SerializeField]
    private Transform bulletSpawnPos;
    [SerializeField, FormerlySerializedAs("hitMaks")]
    private LayerMask hitMask;

    private void Awake()
    {
        if (runtime == null)
            runtime = GetComponent<HoldItemRuntime>();
    }

    public void Execute(HoldItemAttackContext context)
    {
        Transform spawnPos = bulletSpawnPos;
        HoldItemDefine define = runtime.Define;

        Vector3 origin = spawnPos.position;
        Vector3 dir = context.TargetPoint - origin;

        if (dir.sqrMagnitude <= 0.001f)
            return;

        dir.Normalize();

        bool isHit = Physics.Raycast(origin, dir, out RaycastHit hit, define.AttackRange, hitMask, QueryTriggerInteraction.Ignore);

        Debug.DrawRay(origin, dir * define.AttackRange, isHit ? Color.red : Color.green);

        if (isHit)
        {
            context.HitReporter.ReportHit(define, hit.collider, hit.point);
        }
    }
}
