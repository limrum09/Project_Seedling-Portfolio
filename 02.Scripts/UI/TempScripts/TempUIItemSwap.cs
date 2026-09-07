using UnityEngine;

public class TempUIItemSwap : MonoBehaviour
{
    [SerializeField]
    private PlayerSpawner spawner;

    [SerializeField]
    private HoldItemRuntime rifle;
    [SerializeField]
    private HoldItemRuntime raser;
    [SerializeField]
    private HoldItemRuntime pistol;

    private Player player => spawner.CurrentLocalPlayer;

    private void ChangeItem(HoldItemRuntime item)
    {
        player.TryEquip(item);
    }

    public void ChangeRifle()
    {
        ChangeItem(rifle);
    }

    public void ChangeRaser()
    {
        ChangeItem(raser);
    }

    public void ChangePistol()
    {
        ChangeItem(pistol);
    }
}
