using UnityEngine;

namespace FS2.FrustumCulling
{
    public abstract class FrustumCullingBehaviours<T> : FrustumCullingBase where T : Behaviour
    {
        public T[] components;

        public FrustumCullingBehaviours(GameObject gameObject, bool isStatic = true)
        {
            this.isStatic = isStatic;
            transform = gameObject.transform;
            components = gameObject.GetComponents<T>();
            isValid = components != null && components.Length != 0;

            if(!isValid)
            {
                return;
            }

            Collider cacheCollider = gameObject.AddComponent<BoxCollider>();
            center = cacheCollider.bounds.center;
            extents = cacheCollider.bounds.extents;

            if (Application.isPlaying)
            {
                GameObject.Destroy(cacheCollider);
            }
            else
            {
                GameObject.DestroyImmediate(cacheCollider);
            }

            cacheCollider = null;

            visible = (uint)(components[0].enabled ? 1 : 0);
        }

        public override void SetVisible(uint value)
        {
            if (!isValid)
            {
                return;
            }
            
            if (visible == value)
            {
                return;
            }
            
            for(int i = 0; i < components.Length; i++)
            {
                SetVisible(components[i], value == 1);
            }
        }

        private uint m_cacheVisible;
        public override void SetVisible(bool value)
        {
            if (!isValid)
            {
                return;
            }
            
            if (isVisible == value)
            {
                return;
            }
            
            for (int i = 0; i < components.Length; i++)
            {
                SetVisible(components[i], value);
            }
        }

        private void SetVisible(T component, bool value)
        {
            isVisible = value;
            visible = (uint)(value ? 1 : 0);
            component.enabled = value;
        }
    }
}