using UnityEngine;
using System;

namespace FS2.FrustumCulling
{
    [Serializable]
    public class FrustumCullingRenderers : FrustumCullingComponents<Renderer>
    {
        public FrustumCullingRenderers(GameObject gameObject, bool isStatic = true) : base(gameObject, isStatic)
        {
            if(isValid)
            {
                SetDefaultVisible(components[0].enabled);
            }
        }

        protected override bool GetEnabled(Renderer component)
        {
            return component.enabled;
        }

        protected override void SetVisible(Renderer component, bool value)
        {
            if (component == null)
            {
                JSLCore.JSLDebug.LogError("[FrustumCullingRenderers] Renderer is null.");
                return;
            }
            isVisible = value;
            visible = (uint)(value ? 1 : 0);
            component.enabled = value;
        }
    }
}