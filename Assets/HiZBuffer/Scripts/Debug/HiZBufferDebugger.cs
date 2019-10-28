using UnityEngine;

namespace TEDCore.HiZ
{
    [RequireComponent(typeof(HiZBuffer))]
    public class HiZBufferDebugger : MonoBehaviour
    {
        [Range(0, 16)]
        public int lOD = 0;
        [Range(0, 1)]
        public int mode = 0;
        public float multiplier = 1f;

        private Shader m_shader;
        public Shader shader
        {
            get
            {
                if (m_shader == null)
                {
                    m_shader = Shader.Find("Hidden/HiZBufferDebugger");
                }

                return m_shader;
            }
        }

        private Material m_material;
        public Material material
        {
            get
            {
                if (m_material == null)
                {
                    if (shader == null || !shader.isSupported)
                    {
                        return null;
                    }

                    m_material = new Material(shader);
                }

                return m_material;
            }
        }

        private RenderTexture m_hiZBuffer
        {
            get
            {
                HiZBuffer hiZBuffer = GetComponent<HiZBuffer>();

                if (hiZBuffer == null)
                {
                    return null;
                }

                return hiZBuffer.hiZDepthTexture;
            }
        }

        public void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (m_hiZBuffer == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            material.SetInt("_LOD", lOD);
            material.SetInt("_Mode", mode);
            material.SetFloat("_Multiplier", multiplier);
            Graphics.Blit(m_hiZBuffer, destination, material);
        }
    }
}