using UnityEngine;

namespace FS2.FrustumCulling
{
    public abstract class FrustumCullingBehaviour<T> : FrustumCullingBase where T : Behaviour
    {
        public T component;

        public FrustumCullingBehaviour(GameObject gameObject, bool isStatic = true)
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

            SetVisible(!component.enabled);
            SetVisible(component.enabled);
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

        private void SetVisible(T component, bool value)
        {
            isVisible = value;
            visible = (uint)(value ? 1 : 0);
            component.enabled = value;
        }
    }
}