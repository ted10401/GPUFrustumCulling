using UnityEngine;

public class GPUFrustumCullingTool : MonoBehaviour
{
    [SerializeField] private GPUBounds[] gPUBounds;

    [ContextMenu("Execute")]
    private void Execute()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Vector3[] centers = new Vector3[renderers.Length];
        Vector3[] extents = new Vector3[renderers.Length];
        uint[] visibles = new uint[renderers.Length];
        gPUBounds = new GPUBounds[renderers.Length];

        Collider cacheCollider;
        bool tempCollider = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            gPUBounds[i] = new GPUBounds();
            gPUBounds[i].isStatic = true;
            gPUBounds[i].renderer = renderers[i];

            cacheCollider = renderers[i].GetComponent<Collider>();
            tempCollider = cacheCollider == null;
            if (tempCollider)
            {
                cacheCollider = renderers[i].gameObject.AddComponent<BoxCollider>();
            }
            else
            {
                gPUBounds[i].collider = cacheCollider;
            }

            gPUBounds[i].center = cacheCollider.bounds.center;
            gPUBounds[i].extents = cacheCollider.bounds.extents;
            gPUBounds[i].visible = (uint)(renderers[i].enabled ? 1 : 0);

            if(tempCollider)
            {
                DestroyImmediate(cacheCollider);
            }
        }
    }

    private void Start()
    {
        GPUFrustumCullingManager.Instance.Register(gPUBounds);
    }

    private void OnDisable()
    {
        GPUFrustumCullingManager.Instance.Unregister(gPUBounds);
    }
}
