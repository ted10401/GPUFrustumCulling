using UnityEngine;
using System;

namespace FS2.FrustumCulling
{
    [Serializable]
    public class FrustumCullingRenderer : FrustumCullingComponent<Renderer>
    {
        public FrustumCullingRenderer(GameObject gameObject) : base(gameObject)
        {
            SetVisible(!component.enabled);
            SetVisible(component.enabled);
        }

        public FrustumCullingRenderer(GameObject gameObject, bool defaultVisible) : base(gameObject)
        {
            SetVisible(!defaultVisible);
            SetVisible(defaultVisible);
        }

        protected override void SetVisible(Renderer component, bool value)
        {
            isVisible = value;
            visible = (uint)(value ? 1 : 0);
            component.enabled = value;
        }
    }
}