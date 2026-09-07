using System;
using UnityEngine;

public class PlayerFacing : MonoBehaviour
{
    [SerializeField]
    private Transform weaponSocket;
    [SerializeField]
    private Transform weaponAimTarget;
    [SerializeField, Min(10f)]
    private float defaultAimDistance = 20f;
    [SerializeField]
    private float rotateSpeed = 560f;
    [SerializeField]
    private float aimHoldDuration = 4.5f;
    [SerializeField]
    private float maxAimAngle;
    [SerializeField]
    private float minAimAngle;
    [SerializeField]
    private float aimDeadZone = 0.35f;

    private Vector3 lastAimDir;
    private Vector3 aimPoint;
    private Vector3 targetDirection;
    private float aimReleaseTime;
    private bool hasAimPoint;
    private bool isTargetDirection;
    private bool isAimLock;

    public event Action<bool> OnAimLockChanged;

    private void Awake()
    {
        lastAimDir = transform.forward;
        lastAimDir.y = 0f;
        lastAimDir.Normalize();
    }

    private void Update()
    {
        if (isAimLock && Time.time >= aimReleaseTime)
            SetAimLock(false);

        if (isTargetDirection)
            RotatePlayer();

        UpdateWeaponAimTarget();
    }

    private void UpdateWeaponAimTarget()
    {
        if (weaponSocket == null || weaponAimTarget == null)
            return;

        if(isAimLock && hasAimPoint)
        {
            weaponAimTarget.position = aimPoint;
            return;
        }

        Vector3 forward = transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.001f)
            return;

        forward.Normalize();

        weaponAimTarget.position = weaponSocket.position + forward * defaultAimDistance;
    }

    private void RotatePlayer()
    {
        if (!isTargetDirection)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
    }

    private void SetTargetDirection(Vector3 worldDirection)
    {
        worldDirection.y = 0f;

        if (worldDirection.sqrMagnitude <= 0.001f)
            return;

        targetDirection = worldDirection.normalized;
        isTargetDirection = true;
    }

    private void SetAimLock(bool value)
    {
        if (isAimLock == value)
            return;

        isAimLock = value;
        OnAimLockChanged?.Invoke(isAimLock);
    }

    public bool TryResolveAimPoint(Vector3 aimPoint, out Vector3 resolveAimPoint)
    {
        resolveAimPoint = aimPoint;

        Vector3 origin = weaponSocket.position;
        Vector3 dir = aimPoint - origin;

        if (dir.sqrMagnitude <= 0.001f)
            return false;

        float distance = dir.magnitude;

        Vector3 rawHorizontal = Vector3.ProjectOnPlane(dir, Vector3.up);

        float horizontalDis = rawHorizontal.magnitude;

        Vector3 horizontalDir;

        if (horizontalDis <= aimDeadZone)
        {
            horizontalDir = lastAimDir;
        }   
        else
        {
            horizontalDir = rawHorizontal / horizontalDis;
            lastAimDir = horizontalDir;
        }

        if (horizontalDir.sqrMagnitude <= 0.001f)
        {
            horizontalDir = transform.forward;
            horizontalDir.y = 0f;
            horizontalDir.Normalize();

            lastAimDir = horizontalDir;
        }

        float pitchAngle = Mathf.Atan2(dir.y, horizontalDis) * Mathf.Rad2Deg;

        float maxPitch = maxAimAngle;
        float minPitch = -minAimAngle;
        
        bool isValid = pitchAngle >= minPitch && pitchAngle <= maxPitch;

        float clampedPitch = Mathf.Clamp(pitchAngle, minPitch, maxPitch);
        float pitchRad = clampedPitch * Mathf.Deg2Rad;

        Vector3 resolveDir = horizontalDir * Mathf.Cos(pitchRad) + Vector3.up * Mathf.Sin(pitchRad);
        
        resolveAimPoint = origin + resolveDir * distance;

        return isValid;
    }

    public void SetAimPoint(Vector3 worldPoint)
    {
        aimPoint = worldPoint;
        hasAimPoint = true;

        Vector3 worldDir = worldPoint - transform.position;
        worldDir.y = 0f;

        if (worldDir.sqrMagnitude <= 0.001f)
            return;

        SetAimLock(true);
        aimReleaseTime = Time.time + aimHoldDuration;

        SetTargetDirection(worldDir);
    }

    public void SetMovementDirection(float horizontal, float vertical, Vector3 cameraForward)
    {
        if (isAimLock)
            return;

        bool hasMoveInput = Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f;

        if (!hasMoveInput)
            return;

        Vector3 dir = cameraForward;

        if (vertical < -0.01f)
            dir = -cameraForward;

        SetTargetDirection(dir);
    }
}
