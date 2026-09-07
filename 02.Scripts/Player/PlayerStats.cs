using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    private float walkSpeed = 5f;
    [SerializeField]
    private float runSpeedMultiplier = 2f;
    [SerializeField]
    private float jumpHeight = 0.8f;

    [Header("Runtime Resource")]
    [SerializeField]
    private int maxHP = 100;
    [SerializeField]
    private int maxFood = 100;
    [SerializeField]
    private int maxOxygen = 100;
    [SerializeField]
    private float maxEnergy = 100f;
    

    public float WalkSpeed => walkSpeed;
    public float RunSpeed => walkSpeed * runSpeedMultiplier;
    public float JumpHeight => jumpHeight;

    public int MaxHP => maxHP;
    public int MaxFood => maxFood;
    public int MaxOxygen => maxOxygen;
    public float MaxEnergy => maxEnergy;
    

    public void SetWalkSpeed(float value) => walkSpeed = Mathf.Max(0f, value);

    public void SetJumpHeight(float value) => jumpHeight = Mathf.Max(0f, value);
}
