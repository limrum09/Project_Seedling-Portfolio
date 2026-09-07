using UnityEngine;

public class DoorInteraction : MonoBehaviour
{
    [SerializeField]
    private Door door;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerInteraction interaction))
        {
            Debug.Log("Enter Player");
            interaction.RegistInteract(door);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out PlayerInteraction interaction))
        {
            interaction.UnRegisterInteract(door);

            door.Close();
        }
    }
}
