using System.Collections.Generic;
using UnityEngine;

namespace FS2.FrustumCulling
{
    public class FrustumCullingTerrainMonoBehaviour : MonoBehaviour
    {
        private int m_instanceID;
        [SerializeField] private List<FrustumCullingRenderer> m_frustumCullingRenderers = new List<FrustumCullingRenderer>();
        [SerializeField] private List<FrustumCullingLight> m_frustumCullingLights = new List<FrustumCullingLight>();
        [SerializeField] private FrustumCullingBase[] m_frustumCullingBases = null;

        public void Reset()
        {
            //FrustumCullingRenderer
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            FrustumCullingRenderer cacheFrustumCullingRenderer;
            bool newComponent = true;
            for (int i = 0; i < renderers.Length; i++)
            {
                if(renderers[i] == null || !renderers[i].enabled)
                {
                    continue;
                }

                newComponent = true;
                for (int j = 0; j < m_frustumCullingRenderers.Count; j++)
                {
                    if(m_frustumCullingRenderers[j].component == renderers[i])
                    {
                        newComponent = false;
                        break;
                    }
                }

                if(!newComponent)
                {
                    continue;
                }

                cacheFrustumCullingRenderer = new FrustumCullingRenderer(renderers[i].gameObject);
                m_frustumCullingRenderers.Add(cacheFrustumCullingRenderer);
            }

            //FrustumCullingLight
            Light[] lights = GetComponentsInChildren<Light>();
            FrustumCullingLight cacheFrustumCullingLight;
            for (int i = 0; i < lights.Length; i++)
            {
                if(lights[i] == null || !lights[i].enabled)
                {
                    continue;
                }

                newComponent = true;
                for (int j = 0; j < m_frustumCullingLights.Count; j++)
                {
                    if (m_frustumCullingLights[j].component == lights[i])
                    {
                        newComponent = false;
                        break;
                    }
                }

                if (!newComponent)
                {
                    continue;
                }

                cacheFrustumCullingLight = new FrustumCullingLight(lights[i].gameObject);
                m_frustumCullingLights.Add(cacheFrustumCullingLight);
            }

            //Total
            m_frustumCullingBases = new FrustumCullingBase[m_frustumCullingRenderers.Count + m_frustumCullingLights.Count];
        }

        public void Revert()
        {
            for(int i = 0; i < m_frustumCullingRenderers.Count; i++)
            {
                m_frustumCullingRenderers[i].SetVisible(false);
                m_frustumCullingRenderers[i].SetVisible(true);
            }

            for (int i = 0; i < m_frustumCullingLights.Count; i++)
            {
                m_frustumCullingLights[i].SetVisible(false);
                m_frustumCullingLights[i].SetVisible(true);
            }
        }

        private void Awake()
        {
            m_instanceID = GetInstanceID();
            for (int i = 0; i < m_frustumCullingBases.Length; i++)
            {
                if (i < m_frustumCullingRenderers.Count)
                {
                    m_frustumCullingBases[i] = m_frustumCullingRenderers[i];
                }
                else
                {
                    m_frustumCullingBases[i] = m_frustumCullingLights[i - m_frustumCullingRenderers.Count];
                }
            }
        }

        private void OnEnable()
        {
            if (m_frustumCullingBases != null && m_frustumCullingBases.Length > 0)
            {
                FrustumCullingManager.Instance?.RegisterGPU(m_instanceID, m_frustumCullingBases);
            }
        }

        private void OnDisable()
        {
            if (m_frustumCullingBases != null && m_frustumCullingBases.Length > 0)
            {
                FrustumCullingManager.Instance?.UnregisterGPU(m_instanceID);
            }
        }
    }
}