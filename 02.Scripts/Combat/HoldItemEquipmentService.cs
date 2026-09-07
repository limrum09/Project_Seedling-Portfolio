using System;
using UnityEngine;

public class HoldItemEquipmentService : MonoBehaviour
{
    [SerializeField]
    private HoldItemRuntime startingItem;
    [SerializeField]
    private Transform weaponSocketPos;

    private HoldItemFactory itemFactory;

    public HoldItemRuntime CurrentItem { get; private set; }

    public event Action<HoldItemRuntime> OnHoldItemChanged;

    private void Awake()
    {
        itemFactory = new HoldItemFactory();
    }

    private void Start()
    {
        if (CurrentItem == null && startingItem != null)
            TryEquip(startingItem);
    }

    public bool TryEquip(HoldItemRuntime item)
    {
        if(item == null)
            return false;

        if (CurrentItem != null && CurrentItem.Define == item.Define)
            return false;

        if(weaponSocketPos == null)
        {
            Debug.LogError($"{weaponSocketPos}의 연결 필요");
            return false;
        }

        HoldItemRuntime newItem = itemFactory.ItemCreate(item.Define.ItemID, weaponSocketPos);

        if (newItem == null)
        {
            Debug.Log("아이템 없음");
            return false;
        }
            

        HoldItemRuntime previousItem = CurrentItem;
        CurrentItem = newItem;

        OnHoldItemChanged?.Invoke(CurrentItem);

        if (previousItem != null)
        {
            previousItem.gameObject.SetActive(false);
            Destroy(previousItem.gameObject);
        }

        return true;
    }

    public bool TryEquip(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) 
            return false;

        if (CurrentItem != null && string.Equals(CurrentItem.Define.ItemID, itemId, StringComparison.Ordinal))
            return false;

        HoldItemRuntime newItem = itemFactory.ItemCreate(itemId, weaponSocketPos);

        if(newItem == null)
        {
            Debug.Log("아이템 없음");
            return false;
        }

        HoldItemRuntime prevItem = CurrentItem;

        CurrentItem = newItem;
        OnHoldItemChanged?.Invoke(CurrentItem);

        if(prevItem != null)
        {
            prevItem.gameObject.SetActive(false);
            Destroy(prevItem.gameObject);
        }

        return true;
    }

    public void UnEquip()
    {
        if (CurrentItem == null)
            return;

        HoldItemRuntime previousItem = CurrentItem;
        CurrentItem = null;

        OnHoldItemChanged?.Invoke(CurrentItem);
        Destroy(previousItem.gameObject);
    }
}
