using System.Collections.Generic;
using UnityEngine;

public class HoldItemFactory
{
    private const string ResourcesPath = "Weapons/";

    private readonly Dictionary<string, HoldItemRuntime> holdItemCashes = new ();

    public HoldItemRuntime ItemCreate(string itemId, Transform parent)
    {
        if(!holdItemCashes.TryGetValue(itemId, out HoldItemRuntime item))
        {
            item = Resources.Load<HoldItemRuntime>(ResourcesPath + itemId);

            if (item == null)
                return null;

            holdItemCashes.Add(itemId, item);
        }

        return Object.Instantiate(item, parent);
    }
}
