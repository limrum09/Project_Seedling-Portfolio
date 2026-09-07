using UnityEngine;
public class CameraController : MonoBehaviour
{
    [SerializeField]
    private CameraSwitcher switcher;
    [SerializeField]
    private BuildCameraMovement cameraMovment;

    private bool isBuildMode;

    /// <summary>
    /// 기본 Camera Mode 초기화
    /// </summary>
    private void Awake()
    {
        isBuildMode = false;
    }

    /// <summary>
    /// Player Camera Target을 일반 Camera와 Build Camera에 연결
    /// </summary>
    /// <param name="player">Local Player Camera Target</param>
    public void BindPlayer(Transform player)
    {
        switcher.SetPlayerTarget(player);
        cameraMovment.SetPlayerTransform(player);
    }

    /// <summary>
    /// Build Camera가 사용할 Placement Input 연결
    /// </summary>
    /// <param name="inputSource">Placement 입력 접근점</param>
    public void BindInput(IPlacementInputSource inputSource)
    {
        cameraMovment.BindInput(inputSource);
    }

    public void SetGamePlayerTarget(Transform target)
    {
        switcher.SetGamePlayerTarget(target);
    }

    public void SetVehicleTarget(Transform target)
    {
        switcher.SetVehicleTarget(target);
    }

    /// <summary>
    /// 일반 Camera와 Build Camera Mode 전환
    /// </summary>
    /// <param name="getBuildMode">Build Camera Mode 활성 여부</param>
    public void SetBuildMode(bool getBuildMode)
    {
        if (isBuildMode == getBuildMode)
            return;

        isBuildMode = getBuildMode;

        if (isBuildMode)
        {
            cameraMovment.Begin();
            switcher.SetBuildMode(true);
        }
        else
        {
            cameraMovment.End();
            switcher.SetBuildMode(false);
        }
    }
}
