using UnityEngine;

namespace FS2.FrustumCulling
{
    public class FrustumCullingRendererMonoBehaviour : MonoBehaviour
    {
        private int m_instanceID;
        [SerializeField] private FrustumCullingRenderer m_frustumCullingRenderer;

        public void Reset()
        {
            m_frustumCullingRenderer = new FrustumCullingRenderer(gameObject);
        }

        private void Awake()
        {
            m_instanceID = GetInstanceID();
        }

        private void OnEnable()
        {
            if (m_frustumCullingRenderer != null)
            {
                FrustumCullingManager.Instance?.RegisterCPU(m_instanceID, m_frustumCullingRenderer);
            }
        }

        private void OnDisable()
        {
            if (m_frustumCullingRenderer != null)
            {
                FrustumCullingManager.Instance?.UnregisterGPU(m_instanceID);
            }
        }
    }
}