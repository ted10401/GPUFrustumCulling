using UnityEngine;
using System;

namespace FS2.FrustumCulling
{
    [Serializable]
    public class FrustumCullingLight : FrustumCullingBehaviour<Light>
    {
        public FrustumCullingLight(GameObject gameObject) : base(gameObject)
        {

        }

        public FrustumCullingLight(GameObject gameObject, bool defaultVisible) : base(gameObject)
        {
            SetVisible(!defaultVisible);
            SetVisible(defaultVisible);
        }
    }
}