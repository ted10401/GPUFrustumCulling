using UnityEngine;

namespace FS2.GPUFrustumCulling
{
    public abstract class FrustumCullingRenderersMonoBehaviourBase<T> : MonoBehaviour where T : FrustumCullingBase
    {
        [SerializeField] protected T m_frustumCullingBase;

        protected abstract void Reset();

        private void OnEnable()
        {
            if (m_frustumCullingBase != null && m_frustumCullingBase.isValid)
            {
                GPUFrustumCullingManager.Instance?.Register(m_frustumCullingBase);
            }
        }

        private void OnDisable()
        {
            if (m_frustumCullingBase != null && m_frustumCullingBase.isValid)
            {
                GPUFrustumCullingManager.Instance?.Unregister(m_frustumCullingBase);
            }
        }
    }
}