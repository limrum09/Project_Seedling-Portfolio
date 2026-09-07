using System;
using UnityEngine;

public interface IHoldItemAttack
{
    void Execute(HoldItemAttackContext context);
}

public interface IContinuousHoldItemAttack
{
    void Begin(HoldItemAttackContext context);
    void UpdateBeam(HoldItemAttackContext context);
    void ApplyDamageTick(HoldItemAttackContext context);
    void End();
}

public interface IHoldItemHitReporter
{
    void ReportHit(HoldItemDefine define, Collider target, Vector3 hitPoint);
}

public class HoldItemAttackContext
{
    public GameObject Attacker { get; }
    public Vector3 TargetPoint { get; }
    public IHoldItemHitReporter HitReporter { get; }
    public ProjectilePool Pool { get; }
    public HitImpactEffectPool ImpactPool { get; }

    public HoldItemAttackContext(GameObject attacker, Vector3 targetPoint, IHoldItemHitReporter hitReporter, ProjectilePool projectilePool, HitImpactEffectPool impactPool)
    {
        Attacker = attacker;
        TargetPoint = targetPoint;
        HitReporter = hitReporter;
        Pool = projectilePool;
        ImpactPool = impactPool;
    }
}

public class HoldItemAttackService : MonoBehaviour, IHoldItemHitReporter
{
    [SerializeField]
    private HoldItemEquipmentService equipmentService;
    [SerializeField]
    private AttackEnergyGateway energyGateway;

    private ProjectilePool projectilePool;
    private HitImpactEffectPool hitImpactEffectPool;
    private IContinuousHoldItemAttack activeContinueAttack;
    private HoldItemRuntime activeItem;
    private float nextAttackTime;

    public event Action<HoldItemRuntime> OnAttackSuccess;
    public event Action<HoldItemDefine, Collider, Vector3> OnHit;

    public AttackEnergyGateway EnergyGateway => energyGateway;

    private void Awake()
    {
        if (equipmentService == null)
            equipmentService = GetComponent<HoldItemEquipmentService>();
    }

    private bool TryConsumeAttackEnergy(HoldItemDefine define)
    {
        if (define.EnergyCost <= 0f)
            return false;

        return energyGateway.TryConsumeEnergy(define.EnergyCost);
    }

    public void BindPool(ProjectilePool getProjectilePool, HitImpactEffectPool getHitImpackEffectPool)
    {
        projectilePool = getProjectilePool;
        hitImpactEffectPool = getHitImpackEffectPool;
    }

    public bool TryAttack(GameObject attacker, Vector3 targetPoint)
    {
        if (attacker == null)
            return false;

        HoldItemRuntime currentItem = equipmentService.CurrentItem;

        if(currentItem == null) 
            return false;

        if (Time.time < nextAttackTime)
            return false;

        IHoldItemAttack attack = currentItem.GetComponent<IHoldItemAttack>();

        if(attack == null)
        {
            Debug.LogError("IHoldItemAttack 구현체가 없다");
            return false;
        }

        if (!TryConsumeAttackEnergy(currentItem.Define))
            return false;

        HoldItemAttackContext context = new HoldItemAttackContext(attacker, targetPoint, this, projectilePool, hitImpactEffectPool);

        attack.Execute(context);

        nextAttackTime = Time.time + currentItem.Define.AttackCooldown;

        OnAttackSuccess?.Invoke(currentItem);

        return true;
    }

    public bool BeginAttack(GameObject attacker, Vector3 aimPoint)
    {
        if (attacker == null)
            return false;

        HoldItemRuntime currentItem = equipmentService.CurrentItem;

        if (currentItem == null)
            return false;

        IContinuousHoldItemAttack attack = currentItem.GetComponent<IContinuousHoldItemAttack>();

        if (attack == null)
            return TryAttack(attacker, aimPoint);

        EndAttack();

        if (!TryConsumeAttackEnergy(currentItem.Define))
            return false;

        activeContinueAttack = attack;
        activeItem = currentItem;

        HoldItemAttackContext context = new HoldItemAttackContext(attacker, aimPoint, this, projectilePool, hitImpactEffectPool);

        activeContinueAttack.Begin(context);
        activeContinueAttack.ApplyDamageTick(context);

        nextAttackTime = Time.time + currentItem.Define.AttackCooldown;

        OnAttackSuccess?.Invoke(activeItem);
        return true;
    }

    public bool HoldAttack(GameObject attacker, Vector3 aimPoint)
    {
        HoldItemRuntime currentItem = equipmentService.CurrentItem;

        if (currentItem == null)
            return false;

        if (activeContinueAttack == null)
        {
            IContinuousHoldItemAttack continueAttack = currentItem.GetComponent<IContinuousHoldItemAttack>();

            if (continueAttack != null)
                return false;

            return TryAttack(attacker, aimPoint);
        }
            

        if (currentItem != activeItem)
        {
            EndAttack();
            return false;
        }

        HoldItemAttackContext context = new HoldItemAttackContext(attacker, aimPoint, this, projectilePool, hitImpactEffectPool);

        activeContinueAttack.UpdateBeam(context);

        if(Time.time >= nextAttackTime)
        {
            if (!TryConsumeAttackEnergy(currentItem.Define))
            {
                EndAttack();
                return false;
            }

            activeContinueAttack.ApplyDamageTick(context);

            nextAttackTime = Time.time + currentItem.Define.AttackCooldown;
        }

        return true;
    }

    public void EndAttack()
    {
        if(activeContinueAttack != null)
            activeContinueAttack.End();

        activeContinueAttack = null;
        activeItem = null;
    }

    public void ReportHit(HoldItemDefine define, Collider target, Vector3 hitPoint)
    {
        OnHit?.Invoke(define, target, hitPoint);
    }
}
