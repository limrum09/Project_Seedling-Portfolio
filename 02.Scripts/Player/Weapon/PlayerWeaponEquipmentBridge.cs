using UnityEngine;

public class PlayerWeaponEquipmentBridge : MonoBehaviour
{
    [SerializeField]
    private PlayerWeaponReplica replica;
    [SerializeField]
    private HoldItemEquipmentService equipmentService;

    private void OnEnable()
    {
        replica.OnWeaponStateChanged += HandleWeaponStateChanged;

        if (replica.IsInitialized)
            HandleWeaponStateChanged(replica.GetSnapshot());
    }

    private void OnDisable()
    {
        replica.OnWeaponStateChanged -= HandleWeaponStateChanged;
    }

    private void HandleWeaponStateChanged(PlayerWeaponSnapshot snapshot)
    {
        if (string.IsNullOrEmpty(snapshot.MainWeaponId))
        {
            equipmentService.UnEquip();
            return;
        }

        equipmentService.TryEquip(snapshot.MainWeaponId);
    }
}
