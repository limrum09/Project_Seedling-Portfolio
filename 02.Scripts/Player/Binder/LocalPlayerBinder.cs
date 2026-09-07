using UnityEngine;

/// <summary>
/// 준비된 로컬 Plyaer를 UI, 카메라와 로컬 조작 시스템에 연결
/// </summary>
public class LocalPlayerBinder : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField]
    private LocalPlayerSpawner playerSpawner;

    [Header("Binder")]
    [SerializeField]
    private PlayerUIBinder uiBinder;
    [SerializeField]
    private ProductionWorldUIBinder productionWorldUIBinder;
    [SerializeField]
    private GameplayModeController gamePlayMode;

    [Header("Placement")]
    [SerializeField]
    private FurnitureController furnitureCtr;
    [SerializeField]
    private Building buildingCtr;

    [Header("Input")]
    [SerializeField]
    private BuildInputController buildInputController;
    [SerializeField]
    private FurnitureInputController furnitureInputController;

    [Header("Camera")]
    [SerializeField]
    private Camera worldCamera;
    [SerializeField]
    private CameraController cameraController;

    private Player currentPlayer;

    private void OnEnable()
    {
        playerSpawner.OnLocalPlayerReady += Bind;

        if (playerSpawner.CurrentLocalPlayer != null)
            Bind(playerSpawner.CurrentLocalPlayer);
    }

    private void OnDisable()
    {
        playerSpawner.OnLocalPlayerReady -= Bind;

        productionWorldUIBinder.Unbind();
    }

    /// <summary>
    /// 지정한 로컬 Player를 UI, 카메라와 배치 시스템에 연결
    /// </summary>
    /// <param name="player">연결할 로컬 Player</param>
    public void Bind(Player player)
    {
        player.SetCamera(worldCamera);

        buildInputController.BindInput(player.PlacementInputSource, worldCamera);
        furnitureInputController.BindInput(player.PlacementInputSource);
        cameraController.BindInput(player.PlacementInputSource);

        furnitureCtr.Bind(playerSpawner.CurrentAuthority.Consumption);
        buildingCtr.Bind(playerSpawner.CurrentAuthority.Consumption);

        uiBinder.Bind(player, buildingCtr, furnitureCtr);
        productionWorldUIBinder.Bind(worldCamera, playerSpawner.CurrentAuthority.ItemReceiver);
        gamePlayMode.Bind(player, buildingCtr, furnitureCtr, player.InputMapControl);
        cameraController.BindPlayer(player.CameraTarget);
    }
}
