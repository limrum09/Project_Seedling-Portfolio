using UnityEngine;
using UnityEngine.Animations.Rigging;

[RequireComponent (typeof(CharacterController))]
public class PlayerAnimation : MonoBehaviour
{
    [Header("Components")]
    [SerializeField]
    private CharacterController controller;
    [SerializeField]
    private PlayerMovement movement;
    [SerializeField]
    private PlayerStats stats;
    [SerializeField]
    private Animator anim;

    [Header("Rig")]
    [SerializeField]
    private MultiAimConstraint weaponAimCondtraint;
    [SerializeField]
    private TwoBoneIKConstraint leftArmIK;    
    [SerializeField]
    private Transform leftHandIKTarget;
    [SerializeField]
    private Transform weaponSocketPos;
    [SerializeField, Min(0f)]
    private float aimWeightChangedSpeed = 5f;

    [Header("Values")]
    [SerializeField, Min(0f)]
    private float speedTime = 0.1f;
    [SerializeField]
    private float groundDelayTime = 0.25f;

    [Header("Animation Controller")]
    [SerializeField]
    private RuntimeAnimatorController defaultAnimationController;

    private HoldItemRuntime currentItem;
    private Transform currentLeftGrid;
    private float leftHandWeight;
    private float unGroundTime;
    private bool isAiming;

    public bool IsGround { get; private set; }

    private void Awake()
    {
        leftHandWeight = 0f;

        if (controller == null)
            controller = GetComponent<CharacterController>();

        if(anim == null)
            anim = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        UpdateWeaponSocketPos();
        UpdateLeftHandTarget();
    }

    private void LateUpdate()
    {
        Vector3 velocity = controller.velocity;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);

        float maxiumMoveSpeed = Mathf.Max(0.01f, stats.RunSpeed);
        float normalizedSpeed = horizontalVelocity.magnitude / maxiumMoveSpeed;
        normalizedSpeed = Mathf.Clamp01(normalizedSpeed);


        if (movement.JumpStartedThisFrame)
        {
            unGroundTime = groundDelayTime;
            IsGround = false;

        }
        else if (movement.IsCheckGournd)
        {
            unGroundTime = 0f;
            IsGround = true;
        }
        else
        {
            unGroundTime += Time.deltaTime;
            IsGround = unGroundTime < groundDelayTime;
        }

        anim.SetFloat("MoveSpeed", normalizedSpeed, speedTime, Time.deltaTime);
        anim.SetBool("IsGround", IsGround);
        //anim.SetBool("IsJetpack", movement.IsJetPackAcitve);
        anim.SetBool("IsJetpack", movement.IsJetPackAcitve);
        anim.SetBool("IsLanding", movement.IsLanding);
        anim.SetFloat("VerticalSpeed", movement.VerticalVelocity);

        if (movement.JumpStartedThisFrame)
            anim.SetTrigger("JumpTrigger");

        Vector3 localMoveDirection = Vector3.zero;

        if (horizontalVelocity.sqrMagnitude > 0.001f)
            localMoveDirection = transform.InverseTransformDirection(horizontalVelocity.normalized);

        Vector2 animationMoveDir;

        if (movement.IsJetPackAcitve)
        {
            animationMoveDir = movement.AirMoveDirection;
        }
        else
        {
            animationMoveDir = new Vector2(localMoveDirection.x, localMoveDirection.z);
        }

        anim.SetFloat("DirXMove", animationMoveDir.x, speedTime, Time.deltaTime);
        anim.SetFloat("DirYMove", animationMoveDir.y, speedTime, Time.deltaTime);
        anim.SetBool("IsAiming", isAiming);
    }

    private void UpdateWeaponSocketPos()
    {

        if (currentItem == null || weaponSocketPos == null)
            return;

        weaponSocketPos.localEulerAngles = isAiming ? Vector3.zero : currentItem.IdleEuler;

        if (weaponAimCondtraint == null)
            return;

        float targetWeight = isAiming ? 1f : 0f;

        weaponAimCondtraint.weight = Mathf.MoveTowards(weaponAimCondtraint.weight, targetWeight, aimWeightChangedSpeed * Time.deltaTime);
    }

    private void UpdateLeftHandTarget()
    {
        if (leftArmIK == null)
            return;

        leftArmIK.weight = leftHandWeight;

        if (currentLeftGrid == null || leftHandIKTarget == null) 
            return;

        leftHandIKTarget.SetPositionAndRotation(currentLeftGrid.position, currentLeftGrid.rotation);
    }

    public void SetAiming(bool value)
    {
        isAiming = value;
    }

    public void SetHoldItem(HoldItemRuntime item)
    {
        currentItem = item;

        if(currentItem == null)
        {
            currentLeftGrid = null;
            leftHandWeight = 0f;

            anim.runtimeAnimatorController = defaultAnimationController;
            anim.SetTrigger("UnequipTrigger");
            anim.SetBool("HasWeapon", currentItem != null);
            return;
        }

        currentLeftGrid = item.SecondGrip;

        leftHandWeight = currentLeftGrid != null ? 1f : 0f;

        anim.runtimeAnimatorController = item.AnimationController;
        anim.SetTrigger("EquipTrigger");
        anim.SetBool("HasWeapon", currentItem != null);
    }

    public void PlayerAttack()
    {
        anim.SetTrigger("AttackTrigger");
    }
}
