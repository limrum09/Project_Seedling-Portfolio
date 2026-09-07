using UnityEditor;
using UnityEngine;

/// <summary>
/// 유저 조작과 카메라 동작에 적용할 게임 플레이 모드를 정의
/// </summary>
public enum GamePlayMode
{
    Normal,
    Build,
    Furniture,
    Vehicle
}

/// <summary>
/// Building의 상태 변화를 감지해 플레이어 조작과 카메라 모드를 전환
/// 건설 상태를 직접 변경하거나 건설 규칙을 실행하지는 않음
/// </summary>
public class GameplayModeController : MonoBehaviour
{
    [SerializeField]
    private CameraController viewCamera;

    private Player player;
    private IBuildingPlacementAccess buildingAccess;
    private IFurniturePlacementAccess furnitureAccess;
    private IInputMapControl inputMapControl;
    private PlayerVehicleAttachment vehicleAttachment;

    public GamePlayMode CurrentMode { get; private set; }

    /// <summary>
    /// 기본 Gameplay Mode 설정
    /// </summary>
    private void Awake()
    {
        CurrentMode = GamePlayMode.Normal;
    }

    /// <summary>
    /// Gameplay Mode 접근점 연결 해제
    /// </summary>
    private void OnDestroy()
    {
        Unbind();
    }

    /// <summary>
    /// 현재 Player와 Placement 상태 이벤트 연결 해제
    /// </summary>
    private void Unbind()
    {
        if (buildingAccess != null)
            buildingAccess.OnChangedState -= ChangedBuildState;

        if (furnitureAccess != null)
            furnitureAccess.OnChangedState -= ChangedFurniureState;

        if (vehicleAttachment != null)
            vehicleAttachment.OnStateChanged -= ChagnedVechicleMountState;

        player = null;
        buildingAccess = null;
        furnitureAccess = null;
        inputMapControl = null;
    }

    /// <summary>
    /// 변경된 건설 상태를 게임 플레이 모드에 반영
    /// </summary>
    /// <param name="buildState">변경된 건설 상태</param>
    private void ChangedBuildState(BuildState buildState)
    {
        RefreshMode(false);
    }

    /// <summary>
    /// 변경된 가구 상태를 게임 플레이 모드에 반영
    /// </summary>
    /// <param name="furnitureState">변경된 가구 상태</param>
    private void ChangedFurniureState(FurnitureState furnitureState)
    {
        RefreshMode(false);
    }

    private void ChagnedVechicleMountState(VehicleMountState state, VehicleSeat seat)
    {
        RefreshMode(false);
    }

    /// <summary>
    /// 현재 Gameplay Mode에 Player 조작 권한과 Action Map 상태 적용
    /// </summary>
    private void ApplyMode()
    {
        switch (CurrentMode)
        {
            case GamePlayMode.Build:
                player.SetControlPermissions(PlayerControlPermissions.None);
                inputMapControl.SetPlayerActionsEnabled(false);
                inputMapControl.SetPlacementActionsEnabled(true);

                viewCamera.SetBuildMode(true);
                break;
            case GamePlayMode.Furniture:
                player.SetControlPermissions(PlayerControlPermissions.Movement);
                inputMapControl.SetPlayerActionsEnabled(true);
                inputMapControl.SetPlacementActionsEnabled(true);

                viewCamera.SetGamePlayerTarget(player.CameraTarget);
                viewCamera.SetBuildMode(false);
                break;
            case GamePlayMode.Vehicle:
                player.SetControlPermissions(PlayerControlPermissions.None);
                inputMapControl.SetPlayerActionsEnabled(true);
                inputMapControl.SetPlacementActionsEnabled(false);

                viewCamera.SetVehicleTarget(vehicleAttachment.CurrentSeat.CameraTarget);
                viewCamera.SetBuildMode(false);
                break;
            case GamePlayMode.Normal:
                player.SetControlPermissions(PlayerControlPermissions.All);
                inputMapControl.SetPlayerActionsEnabled(true);
                inputMapControl.SetPlacementActionsEnabled(false);

                viewCamera.SetGamePlayerTarget(player.CameraTarget);
                viewCamera.SetBuildMode(false);
                break;
        }
    }

    /// <summary>
    /// Building과 Furniture 상태에 맞는 Gameplay Mode 갱신
    /// </summary>
    /// <param name="force">현재 Mode와 같아도 다시 적용할지 여부</param>
    private void RefreshMode(bool force)
    {
        GamePlayMode nextMode;

        if (vehicleAttachment.IsVehicleMode)
            nextMode = GamePlayMode.Vehicle;
        else if (buildingAccess.CurrentState != BuildState.None)
            nextMode = GamePlayMode.Build;
        else if (furnitureAccess.CurrentState != FurnitureState.None)
            nextMode = GamePlayMode.Furniture;
        else
            nextMode = GamePlayMode.Normal;

        if (!force && CurrentMode == nextMode)
            return;

        CurrentMode = nextMode;
        ApplyMode();
    }

    /// <summary>
    /// Local Player와 Gameplay Mode 관련 접근점 연결
    /// </summary>
    /// <param name="getPlayer">연결할 Local Player</param>
    /// <param name="getBuildingAccess">Building 접근점</param>
    /// <param name="getFurnitureAccess">Furniture 접근점</param>
    /// <param name="getInputMapControl">Local Input Action Map 접근점</param>
    public void Bind(Player getPlayer, IBuildingPlacementAccess getBuildingAccess, IFurniturePlacementAccess getFurnitureAccess, IInputMapControl getInputMapControl)
    {
        if (getPlayer == null)
            throw new System.ArgumentNullException(nameof(getPlayer));

        if (getBuildingAccess == null)
            throw new System.ArgumentNullException(nameof(getBuildingAccess));

        if (getFurnitureAccess == null)
            throw new System.ArgumentNullException(nameof(getFurnitureAccess));

        if (getInputMapControl == null)
            throw new System.ArgumentNullException(nameof(getInputMapControl));

        Unbind();

        player = getPlayer;

        buildingAccess = getBuildingAccess;
        furnitureAccess = getFurnitureAccess;
        inputMapControl = getInputMapControl;
        vehicleAttachment = player.VehicleAttachment;

        buildingAccess.OnChangedState += ChangedBuildState;
        furnitureAccess.OnChangedState += ChangedFurniureState;
        vehicleAttachment.OnStateChanged += ChagnedVechicleMountState;

        RefreshMode(true);
    }
}
