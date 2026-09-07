using UnityEngine;

public class Door : MonoBehaviour, IPlayerInteractable
{
    [SerializeField]
    private Animator anim;
    [SerializeField]
    private bool isConnector = true;

    private BuildingConnector owner;

    private bool isOpen;

    public bool CanInteraction => GetInteraction();

    private bool GetInteraction()
    {
        if (isConnector)
            return !owner.IsOccupied;
        else
            return true;
    }

    private void Open()
    {
        anim.SetBool("IsOpen", true);
        isOpen = true;
    }

    public void Close()
    {
        anim.SetBool("IsOpen", false);
        isOpen = false;
    }

    public void ChangedOccupied(bool value)
    {
        this.gameObject.SetActive(!value);
    }

    public void BindConnector(BuildingConnector connector) => owner = connector;

    public void Interact(PlayerInteractionContext context)
    {
        Debug.Log("Door Interac");

        if (!CanInteraction)
            return;

        if (isOpen)
            Close();
        else
            Open();
    }
}
