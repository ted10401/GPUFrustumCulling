using UnityEngine;

namespace FS2.GPUFrustumCulling
{
    public class FrustumCullingRendererMonoBehaviour : MonoBehaviour
    {
        public FrustumCullingRenderer frustumCullingRenderer;

        private void Reset()
        {
            frustumCullingRenderer = new FrustumCullingRenderer(gameObject);
        }

        private void OnEnable()
        {
            if(frustumCullingRenderer != null && frustumCullingRenderer.isValid)
            {
                frustumCullingRenderer.center = frustumCullingRenderer.transform.position;
                GPUFrustumCullingManager.Instance?.Register(frustumCullingRenderer);
            }
        }

        private void OnDisable()
        {
            if (frustumCullingRenderer != null && frustumCullingRenderer.isValid)
            {
                GPUFrustumCullingManager.Instance?.Unregister(frustumCullingRenderer);
            }
        }
    }
}