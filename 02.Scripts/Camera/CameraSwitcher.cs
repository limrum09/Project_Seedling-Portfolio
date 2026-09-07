using Unity.Cinemachine;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [Header("Basix Camera")]
    [SerializeField]
    private CinemachineCamera basicCamera;
    [SerializeField]
    private CinemachineInputAxisController basicInputCtr;

    [Header("Build Camera")]
    [SerializeField]
    private CinemachineCamera buildCamera;

    [Header("VehicleCamera")]
    [SerializeField]
    private CinemachineCamera vehicleCamera;
    [SerializeField]
    private CinemachineInputAxisController vehicleInputCtr;

    [Header("Prioritys")]
    [SerializeField]
    private int gameplayPriority = 10;
    [SerializeField]
    private int buildPriority = 20;
    [SerializeField]
    private int vehiclePriority = 20;

    private bool isBuildMode;
    private bool isVehicleMode;

    private void Awake()
    {
        ApplyCameraState();
    }

    private void ApplyCameraState()
    {
        bool useBasicCamera = !isBuildMode && !isVehicleMode;
        bool useVehicleCamera = !isBuildMode && isVehicleMode;

        basicCamera.Priority = useBasicCamera ? gameplayPriority : 0;
        buildCamera.Priority = isBuildMode ? buildPriority : 0;
        vehicleCamera.Priority = useVehicleCamera ? vehiclePriority : 0;

        basicInputCtr.enabled = useBasicCamera;
        vehicleInputCtr.enabled = useVehicleCamera;
    }

    /// <summary>
    /// 일반 플레이 카메라가 추적할 로컬 Player Target을 설정
    /// </summary>
    /// <param name="playerTarget">카메라가 추적할 PlayerTarget</param>
    public void SetPlayerTarget(Transform playerTarget)
    {
        basicCamera.Follow = playerTarget;
    }

    public void SetGamePlayerTarget(Transform gameplayTarget)
    {
        basicCamera.Follow = gameplayTarget;
        isVehicleMode = false;

        ApplyCameraState();
    }

    public void SetVehicleTarget(Transform vehicleTarget)
    {
        vehicleCamera.Follow = vehicleTarget;
        isVehicleMode = true;

        ApplyCameraState();
    }

    public void SetBuildMode(bool getBuildMode)
    {
        isBuildMode = getBuildMode;

        ApplyCameraState();
    }
}
