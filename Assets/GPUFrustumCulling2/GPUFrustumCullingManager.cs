using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class GPUFrustumCullingManager : MonoBehaviour
{
    public static GPUFrustumCullingManager Instance { get; private set; }

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
    public int size;

    private int m_kernelID;
    private bool m_supportsComputeShaders;
    private List<GPUBounds> m_gPUBounds = new List<GPUBounds>();
    private Plane[] m_frustumPlanes;
    private Vector4[] m_frustumVector4;
    private BufferData[] m_bufferDatas;
    private uint[] m_resultBufferDatas;
    private NativeArray<uint> m_nativeResultBufferDatas;
    private ComputeBuffer m_boundsBuffer;
    private ComputeBuffer m_resultBuffer;
    private int m_threadGroups;
    private Queue<AsyncGPUReadbackRequest> m_asyncGPUReadbackRequests = new Queue<AsyncGPUReadbackRequest>();

    private void Awake()
    {
        Instance = this;
        m_kernelID = computeShader.FindKernel(KERNEL_NAME);
        m_supportsComputeShaders = SystemInfo.supportsComputeShaders;
    }

    private void OnDisable()
    {
        ReleaseBuffer();
    }

    public void Register(GPUBounds gPUBounds)
    {
        m_gPUBounds.Add(gPUBounds);
    }

    public void Register(GPUBounds[] gPUBounds)
    {
        m_gPUBounds.AddRange(gPUBounds);
    }

    public void Unregister(GPUBounds gPUBounds)
    {
        m_gPUBounds.Remove(gPUBounds);
    }

    public void Unregister(GPUBounds[] gPUBounds)
    {
        for (int i = 0; i < gPUBounds.Length; i++)
        {
            m_gPUBounds.Remove(gPUBounds[i]);
        }
    }

    private AsyncGPUReadbackRequest m_request;
    private bool m_requestReady;
    private bool m_reassign;
    private void Update()
    {
        if (m_gPUBounds.Count == 0)
        {
            return;
        }

        m_requestReady = false;
        while (m_asyncGPUReadbackRequests.Count > 0)
        {
            m_request = m_asyncGPUReadbackRequests.Peek();
            if (m_request.hasError)
            {
                m_asyncGPUReadbackRequests.Dequeue();
            }
            else if (m_request.done)
            {
                m_requestReady = true;
                m_nativeResultBufferDatas = m_request.GetData<uint>();
                m_asyncGPUReadbackRequests.Dequeue();
            }
            else
            {
                break;
            }
        }

        if (m_requestReady && m_nativeResultBufferDatas.Length == m_resultBufferDatas.Length)
        {
            m_nativeResultBufferDatas.CopyTo(m_resultBufferDatas);
            UpdateComputeBuffer(m_resultBufferDatas);
        }

        m_frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        if (m_frustumVector4 == null || m_frustumVector4.Length != m_frustumPlanes.Length)
        {
            m_frustumVector4 = new Vector4[m_frustumPlanes.Length];
        }

        for (int i = 0; i < m_frustumVector4.Length; i++)
        {
            m_frustumVector4[i] = m_frustumPlanes[i].ToVector4();
        }

        if (m_bufferDatas == null || m_bufferDatas.Length != m_gPUBounds.Count)
        {
            size = m_gPUBounds.Count;
            m_bufferDatas = new BufferData[m_gPUBounds.Count];
            for (int i = 0; i < m_bufferDatas.Length; i++)
            {
                BufferData bufferData = new BufferData();
                bufferData.center = m_gPUBounds[i].center;
                bufferData.extents = m_gPUBounds[i].extents;

                m_bufferDatas[i] = bufferData;
            }

            m_resultBufferDatas = new uint[m_bufferDatas.Length];

            if (m_supportsComputeShaders && !forceNotSupportsComputeShaders)
            {
                ReleaseBuffer();

                m_boundsBuffer = new ComputeBuffer(m_bufferDatas.Length, sizeof(float) * 6);
                m_boundsBuffer.SetData(m_bufferDatas);
                computeShader.SetBuffer(m_kernelID, BOUNDS_BUFFER_NAME, m_boundsBuffer);

                m_resultBuffer = new ComputeBuffer(m_bufferDatas.Length, sizeof(uint));
                computeShader.SetBuffer(m_kernelID, RESULT_BUFFER_NAME, m_resultBuffer);

                m_threadGroups = Mathf.CeilToInt((float)m_bufferDatas.Length / THREAD_COUNT);
            }
        }

        m_reassign = false;
        for (int i = 0; i < m_bufferDatas.Length; i++)
        {
            if (!m_gPUBounds[i].isStatic)
            {
                m_reassign = true;
                m_bufferDatas[i].center = m_gPUBounds[i].collider.bounds.center;
            }
        }
        m_boundsBuffer.SetData(m_bufferDatas);

        if (m_supportsComputeShaders && !forceNotSupportsComputeShaders)
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
        computeShader.SetVectorArray(FRUSTUM_PLANES_NAME, m_frustumVector4);
        computeShader.Dispatch(m_kernelID, m_threadGroups, 1, 1);

        if (asyncGPUReadback)
        {
            m_asyncGPUReadbackRequests.Enqueue(AsyncGPUReadback.Request(m_resultBuffer));
        }
        else
        {
            m_resultBuffer.GetData(m_resultBufferDatas);
            UpdateComputeBuffer(m_resultBufferDatas);
        }
    }

    private void ReleaseBuffer()
    {
        if (m_boundsBuffer != null)
        {
            m_boundsBuffer.Release();
            m_boundsBuffer = null;
        }

        if (m_resultBuffer != null)
        {
            m_resultBuffer.Release();
            m_resultBuffer = null;
        }
    }

    private void UpdateComputeBuffer(NativeArray<uint> resultBuffers)
    {
        if (!Application.isPlaying)
        {
            ReleaseBuffer();
            return;
        }

        for (int i = 0; i < resultBuffers.Length; i++)
        {
            m_gPUBounds[i].SetVisible(resultBuffers[i]);
        }
    }

    private void UpdateComputeBuffer(uint[] resultBuffers)
    {
        if (!Application.isPlaying)
        {
            ReleaseBuffer();
            return;
        }

        for (int i = 0; i < resultBuffers.Length; i++)
        {
            m_gPUBounds[i].SetVisible(resultBuffers[i]);
        }
    }

    private bool m_cacheReseult;
    private void UpdateGeneral()
    {
        for (int i = 0; i < m_bufferDatas.Length; i++)
        {
            //m_cacheReseult = GeometryUtility.TestPlanesAABB(m_frustumPlanes, m_gPUFrustumCullingComponents[i].cacheCollider.bounds);
            m_cacheReseult = FrustumCullingUtility.TestPlanesAABBInternalFast(m_frustumVector4, m_bufferDatas[i].center, m_bufferDatas[i].extents);
            m_gPUBounds[i].SetVisible(m_cacheReseult);
        }
    }
}
