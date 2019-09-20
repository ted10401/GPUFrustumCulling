using UnityEngine;
using UnityEngine.Rendering;

public class GPUFrustumCulling : MonoBehaviour
{
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
    private ComputeBuffer m_computeBuffer;
    private BufferData[] m_bufferDatas;
    private ComputeBuffer m_resultComputeBuffer;
    private uint[] m_resultBufferDatas;

    private void Awake()
    {
        m_supportsComputeShaders = SystemInfo.supportsComputeShaders;
        if (forceNotSupportsComputeShaders)
        {
            m_supportsComputeShaders = false;
        }

        m_kernelID = computeShader.FindKernel("CSMain");
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
        m_bufferDatas = new BufferData[m_colliders.Length];
        for (int i = 0; i < m_colliders.Length; i++)
        {
            BufferData bufferData = new BufferData();
            InitializeBufferData(m_colliders[i], ref bufferData);
            m_bufferDatas[i] = bufferData;
        }

        m_resultBufferDatas = new uint[m_colliders.Length];

        if (m_supportsComputeShaders)
        {
            m_threadGroups = Mathf.CeilToInt(m_colliders.Length / THREAD_COUNT);

            ReleaseBuffer();

            m_computeBuffer = new ComputeBuffer(m_colliders.Length, sizeof(float) * 3 * 2);
            m_computeBuffer.SetData(m_bufferDatas);
            computeShader.SetBuffer(m_kernelID, "buffer", m_computeBuffer);

            m_resultComputeBuffer = new ComputeBuffer(m_colliders.Length, sizeof(uint));
            computeShader.SetBuffer(m_kernelID, "resultBuffer", m_resultComputeBuffer);
        }
    }

    private void InitializeBufferData(Collider tempCollider, ref BufferData bufferData)
    {
        bufferData.center = tempCollider.bounds.center;
        bufferData.extents = tempCollider.bounds.extents;
    }

    private void Update()
    {
        if (!m_valid)
        {
            return;
        }

        if (m_supportsComputeShaders)
        {
            UpdateComputeShader();
        }
        else
        {
            UpdateGeneral();
        }
    }

    private void UpdateComputeShader()
    {
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        Vector4[] frustumPlanes4 = new Vector4[frustumPlanes.Length];
        for (int i = 0; i < frustumPlanes4.Length; i++)
        {
            frustumPlanes4[i] = frustumPlanes[i].normal;
            frustumPlanes4[i].w = frustumPlanes[i].distance;
        }
        computeShader.SetVectorArray("frustumPlanes", frustumPlanes4);

        computeShader.Dispatch(m_kernelID, m_threadGroups, 1, 1);
        m_resultComputeBuffer.GetData(m_resultBufferDatas, 0, 0, m_resultBufferDatas.Length);

        if (asyncGPUReadback)
        {
            AsyncGPUReadback.Request(m_computeBuffer, OnAsyncGPUReadbackRequest);
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
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        Vector4[] frustumPlanes4 = new Vector4[frustumPlanes.Length];
        for(int i = 0; i < frustumPlanes4.Length; i++)
        {
            frustumPlanes4[i] = frustumPlanes[i].normal;
            frustumPlanes4[i].w = frustumPlanes[i].distance;
        }

        for (int i = 0; i < m_renderers.Length; i++)
        {
            m_renderers[i].enabled = GeometryUtility.TestPlanesAABB(frustumPlanes, m_colliders[i].bounds);
            //m_renderers[i].enabled = FrustumCullingUtility.TestPlanesAABBInternalFast(frustumPlanes4, m_colliders[i].bounds);
        }
    }

    private void OnDisable()
    {
        ReleaseBuffer();
    }

    private void ReleaseBuffer()
    {
        if (m_computeBuffer != null)
        {
            m_computeBuffer.Release();
            m_computeBuffer = null;
        }

        if(m_resultComputeBuffer != null)
        {
            m_resultComputeBuffer.Release();
            m_resultComputeBuffer = null;
        }
    }
}
