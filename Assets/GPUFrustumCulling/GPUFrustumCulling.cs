using UnityEngine;
using UnityEngine.Rendering;

public class GPUFrustumCulling : MonoBehaviour
{
    private const int THREAD_COUNT = 64;

    struct BufferData
    {
        public Vector3 center;
        public Vector3 extents;
        public uint visible;
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

        if (m_supportsComputeShaders)
        {
            m_threadGroups = Mathf.CeilToInt(m_colliders.Length / THREAD_COUNT);

            m_computeBuffer = new ComputeBuffer(m_colliders.Length, sizeof(float) * 3 * 2 + sizeof(uint));
            m_computeBuffer.SetData(m_bufferDatas);
            computeShader.SetBuffer(m_kernelID, "buffer", m_computeBuffer);
        }
    }

    private void InitializeBufferData(Collider tempCollider, ref BufferData bufferData)
    {
        bufferData.center = tempCollider.bounds.center;
        bufferData.extents = tempCollider.bounds.extents;
        bufferData.visible = 1;
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
        if (m_computeBuffer == null)
        {
            return;
        }

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        Vector4[] frustumPlanes = new Vector4[6];
        for (int i = 0; i < 6; ++i)
        {
            frustumPlanes[i] = new Vector4(-planes[i].normal.x, -planes[i].normal.y, -planes[i].normal.z, -planes[i].distance);
        }
        computeShader.SetVectorArray("frustumPlanes", frustumPlanes);

        Vector3[] frustumCorners = new Vector3[4];
        Camera.main.CalculateFrustumCorners(new Rect(0, 0, 1, 1), Camera.main.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
        Vector3 minFrustumPlanes = frustumCorners[0];
        Vector3 maxFrustumPlanes = frustumCorners[0];
        for (int i = 1; i < 4; ++i)
        {
            minFrustumPlanes = Vector3.Min(minFrustumPlanes, frustumCorners[i]);
            maxFrustumPlanes = Vector3.Max(maxFrustumPlanes, frustumCorners[i]);
        }

        Camera.main.CalculateFrustumCorners(new Rect(0, 0, 1, 1), Camera.main.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
        for (int i = 1; i < 4; ++i)
        {
            minFrustumPlanes = Vector3.Min(minFrustumPlanes, frustumCorners[i]);
            maxFrustumPlanes = Vector3.Max(maxFrustumPlanes, frustumCorners[i]);
        }

        computeShader.SetVector("_FrustumMinPoint", new Vector4(minFrustumPlanes.x, minFrustumPlanes.y, minFrustumPlanes.z, 1));
        computeShader.SetVector("_FrustumMaxPoint", new Vector4(maxFrustumPlanes.x, maxFrustumPlanes.y, maxFrustumPlanes.z, 1));

        computeShader.Dispatch(m_kernelID, m_threadGroups, 1, 1);
        m_computeBuffer.GetData(m_bufferDatas, 0, 0, m_bufferDatas.Length);

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
            m_renderers[i].enabled = m_bufferDatas[i].visible == 1;
        }
    }

    private float m_deltaTime;
    private void UpdateGeneral()
    {
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

        for (int i = 0; i < m_renderers.Length; i++)
        {
            m_renderers[i].enabled = GeometryUtility.TestPlanesAABB(frustumPlanes, m_colliders[i].bounds);
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
        }
    }
}
