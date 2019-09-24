using UnityEngine;

namespace FS2.GPUFrustumCulling
{
    public abstract class FrustumCullingBase
    {
        public bool isValid = false;
        public bool isStatic = true;
        public Transform transform;
        public Vector3 center;
        public Vector3 extents;
        public uint visible;

        public abstract void SetVisible(uint value);
        public abstract void SetVisible(bool value);
    }
}