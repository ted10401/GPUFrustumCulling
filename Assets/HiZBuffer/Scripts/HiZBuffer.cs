using UnityEngine;
using UnityEngine.Rendering;

namespace TEDCore.HiZ
{
    [RequireComponent(typeof(Camera))]
    public class HiZBuffer : MonoBehaviour
    {
        private const int MAXIMUM_BUFFER_SIZE = 2048;

        private enum Pass
        {
            Blit,
            Reduce
        }

        private Shader m_shader;
        public Shader shader
        {
            get
            {
                if (m_shader == null)
                {
                    m_shader = Shader.Find("Hidden/Hi-Z Buffer");
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

        private Camera m_camera;
        public new Camera camera
        {
            get
            {
                if (m_camera == null)
                {
                    m_camera = GetComponent<Camera>();
                }

                return m_camera;
            }
        }

        public Vector2 textureSize { get { return m_textureSize; } }
        private Vector2 m_textureSize = Vector2.zero;

        public RenderTexture hiZDepthTexture { get; private set; }

        private int m_lODCount;
        public int lODCount
        {
            get
            {
                if (hiZDepthTexture == null)
                {
                    return 0;
                }

                return 1 + m_lODCount;
            }
        }

        private CommandBuffer m_commandBuffer;
        public CameraEvent m_cameraEvent = CameraEvent.BeforeReflections;

        private int[] m_temporaries;

        private void OnEnable()
        {
            camera.depthTextureMode = DepthTextureMode.Depth;
        }

        private void OnDisable()
        {
            if (camera != null)
            {
                if (m_commandBuffer != null)
                {
                    camera.RemoveCommandBuffer(m_cameraEvent, m_commandBuffer);
                    m_commandBuffer = null;
                }
            }

            ReleaseBuffer();
        }

        private void ReleaseBuffer()
        {
            if (hiZDepthTexture != null)
            {
                hiZDepthTexture.Release();
                hiZDepthTexture = null;
            }
        }

        private void OnPreRender()
        {
            int size = Mathf.Max(camera.pixelWidth, camera.pixelHeight);
            size = Mathf.Min(Mathf.NextPowerOfTwo(size), MAXIMUM_BUFFER_SIZE);

            m_lODCount = (int)Mathf.Floor(Mathf.Log(size, 2f));
            if (m_lODCount == 0)
            {
                return;
            }

            bool isCommandBufferInvalid = false;
            if (hiZDepthTexture == null || hiZDepthTexture.width != size || hiZDepthTexture.height != size)
            {
                ReleaseBuffer();

                m_textureSize.x = size;
                m_textureSize.y = size;

                hiZDepthTexture = new RenderTexture(size, size, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
                hiZDepthTexture.filterMode = FilterMode.Point;
                hiZDepthTexture.useMipMap = true;
                hiZDepthTexture.autoGenerateMips = false;
                hiZDepthTexture.hideFlags = HideFlags.HideAndDontSave;
                hiZDepthTexture.Create();

                isCommandBufferInvalid = true;
            }

            if (m_commandBuffer == null || isCommandBufferInvalid == true)
            {
                m_temporaries = new int[m_lODCount];

                if (m_commandBuffer != null)
                {
                    camera.RemoveCommandBuffer(m_cameraEvent, m_commandBuffer);
                }

                m_commandBuffer = new CommandBuffer();
                m_commandBuffer.name = "Hi-Z Buffer";

                RenderTargetIdentifier id = new RenderTargetIdentifier(hiZDepthTexture);

                m_commandBuffer.Blit(null, id, material, (int)Pass.Blit);

                for (int i = 0; i < m_lODCount; ++i)
                {
                    m_temporaries[i] = Shader.PropertyToID("_09659d57_Temporaries" + i.ToString());
                    size >>= 1;
                    size = Mathf.Max(size, 1);

                    m_commandBuffer.GetTemporaryRT(m_temporaries[i], size, size, 0, FilterMode.Point, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);

                    if (i == 0)
                    {
                        m_commandBuffer.Blit(id, m_temporaries[0], material, (int)Pass.Reduce);
                    }
                    else
                    {
                        m_commandBuffer.Blit(m_temporaries[i - 1], m_temporaries[i], material, (int)Pass.Reduce);
                    }

                    m_commandBuffer.CopyTexture(m_temporaries[i], 0, 0, id, 0, i + 1);

                    if (i >= 1)
                    {
                        m_commandBuffer.ReleaseTemporaryRT(m_temporaries[i - 1]);
                    }
                }

                m_commandBuffer.ReleaseTemporaryRT(m_temporaries[m_lODCount - 1]);

                camera.AddCommandBuffer(m_cameraEvent, m_commandBuffer);
            }
        }
    }
}