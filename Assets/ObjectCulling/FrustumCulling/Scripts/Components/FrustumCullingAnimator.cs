using UnityEngine;
using System;

namespace FS2.FrustumCulling
{
    [Serializable]
    public class FrustumCullingAnimator : FrustumCullingBehaviour<Animator>
    {
        public FrustumCullingAnimator(GameObject gameObject, bool isStatic = true) : base(gameObject, isStatic)
        {

        }
    }
}