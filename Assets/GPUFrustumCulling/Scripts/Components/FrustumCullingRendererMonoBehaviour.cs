
namespace FS2.GPUFrustumCulling
{
    public class FrustumCullingRendererMonoBehaviour : FrustumCullingRenderersMonoBehaviourBase<FrustumCullingRenderer>
    {
        protected override void Reset()
        {
            m_frustumCullingBase = new FrustumCullingRenderer(gameObject);
        }
    }
}