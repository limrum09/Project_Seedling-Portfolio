using System;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Build Camera 이동, 회전과 Zoom 처리
/// </summary>
public class BuildCameraMovement : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField]
    private Camera worldCamera;
    [SerializeField]
    private CinemachineCamera buildCamera;
    [SerializeField]
    private CinemachineFollow buildFollow;
    [SerializeField]
    private CinemachineHardLookAt buildLookAt;

    [Header("Objects")]
    [SerializeField]
    private Transform buildTarget;

    [Header("Values")]
    [SerializeField]
    private float moveSpeed = 10f;
    [SerializeField]
    private float zoomSpeed = 10f;
    [SerializeField]
    private float rotateSpeed = 10f;
    [SerializeField]
    private float minZoomDistance = 20f;
    [SerializeField]
    private float maxZoomDistance = 150f;

    private IPlacementInputSource inputSource;

    private Vector3 originFollowOffset;
    private Vector3 origindLookAtOffset;

    private float currentZoomDistance;
    private float yaw;
    private Vector3 zoomDir;

    private bool isCameraMove;
    private Transform playerTf;
    private Vector3 targetPos;

    /// <summary>
    /// Build Camera 이동 상태와 Zoom 기준값 초기화
    /// </summary>
    private void Awake()
    {
        isCameraMove = false;
        playerTf = null;

        targetPos = Vector3.zero;
        InitZoom();
    }

    /// <summary>
    /// Build Camera 활성 상태에서 이동, Zoom과 회전 입력 처리
    /// </summary>
    private void Update()
    {
        if (!isCameraMove)
            return;

        MoveCamera();
        CameraZoomInOut();
        RotateCamera();
    }

    /// <summary>
    /// Build Camera가 사용할 Placement Input 연결
    /// </summary>
    /// <param name="source">Placement 입력 접근점</param>
    public void BindInput(IPlacementInputSource source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        inputSource = source;
    }

    /// <summary>
    /// 기본 Camera Offset과 Zoom 방향 초기화
    /// </summary>
    private void InitZoom()
    {
        originFollowOffset = buildFollow.FollowOffset;
        origindLookAtOffset = buildLookAt.LookAtOffset;

        Vector3 cameraFromLookPoint = buildFollow.FollowOffset - buildLookAt.LookAtOffset;

        currentZoomDistance = cameraFromLookPoint.magnitude;
        zoomDir = cameraFromLookPoint.normalized;
    }

    /// <summary>
    /// Camera 회전 입력을 Build Target에 적용
    /// </summary>
    private void RotateCamera()
    {
        if (!inputSource.ModifierHeld)
            return;

        Vector2 lookDelta = inputSource.CameraRotate;

        yaw += lookDelta.x * rotateSpeed;
        yaw = Mathf.Repeat(yaw, 360f);

        buildTarget.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    /// <summary>
    /// Camera 이동 입력을 Build Target 위치에 적용
    /// </summary>
    private void MoveCamera()
    {
        Vector2 moveInput = inputSource.CameraMove;

        Vector3 forward = Vector3.ProjectOnPlane(buildTarget.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(buildTarget.right, Vector3.up).normalized;

        if (forward.sqrMagnitude <= 0.001f && right.sqrMagnitude <= 0.001f)
            return;

        Vector3 moveDir = forward * moveInput.y + right * moveInput.x;

        targetPos += moveDir.normalized * moveSpeed * Time.deltaTime;

        buildTarget.position = Vector3.Lerp(buildTarget.position, targetPos, 10f * Time.deltaTime);
    }

    /// <summary>
    /// Camera Zoom 입력을 Follow Offset에 적용
    /// </summary>
    private void CameraZoomInOut()
    {
        float scroll = inputSource.Zoom;

        if (Mathf.Abs(scroll) <= 0.01f)
            return;

        currentZoomDistance -= scroll * zoomSpeed;
        currentZoomDistance = Mathf.Clamp(currentZoomDistance, minZoomDistance, maxZoomDistance);

        buildFollow.FollowOffset = buildLookAt.LookAtOffset + zoomDir * currentZoomDistance;
    }

    /// <summary>
    /// Build Camera 기준 위치에 사용할 Player Transform 설정
    /// </summary>
    /// <param name="playerTarget">Local Player Camera Target</param>
    public void SetPlayerTransform(Transform playerTarget)
    {
        if (playerTarget == null)
            throw new ArgumentNullException(nameof(playerTarget));

        playerTf = playerTarget;
    }

    /// <summary>
    /// Player 위치와 현재 World Camera 방향을 기준으로 Build Camera 이동 시작
    /// </summary>
    public void Begin()
    {
        Vector3 playerPos = new Vector3(playerTf.position.x, playerTf.position.y, playerTf.position.z);
        buildTarget.position = playerPos;
        targetPos = buildTarget.position;

        yaw = worldCamera.transform.eulerAngles.y;

        isCameraMove = true;
    }

    /// <summary>
    /// Build Camera 이동을 종료하고 기본 Offset 복원
    /// </summary>
    public void End()
    {
        Vector3 playerPos = new Vector3(playerTf.position.x, playerTf.position.y, playerTf.position.z);

        buildTarget.position = playerPos;
        buildTarget.rotation = Quaternion.identity;

        buildFollow.FollowOffset = originFollowOffset;
        buildLookAt.LookAtOffset = origindLookAtOffset;

        isCameraMove = false;
    }
}
