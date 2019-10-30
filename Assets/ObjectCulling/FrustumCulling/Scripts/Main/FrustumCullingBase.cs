using UnityEngine;
using System;

namespace FS2.FrustumCulling
{
    [Serializable]
    public class FrustumCullingBase
    {
        public bool isValid = false;
        public bool isStatic = true;
        public Transform transform;
        public Vector3 center;
        public Vector3 extents;
        public uint visible;
        public bool isVisible;

        public void SetStatic(bool value)
        {
            if (!isValid)
            {
                return;
            }

            isStatic = value;
        }

        public virtual void SetVisible(uint value) { }
        public virtual void SetVisible(bool value) { }
    }
}