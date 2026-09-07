using System.Collections.Generic;
using UnityEngine;

public class CorridorRoutePreviewController : MonoBehaviour
{
    [SerializeField]
    private Material validMaterial;
    [SerializeField]
    private Material invalidMaterial;

    private readonly List<GameObject> previews = new List<GameObject>();
    private readonly List<BuildingDefine> previewDefines = new List<BuildingDefine>();

    private bool NeedsRebuild(CorridorRoutePlan plan)
    {
        if (previews.Count != plan.Modules.Count)
            return true;

        if(previewDefines.Count != plan.Modules.Count) 
            return true;

        for (int i = 0; i < plan.Modules.Count; i++)
        {
            if (previewDefines[i] != plan.Modules[i].Define)
                return true;
        }

        return false;
    }

    private void Rebuild(CorridorRoutePlan plan)
    {
        Hide();

        foreach(CorridorModulePlan module in plan.Modules)
        {
            if(module.Define == null)
            {
                Hide();
                return;
            }

            GameObject preview = Instantiate(module.Define.PreviewPrefab, transform);

            foreach (Collider collider in preview.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;

            previews.Add(preview);
            previewDefines.Add(module.Define);
        }
    }

    private void SetMaterial(GameObject preview, Material material)
    {
        if (preview == null || material == null)
            return;

        foreach (Renderer renderer in preview.GetComponentsInChildren<Renderer>(true))
            renderer.sharedMaterial = material;
    }

    public void Show(CorridorRoutePlan plan, bool canPlace)
    {
        if(plan == null || plan.Modules == null || plan.Modules.Count == 0)
        {
            Hide();
            return;
        }

        if(NeedsRebuild(plan))
            Rebuild(plan);

        if (previews.Count != plan.Modules.Count)
            return;

        Material targetMaterial = canPlace ? validMaterial : invalidMaterial;

        for(int i = 0; i < plan.Modules.Count; i++)
        {
            CorridorModulePlan module = plan.Modules[i];

            GameObject preview = previews[i];

            if (preview == null)
                continue;

            preview.transform.SetPositionAndRotation(module.Pos, module.Rotation);

            SetMaterial(preview, targetMaterial);
        }

    }

    public void Hide()
    {
        foreach(GameObject preview in previews)
        {
            if (preview != null)
                Destroy(preview);
        }

        previews.Clear();
        previewDefines.Clear();
    }
}
