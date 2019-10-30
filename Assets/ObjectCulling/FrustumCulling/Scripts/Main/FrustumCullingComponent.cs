using UnityEngine;

namespace FS2.FrustumCulling
{
    public abstract class FrustumCullingComponent<T> : FrustumCullingBase where T : Component
    {
        public T component;

        public FrustumCullingComponent(GameObject gameObject, bool isStatic = true)
        {
            this.isStatic = isStatic;
            transform = gameObject.transform;
            component = gameObject.GetComponent<T>();
            isValid = component != null;

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
            
            SetVisible(component, value == 1);
        }
        
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
            
            SetVisible(component, value);
        }

        protected abstract void SetVisible(T component, bool value);
    }
}