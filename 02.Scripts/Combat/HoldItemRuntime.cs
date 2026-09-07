using UnityEngine;

public class HoldItemRuntime : MonoBehaviour
{
    [Header("Components")]
    [SerializeField]
    private HoldItemDefine define;
    [SerializeField]
    private RuntimeAnimatorController animationController;

    [Header("Grips")]
    [SerializeField]
    private Transform primaryGrip;
    [SerializeField]
    private Transform secondGrip;

    [Header("Weapon Pos")]
    [SerializeField]
    private Transform aimTransform;
    [SerializeField]
    private Vector3 idleEuler;
    [SerializeField]
    private float poseChaneSpeed = 360f;

    public HoldItemDefine Define => define;
    public RuntimeAnimatorController AnimationController => animationController;

    public Transform PrimaryGrip => primaryGrip;
    public Transform SecondGrip => secondGrip;
    public Transform AimTransform => aimTransform;

    public Vector3 IdleEuler => idleEuler;

    public float PoseChangeSpeed => poseChaneSpeed;
    public bool CanAim => primaryGrip != null && aimTransform != null;
}
