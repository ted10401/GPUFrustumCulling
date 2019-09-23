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
    }

    private void Awake()
    {
        gPUBounds.center = gPUBounds.collider.bounds.center;
        gPUBounds.extents = gPUBounds.collider.bounds.extents;
        gPUBounds.visible = (uint)(gPUBounds.renderer.enabled ? 1 : 0);
    }

    private void OnEnable()
    {
        GPUFrustumCullingManager.Instance.Register(gPUBounds);
    }

    private void OnDisable()
    {
        GPUFrustumCullingManager.Instance.Unregister(gPUBounds);
    }
}
