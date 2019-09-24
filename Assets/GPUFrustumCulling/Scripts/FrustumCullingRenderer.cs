using UnityEngine;
using System;

namespace FS2.GPUFrustumCulling
{
    [Serializable]
    public class FrustumCullingRenderer : FrustumCullingComponent<Renderer>
    {
        public FrustumCullingRenderer(GameObject gameObject) : base(gameObject)
        {
            if(isValid)
            {
                SetDefaultVisible(component.enabled);
            }
        }

        protected override void SetVisible(Renderer component, bool value)
        {
            component.enabled = value;
        }
    }
}