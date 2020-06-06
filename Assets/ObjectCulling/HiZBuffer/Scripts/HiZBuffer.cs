// https://github.com/gokselgoktas/hi-z-buffer

using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class HiZBuffer : MonoBehaviour
{
    // Consts
    private const int MAXIMUM_BUFFER_SIZE = 1024;
    private const int PASS_BLIT = 0;
    private const int PASS_REDUCE = 1;
    private const string HIZ_COMMAND_BUFFER_NAME = "HiZ Buffer";
    private const string HIZ_TEMPORARIES_FORMAT = "_09659d57_Temporaries_{0}";
    private readonly int HIZ_LOD_ID = Shader.PropertyToID("_LOD");
    private readonly int HIZ_STRENGTH_ID = Shader.PropertyToID("_Strength");

    [Header("Debug")]
    [SerializeField] private bool m_debugHiZBuffer = false;
    [SerializeField] [Range(0, 10)] private int m_debugHiZLOD = 0;
    [SerializeField] [Range(1, 100)] private float m_debugHiZStrength = 1;

    [Header("References")]
    [SerializeField] private Camera m_mainCamera = null;
    [SerializeField] private Shader m_hiZBufferShader = null;
    [SerializeField] private Shader m_debugHiZBufferShader = null;

    private Material m_hiZBufferMaterial = null;
    private Material m_debugHiZBufferMaterial = null;
    private int m_hiZBufferTextureSize;
    private int m_LODCount = 0;
    private int[] m_temporaries = null;
    private CameraEvent m_cameraEvent = CameraEvent.AfterReflections;
    private CameraEvent m_lastCameraEvent = CameraEvent.AfterReflections;
    private CommandBuffer m_commandBuffer = null;
    
    public Vector2 hiZBufferTextureSize { get; private set; }
    public RenderTexture hiZBufferTexture { get; private set; }
    
    private void Awake()
    {
        m_hiZBufferMaterial = new Material(m_hiZBufferShader);
        m_debugHiZBufferMaterial = new Material(m_debugHiZBufferShader);
        m_mainCamera.depthTextureMode = DepthTextureMode.Depth;
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.S))
        {
            m_debugHiZBuffer = !m_debugHiZBuffer;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            m_debugHiZLOD = Mathf.Clamp(m_debugHiZLOD - 1, 0, 10);
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            m_debugHiZLOD = Mathf.Clamp(m_debugHiZLOD + 1, 0, 10);
        }
    }

    private void OnDisable()
    {
        ReleaseCommandBuffer();
        ReleaseTexture();
    }

    private void ReleaseCommandBuffer()
    {
        if (m_mainCamera != null)
        {
            if (m_commandBuffer != null)
            {
                m_mainCamera.RemoveCommandBuffer(m_cameraEvent, m_commandBuffer);
                m_commandBuffer = null;
            }
        }
    }

    private void ReleaseTexture()
    {
        if (hiZBufferTexture != null)
        {
            hiZBufferTexture.Release();
            hiZBufferTexture = null;
        }
    }

    private void UpdateHiZBufferTextureSize()
    {
        m_hiZBufferTextureSize = Mathf.Max(m_mainCamera.pixelWidth, m_mainCamera.pixelHeight);
        m_hiZBufferTextureSize = Mathf.Min(Mathf.NextPowerOfTwo(m_hiZBufferTextureSize), MAXIMUM_BUFFER_SIZE);
    }
    
    public void InitializeTexture()
    {
        ReleaseTexture();
        UpdateHiZBufferTextureSize();

        hiZBufferTextureSize = new Vector2(m_hiZBufferTextureSize, m_hiZBufferTextureSize);
        hiZBufferTexture = new RenderTexture(m_hiZBufferTextureSize, m_hiZBufferTextureSize, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
        hiZBufferTexture.filterMode = FilterMode.Point;
        hiZBufferTexture.useMipMap = true;
        hiZBufferTexture.autoGenerateMips = false;
        hiZBufferTexture.Create();
        hiZBufferTexture.hideFlags = HideFlags.HideAndDontSave;
    }

    private void UpdateCommandBuffer()
    {
        UpdateHiZBufferTextureSize();
        m_LODCount = (int)Mathf.Floor(Mathf.Log(m_hiZBufferTextureSize, 2f));

        if (m_LODCount == 0)
        {
            return;
        }

        bool isCommandBufferInvalid = false;
        if (hiZBufferTexture == null ||
            hiZBufferTexture.width != m_hiZBufferTextureSize ||
            hiZBufferTexture.height != m_hiZBufferTextureSize ||
            m_lastCameraEvent != m_cameraEvent)
        {
            InitializeTexture();

            m_lastCameraEvent = m_cameraEvent;
            isCommandBufferInvalid = true;
        }

        if (m_commandBuffer == null || isCommandBufferInvalid)
        {
            ReleaseCommandBuffer();

            m_temporaries = new int[m_LODCount];
            m_commandBuffer = new CommandBuffer();
            m_commandBuffer.name = HIZ_COMMAND_BUFFER_NAME;

            RenderTargetIdentifier id = new RenderTargetIdentifier(hiZBufferTexture);
            m_commandBuffer.Blit(null, id, m_hiZBufferMaterial, PASS_BLIT);

            for (int i = 0; i < m_LODCount; ++i)
            {
                m_temporaries[i] = Shader.PropertyToID(string.Format(HIZ_TEMPORARIES_FORMAT, i));
                m_hiZBufferTextureSize >>= 1;
                m_hiZBufferTextureSize = Mathf.Max(m_hiZBufferTextureSize, 1);

                m_commandBuffer.GetTemporaryRT(m_temporaries[i], m_hiZBufferTextureSize, m_hiZBufferTextureSize, 0, FilterMode.Point, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);

                if (i == 0)
                {
                    m_commandBuffer.Blit(id, m_temporaries[0], m_hiZBufferMaterial, PASS_REDUCE);
                }
                else
                {
                    m_commandBuffer.Blit(m_temporaries[i - 1], m_temporaries[i], m_hiZBufferMaterial, PASS_REDUCE);
                }

                m_commandBuffer.CopyTexture(m_temporaries[i], 0, 0, id, 0, i + 1);

                if (i >= 1)
                {
                    m_commandBuffer.ReleaseTemporaryRT(m_temporaries[i - 1]);
                }
            }

            m_commandBuffer.ReleaseTemporaryRT(m_temporaries[m_LODCount - 1]);
            m_mainCamera.AddCommandBuffer(m_cameraEvent, m_commandBuffer);
        }
    }

    private void OnPreRender()
    {
        UpdateCommandBuffer();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (m_debugHiZBuffer)
        {
            m_debugHiZBufferMaterial.SetInt(HIZ_LOD_ID, m_debugHiZLOD);
            m_debugHiZBufferMaterial.SetFloat(HIZ_STRENGTH_ID, m_debugHiZStrength);
            Graphics.Blit(hiZBufferTexture, destination, m_debugHiZBufferMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}