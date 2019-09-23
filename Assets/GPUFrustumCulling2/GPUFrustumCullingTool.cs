using UnityEngine;
using System.Collections.Generic;

public class GPUFrustumCullingTool : MonoBehaviour
{
    [SerializeField] private GPUBounds[] gPUBounds;

    [ContextMenu("Execute")]
    private void Execute()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        List<GPUBounds> gPUBoundsList = new List<GPUBounds>();
        GPUBounds cacheGPUBrounds;
        Collider cacheCollider;
        bool tempCollider = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            if(!renderers[i].enabled)
            {
                continue;
            }

            cacheGPUBrounds = new GPUBounds();
            cacheGPUBrounds.isStatic = true;
            cacheGPUBrounds.renderer = renderers[i];

            cacheCollider = renderers[i].GetComponent<Collider>();
            tempCollider = cacheCollider == null;
            if (tempCollider)
            {
                cacheCollider = renderers[i].gameObject.AddComponent<BoxCollider>();
            }
            else
            {
                cacheGPUBrounds.collider = cacheCollider;
            }

            cacheGPUBrounds.center = cacheCollider.bounds.center;
            cacheGPUBrounds.extents = cacheCollider.bounds.extents;
            cacheGPUBrounds.visible = 1;
            cacheGPUBrounds.valid = true;

            if (tempCollider)
            {
                DestroyImmediate(cacheCollider);
            }

            gPUBoundsList.Add(cacheGPUBrounds);
        }

        gPUBounds = gPUBoundsList.ToArray();
    }

    private void OnEnable()
    {
        if(gPUBounds == null || gPUBounds.Length == 0)
        {
            return;
        }

        GPUFrustumCullingManager.Instance.Register(gPUBounds);
    }

    private void OnDisable()
    {
        if (gPUBounds == null || gPUBounds.Length == 0)
        {
            return;
        }

        GPUFrustumCullingManager.Instance.Unregister(gPUBounds);
    }
}
