﻿using UnityEngine;

namespace FS2.GPUFrustumCulling
{
    public abstract class FrustumCullingComponent<T> : FrustumCullingBase where T : Component
    {
        public T component;

        public FrustumCullingComponent(GameObject gameObject)
        {
            isStatic = true;
            transform = gameObject.transform;
            component = gameObject.GetComponent<T>();
            isValid = component != null;

            if(!isValid)
            {
                return;
            }

            Collider cacheCollider;
            bool tempCollider = false;
            cacheCollider = gameObject.GetComponent<Collider>();
            tempCollider = cacheCollider == null;
            if (tempCollider)
            {
                cacheCollider = gameObject.AddComponent<BoxCollider>();
            }

            center = cacheCollider.bounds.center;
            extents = cacheCollider.bounds.extents;

            if(tempCollider)
            {
                if(Application.isPlaying)
                {
                    GameObject.Destroy(cacheCollider);
                }
                else
                {
                    GameObject.DestroyImmediate(cacheCollider);
                }

                cacheCollider = null;
            }
        }

        protected void SetDefaultVisible(bool value)
        {
            visible = (uint)(value ? 1 : 0);
        }

        public void SetStatic(bool value)
        {
            if(!isValid)
            {
                return;
            }

            isStatic = value;
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

            visible = value;
            SetVisible(component, visible == 1);
        }

        private uint m_cacheVisible;
        public override void SetVisible(bool value)
        {
            if (!isValid)
            {
                return;
            }

            m_cacheVisible = (uint)(value ? 1 : 0);
            if (visible == m_cacheVisible)
            {
                return;
            }

            visible = m_cacheVisible;
            SetVisible(component, value);
        }

        protected abstract void SetVisible(T component, bool value);
    }
}