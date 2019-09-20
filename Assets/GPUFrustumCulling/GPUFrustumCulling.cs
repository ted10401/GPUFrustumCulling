using UnityEngine;
using UnityEngine.Rendering;

public class GPUFrustumCulling : MonoBehaviour
{
    private const string KERNEL_NAME = "CSMain";
    private const string BOUNDS_BUFFER_NAME = "boundsBuffer";
    private const string RESULT_BUFFER_NAME = "resultBuffer";
    private const string FRUSTUM_PLANES_NAME = "frustumPlanes";
    private const int THREAD_COUNT = 64;

    struct BufferData
    {
        public Vector3 center;
        public Vector3 extents;
    };

    public bool forceNotSupportsComputeShaders = false;
    public bool asyncGPUReadback = false;
    public ComputeShader computeShader;

    private bool m_supportsComputeShaders;
    private int m_kernelID;
    private bool m_valid;
    private Collider[] m_colliders;
    private Renderer[] m_renderers;
    private int m_threadGroups;
    private ComputeBuffer m_boundsBuffer;
    private BufferData[] m_bufferData;
    private ComputeBuffer m_resultBuffer;
    private uint[] m_resultBufferDatas;

    private void Awake()
    {
        m_supportsComputeShaders = SystemInfo.supportsComputeShaders;
        if (forceNotSupportsComputeShaders)
        {
            m_supportsComputeShaders = false;
        }

        m_kernelID = computeShader.FindKernel(KERNEL_NAME);
    }

    public void CreateComputeBuffer(Collider[] colliders, Renderer[] renderers)
    {
        m_colliders = colliders;
        m_renderers = renderers;
        if (m_colliders == null || m_renderers == null ||
            m_colliders.Length == 0 || m_renderers.Length == 0 ||
            m_colliders.Length != m_renderers.Length)
        {
            m_valid = false;
            return;
        }

        m_valid = true;
        m_bufferData = new BufferData[m_colliders.Length];
        for (int i = 0; i < m_colliders.Length; i++)
        {
            BufferData bufferData = new BufferData();
            bufferData.center = m_colliders[i].bounds.center;
            bufferData.extents = m_colliders[i].bounds.extents;

            m_bufferData[i] = bufferData;
        }

        m_resultBufferDatas = new uint[m_colliders.Length];

        if (m_supportsComputeShaders)
        {
            ReleaseBuffer();

            m_boundsBuffer = new ComputeBuffer(m_colliders.Length, sizeof(float) * 3 * 2);
            m_boundsBuffer.SetData(m_bufferData);
            computeShader.SetBuffer(m_kernelID, BOUNDS_BUFFER_NAME, m_boundsBuffer);

            m_resultBuffer = new ComputeBuffer(m_colliders.Length, sizeof(uint));
            computeShader.SetBuffer(m_kernelID, RESULT_BUFFER_NAME, m_resultBuffer);

            m_threadGroups = Mathf.CeilToInt(m_colliders.Length / THREAD_COUNT);
        }
    }

    private Plane[] m_frustumPlanes;
    private void Update()
    {
        if (!m_valid)
        {
            return;
        }

        m_frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

        if (m_supportsComputeShaders)
        {
            UpdateComputeShader();
        }
        else
        {
            UpdateGeneral();
        }
    }

    private Vector4[] m_frustumVector4;
    private void UpdateComputeShader()
    {
        if(m_frustumVector4 == null || m_frustumVector4.Length != m_frustumPlanes.Length)
        {
            m_frustumVector4 = new Vector4[m_frustumPlanes.Length];
        }

        for (int i = 0; i < m_frustumVector4.Length; i++)
        {
            m_frustumVector4[i] = m_frustumPlanes[i].ToVector4();
        }

        for (int i = 0; i < m_colliders.Length; i++)
        {
            m_bufferData[i].center = m_colliders[i].bounds.center;
            m_bufferData[i].extents = m_colliders[i].bounds.extents;
        }
        m_boundsBuffer.SetData(m_bufferData);

        computeShader.SetVectorArray(FRUSTUM_PLANES_NAME, m_frustumVector4);
        computeShader.Dispatch(m_kernelID, m_threadGroups, 1, 1);
        m_resultBuffer.GetData(m_resultBufferDatas, 0, 0, m_resultBufferDatas.Length);

        if (asyncGPUReadback)
        {
            AsyncGPUReadback.Request(m_boundsBuffer, OnAsyncGPUReadbackRequest);
        }
        else
        {
            UpdateComputeBuffer();
        }
    }

    private void OnAsyncGPUReadbackRequest(AsyncGPUReadbackRequest asyncGPUReadbackRequest)
    {
        if (asyncGPUReadbackRequest.done && !asyncGPUReadbackRequest.hasError)
        {
            UpdateComputeBuffer();
        }
    }

    private void UpdateComputeBuffer()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        for (int i = 0; i < m_renderers.Length; i++)
        {
            m_renderers[i].enabled = m_resultBufferDatas[i] == 1;
        }
    }

    private void UpdateGeneral()
    {
        for (int i = 0; i < m_renderers.Length; i++)
        {
            m_renderers[i].enabled = GeometryUtility.TestPlanesAABB(m_frustumPlanes, m_colliders[i].bounds);
        }
    }

    private void OnDisable()
    {
        ReleaseBuffer();
    }

    private void ReleaseBuffer()
    {
        if (m_boundsBuffer != null)
        {
            m_boundsBuffer.Release();
            m_boundsBuffer = null;
        }

        if(m_resultBuffer != null)
        {
            m_resultBuffer.Release();
            m_resultBuffer = null;
        }
    }
}
