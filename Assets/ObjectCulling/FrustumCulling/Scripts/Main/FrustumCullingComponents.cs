using System.Collections.Generic;
using UnityEngine;

namespace FS2.FrustumCulling
{
    public abstract class FrustumCullingComponents<T> : FrustumCullingBase where T : Component
    {
        public T[] components;

        public FrustumCullingComponents(GameObject gameObject, bool isStatic = true)
        {
            this.isStatic = isStatic;
            transform = gameObject.transform;

            List<T> componentList = new List<T>();
            T[] cacheComponents = gameObject.GetComponents<T>();
            componentList.AddRange(cacheComponents);

            cacheComponents = gameObject.GetComponentsInChildren<T>();
            for (int i = 0; i < cacheComponents.Length; i++)
            {
                if(!GetEnabled(cacheComponents[i]))
                {
                    continue;
                }

                if (componentList.Contains(cacheComponents[i]))
                {
                    continue;
                }

                componentList.Add(cacheComponents[i]);
            }

            components = componentList.ToArray();
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
        }

        protected void SetDefaultVisible(bool value)
        {
            visible = (uint)(value ? 1 : 0);
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

        protected abstract bool GetEnabled(T component);
        protected abstract void SetVisible(T component, bool value);
    }
}