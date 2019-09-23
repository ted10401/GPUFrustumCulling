using UnityEngine;

public class GPUFrustumCullingComponent : MonoBehaviour
{
    public GPUBounds gPUBounds;

    private void Reset()
    {
        gPUBounds = new GPUBounds();
        gPUBounds.isStatic = true;
        gPUBounds.renderer = GetComponent<Renderer>();
        gPUBounds.collider = GetComponent<Collider>();
        gPUBounds.center = gPUBounds.collider.bounds.center;
        gPUBounds.extents = gPUBounds.collider.bounds.extents;
        gPUBounds.visible = (uint)(gPUBounds.renderer.enabled ? 1 : 0);
        gPUBounds.valid = gPUBounds.renderer.enabled;
    }

    private void Awake()
    {
        gPUBounds.center = gPUBounds.collider.bounds.center;
        gPUBounds.extents = gPUBounds.collider.bounds.extents;
        gPUBounds.visible = (uint)(gPUBounds.renderer.enabled ? 1 : 0);
        gPUBounds.valid = gPUBounds.renderer.enabled;
    }

    private void OnEnable()
    {
        if(gPUBounds == null || !gPUBounds.valid)
        {
            return;
        }

        GPUFrustumCullingManager.Instance.Register(gPUBounds);
    }

    private void OnDisable()
    {
        if (gPUBounds == null || !gPUBounds.valid)
        {
            return;
        }

        GPUFrustumCullingManager.Instance.Unregister(gPUBounds);
    }
}
