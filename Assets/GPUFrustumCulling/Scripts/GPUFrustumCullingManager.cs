using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using JSLCore;

namespace FS2.GPUFrustumCulling
{
    public class GPUFrustumCullingManager : MonoSingleton<GPUFrustumCullingManager>
    {
        private const string COMPUTE_SHADER_NAME = "GPUFrustumCulling";
        private const string KERNEL_NAME = "CSMain";
        private const string BOUNDS_BUFFER_NAME = "boundsBuffer";
        private const string RESULT_BUFFER_NAME = "resultBuffer";
        private const string FRUSTUM_PLANES_NAME = "frustumPlanes";
        private const int THREAD_COUNT = 64;
        private const int MAXIMUM_DYNAMIC_COUNT = 100;

        struct BufferData
        {
            public Vector3 center;
            public Vector3 extents;
        };

        public bool forceNotSupportsComputeShaders = false;
        public bool asyncGPUReadback = true;

        private bool m_initialized;
        private bool m_supportsComputeShaders;
        private ComputeShader m_computeShader;
        private int m_kernelID;
        private List<FrustumCullingBase> m_frustumCullingBases = new List<FrustumCullingBase>();
        private List<int> m_dynamicIndexes = new List<int>();
        private int m_lastDynamicIndex;
        private int m_tempDynamicCount;
        private Plane[] m_frustumPlanes;
        private Vector4[] m_frustumVector4;
        private BufferData[] m_bufferDatas;
        private uint[] m_tempResultBufferDatas;
        private NativeArray<uint> m_nativeResultBufferDatas;
        private uint[] m_lastResultBuffers;
        private ComputeBuffer m_boundsBuffer;
        private ComputeBuffer m_resultBuffer;
        private int m_threadGroups;
        private Queue<AsyncGPUReadbackRequest> m_asyncGPUReadbackRequests = new Queue<AsyncGPUReadbackRequest>();

        private void Awake()
        {
            m_initialized = false;
            m_supportsComputeShaders = SystemInfo.supportsComputeShaders;
            if (m_supportsComputeShaders)
            {
                OnComputeShaderLoaded(Resources.Load<ComputeShader>(COMPUTE_SHADER_NAME));
            }
        }

        private void OnComputeShaderLoaded(ComputeShader computeShader)
        {
            m_initialized = true;
            m_computeShader = computeShader;
            m_kernelID = m_computeShader.FindKernel(KERNEL_NAME);
        }

        private void OnDisable()
        {
            ReleaseBuffer();
        }

        public void Register(FrustumCullingBase frustumCullingBase)
        {
            m_frustumCullingBases.Add(frustumCullingBase);
            UpdateDynamicIndexes();
        }

        public void Register(FrustumCullingBase[] frustumCullingBases)
        {
            for (int i = 0; i < frustumCullingBases.Length; i++)
            {
                Register(frustumCullingBases[i]);
            }
        }

        public void Unregister(FrustumCullingBase frustumCullingBase)
        {
            m_frustumCullingBases.Remove(frustumCullingBase);
            UpdateDynamicIndexes();
        }

        public void Unregister(FrustumCullingBase[] frustumCullingBases)
        {
            for (int i = 0; i < frustumCullingBases.Length; i++)
            {
                Unregister(frustumCullingBases[i]);
            }
        }

        private void UpdateDynamicIndexes()
        {
            m_lastDynamicIndex = 0;
            m_dynamicIndexes.Clear();
            for (int i = 0; i < m_frustumCullingBases.Count; i++)
            {
                if (m_frustumCullingBases[i].isStatic)
                {
                    continue;
                }

                m_dynamicIndexes.Add(i);
            }
        }

        private AsyncGPUReadbackRequest m_request;
        private bool m_requestReady;
        private bool m_reassign;
        private void Update()
        {
            if (!m_initialized || m_frustumCullingBases.Count == 0)
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

            if (m_requestReady && m_nativeResultBufferDatas.Length == m_tempResultBufferDatas.Length)
            {
                m_nativeResultBufferDatas.CopyTo(m_tempResultBufferDatas);
                UpdateComputeBuffer(m_tempResultBufferDatas);
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

            if (m_bufferDatas == null || m_bufferDatas.Length != m_frustumCullingBases.Count)
            {
                m_bufferDatas = new BufferData[m_frustumCullingBases.Count];
                for (int i = 0; i < m_bufferDatas.Length; i++)
                {
                    BufferData bufferData = new BufferData();
                    bufferData.center = m_frustumCullingBases[i].center;
                    bufferData.extents = m_frustumCullingBases[i].extents;
                    m_bufferDatas[i] = bufferData;
                }

                if (m_supportsComputeShaders && !forceNotSupportsComputeShaders)
                {
                    ReleaseBuffer();

                    m_boundsBuffer = new ComputeBuffer(m_bufferDatas.Length, sizeof(float) * 6);
                    m_boundsBuffer.SetData(m_bufferDatas);
                    m_computeShader.SetBuffer(m_kernelID, BOUNDS_BUFFER_NAME, m_boundsBuffer);

                    m_lastResultBuffers = new uint[m_bufferDatas.Length];
                    for (int i = 0; i < m_lastResultBuffers.Length; i++)
                    {
                        if (m_tempResultBufferDatas != null && m_tempResultBufferDatas.Length > 0)
                        {
                            if (i < m_tempResultBufferDatas.Length)
                            {
                                m_lastResultBuffers[i] = m_tempResultBufferDatas[i];
                            }
                            else
                            {
                                m_lastResultBuffers[i] = 99;
                            }
                        }
                        else
                        {
                            m_lastResultBuffers[i] = 99;
                        }
                    }

                    m_tempResultBufferDatas = new uint[m_bufferDatas.Length];
                    m_resultBuffer = new ComputeBuffer(m_bufferDatas.Length, sizeof(uint));
                    m_computeShader.SetBuffer(m_kernelID, RESULT_BUFFER_NAME, m_resultBuffer);

                    m_threadGroups = Mathf.CeilToInt((float)m_bufferDatas.Length / THREAD_COUNT);
                }
            }

            m_reassign = m_dynamicIndexes.Count > 0;
            if (m_reassign)
            {
                if (m_lastDynamicIndex >= m_dynamicIndexes.Count)
                {
                    m_lastDynamicIndex = 0;
                }

                m_tempDynamicCount = 0;
                for (int i = 0; i < m_dynamicIndexes.Count; i++)
                {
                    if (i < m_lastDynamicIndex)
                    {
                        continue;
                    }

                    m_lastDynamicIndex++;
                    m_tempDynamicCount++;
                    m_bufferDatas[m_dynamicIndexes[i]].center = m_frustumCullingBases[m_dynamicIndexes[i]].transform.position;

                    if (m_tempDynamicCount >= MAXIMUM_DYNAMIC_COUNT)
                    {
                        break;
                    }
                }

                m_boundsBuffer.SetData(m_bufferDatas);
            }

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
            m_computeShader.SetVectorArray(FRUSTUM_PLANES_NAME, m_frustumVector4);
            m_computeShader.Dispatch(m_kernelID, m_threadGroups, 1, 1);

            if (asyncGPUReadback)
            {
                m_asyncGPUReadbackRequests.Enqueue(AsyncGPUReadback.Request(m_resultBuffer));
            }
            else
            {
                m_resultBuffer.GetData(m_tempResultBufferDatas);
                UpdateComputeBuffer(m_tempResultBufferDatas);
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

        private void UpdateComputeBuffer(uint[] resultBuffers)
        {
            if (!Application.isPlaying)
            {
                ReleaseBuffer();
                return;
            }

            if (m_lastResultBuffers.Length == resultBuffers.Length)
            {
                for (int i = 0; i < m_lastResultBuffers.Length; i++)
                {
                    if (m_lastResultBuffers[i] == resultBuffers[i])
                    {
                        continue;
                    }

                    m_lastResultBuffers[i] = resultBuffers[i];
                    m_frustumCullingBases[i].SetVisible(m_lastResultBuffers[i]);
                }
            }
        }

        private bool m_cacheReseult;
        private void UpdateGeneral()
        {
            for (int i = 0; i < m_bufferDatas.Length; i++)
            {
                //m_cacheReseult = GeometryUtility.TestPlanesAABB(m_frustumPlanes, m_gPUFrustumCullingComponents[i].cacheCollider.bounds);
                m_cacheReseult = FrustumCullingUtility.TestPlanesAABBInternalFast(m_frustumVector4, m_bufferDatas[i].center, m_bufferDatas[i].extents);
                m_frustumCullingBases[i].SetVisible(m_cacheReseult);
            }
        }
    }
}