using System.Collections.Generic;
using UnityEngine;

namespace FS2.GPUFrustumCulling
{
    public class FrustumCullingScene : MonoBehaviour
    {
        [SerializeField] private FrustumCullingRenderer[] m_frustumCullingRenderers;

        [ContextMenu("Execute")]
        private void Execute()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            List<FrustumCullingRenderer> frustumCullingRendererList = new List<FrustumCullingRenderer>();
            FrustumCullingRenderer cacheFrustumCullingRenderer;

            for (int i = 0; i < renderers.Length; i++)
            {
                cacheFrustumCullingRenderer = new FrustumCullingRenderer(renderers[i].gameObject);
                frustumCullingRendererList.Add(cacheFrustumCullingRenderer);
            }

            m_frustumCullingRenderers = frustumCullingRendererList.ToArray();
        }

        private void OnEnable()
        {
            if (m_frustumCullingRenderers == null || m_frustumCullingRenderers.Length == 0)
            {
                return;
            }

            GPUFrustumCullingManager.Instance?.Register(m_frustumCullingRenderers);
        }

        private void OnDisable()
        {
            if (m_frustumCullingRenderers == null || m_frustumCullingRenderers.Length == 0)
            {
                return;
            }

            GPUFrustumCullingManager.Instance?.Unregister(m_frustumCullingRenderers);
        }
    }
}