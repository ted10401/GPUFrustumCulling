using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using JSLCore;
using JSLCore.Resource;
using TEDCore.HiZ;

namespace FS2.FrustumCulling
{
    public class FrustumCullingManager : MonoSingleton<FrustumCullingManager>
    {
        private const string COMPUTE_SHADER_NAME = "FrustumCullingComputeShader";
        private const string KERNEL_NAME = "CSMain";
        private const int THREAD_GROUP_X = 256;
        private const int THREAD_GROUP_Y = 1;
        private const int THREAD_GROUP_Z = 1;
        private const int MAXIMUM_DYNAMIC_COUNT = 100;
        private const int MAXIMUM_REQUEST_COUNT = 1;
        private const float EXTENTS_MULTIPLIER = 5f;

        struct BufferData
        {
            public Vector3 center;
            public Vector3 extents;
        };

        public bool drawFrustumPlanes;
        public bool isEnabled = true;
        public bool forceNotSupportsComputeShaders = false;
        public bool asyncGPUReadback = true;

        private bool m_initialized = false;
        private bool m_computeShaderLoaded = false;
        private bool m_supportsComputeShaders;
        private ComputeShader m_computeShader;
        private int m_kernelID;
        private UnityEngine.Camera m_camera;
        private HiZBuffer m_hiZBuffer;
        private Dictionary<int, FrustumCullingBase> m_cpuFrustumCullings = new Dictionary<int, FrustumCullingBase>();
        private Dictionary<int, FrustumCullingBase[]> m_gpuFrustumCullings = new Dictionary<int, FrustumCullingBase[]>();
        private int m_gpuFrustumCullingCount;
        private int m_cacheIndex;
        private Plane[] m_frustumPlanes = new Plane[6];
        private Vector4[] m_frustumVector4;
        private BufferData[] m_bufferDatas;
        private uint[] m_tempResultBufferDatas;
        private NativeArray<uint> m_nativeResultBufferDatas;
        private uint[] m_lastResultBuffers;
        private ComputeBuffer m_boundsBuffer;
        private ComputeBuffer m_resultBuffer;
        private int m_threadGroups;
        private Queue<AsyncGPUReadbackRequest> m_asyncGPUReadbackRequests = new Queue<AsyncGPUReadbackRequest>();
        private Queue<BufferData> m_bufferDataPool = new Queue<BufferData>();

        public void Initialize()
        {
            if(m_initialized)
            {
                return;
            }

            m_initialized = true;
            m_camera = UnityEngine.Camera.main;
            m_hiZBuffer = m_camera.GetComponent<HiZBuffer>();
            m_supportsComputeShaders = SystemInfo.supportsComputeShaders;
            if (m_supportsComputeShaders)
            {
                ResourceManager.Instance.LoadAsync<ComputeShader>(COMPUTE_SHADER_NAME, OnComputeShaderLoaded);
            }
        }

        private void OnComputeShaderLoaded(ComputeShader computeShader)
        {
            m_computeShaderLoaded = true;
            m_computeShader = computeShader;
            m_kernelID = m_computeShader.FindKernel(KERNEL_NAME);
            SetupGPUFrustumCullings();
        }

        private void OnDisable()
        {
            ReleaseBuffer();
        }

        public void RegisterCPU(int instanceID, FrustumCullingBase frustumCullingBase)
        {
            if (m_cpuFrustumCullings.ContainsKey(instanceID))
            {
                return;
            }

            m_cpuFrustumCullings.Add(instanceID, frustumCullingBase);
        }

        public void RegisterGPU(int instanceID, FrustumCullingBase[] frustumCullingBases)
        {
            if(m_gpuFrustumCullings.ContainsKey(instanceID))
            {
                return;
            }

            m_gpuFrustumCullings.Add(instanceID, frustumCullingBases);
            SetupGPUFrustumCullings();
        }

        public void UnregisterCPU(int instanceID)
        {
            m_cpuFrustumCullings.Remove(instanceID);
        }

        public void UnregisterGPU(int instanceID)
        {
            m_gpuFrustumCullings.Remove(instanceID);
            SetupGPUFrustumCullings();
        }

        private bool SupportsComputeShaders()
        {
            return m_supportsComputeShaders && !forceNotSupportsComputeShaders;
        }

        private BufferData m_bufferData;
        private void SetupGPUFrustumCullings()
        {
            if (!m_computeShaderLoaded)
            {
                return;
            }

            if (!SupportsComputeShaders())
            {
                return;
            }

            if(m_gpuFrustumCullings.Count == 0)
            {
                return;
            }

            m_gpuFrustumCullingCount = 0;
            foreach(FrustumCullingBase[] values in m_gpuFrustumCullings.Values)
            {
                m_gpuFrustumCullingCount += values.Length;
            }

            if (m_bufferDatas == null || m_bufferDatas.Length != m_gpuFrustumCullingCount)
            {
                if(m_bufferDatas != null)
                {
                    for(int i = 0; i < m_bufferDatas.Length; i++)
                    {
                        RecycleBufferData(m_bufferDatas[i]);
                    }
                }

                m_bufferDatas = new BufferData[m_gpuFrustumCullingCount];
                m_cacheIndex = -1;
                foreach(FrustumCullingBase[] values in m_gpuFrustumCullings.Values)
                {
                    for(int i = 0; i < values.Length; i++)
                    {
                        m_cacheIndex++;
                        m_bufferData = GetBufferData();
                        m_bufferData.center = values[i].center;
                        m_bufferData.extents = values[i].extents * EXTENTS_MULTIPLIER;
                        m_bufferDatas[m_cacheIndex] = m_bufferData;
                    }
                }

                ReleaseBuffer();

                m_boundsBuffer = new ComputeBuffer(m_gpuFrustumCullingCount, sizeof(float) * 6);
                m_boundsBuffer.SetData(m_bufferDatas);
                m_computeShader.SetBuffer(m_kernelID, FrustumCullingShaderIDs.boundsBuffer, m_boundsBuffer);

                m_lastResultBuffers = new uint[m_gpuFrustumCullingCount];
                for (int i = 0; i < m_gpuFrustumCullingCount; i++)
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

                m_tempResultBufferDatas = new uint[m_gpuFrustumCullingCount];
                m_resultBuffer = new ComputeBuffer(m_gpuFrustumCullingCount, sizeof(uint));
                m_computeShader.SetBuffer(m_kernelID, FrustumCullingShaderIDs.resultBuffer, m_resultBuffer);
                m_computeShader.SetInt(FrustumCullingShaderIDs._Count, m_gpuFrustumCullingCount);

                m_threadGroups = Mathf.CeilToInt((float)m_gpuFrustumCullingCount / THREAD_GROUP_X);
            }
        }

        private void RecycleBufferData(BufferData bufferData)
        {
            m_bufferDataPool.Enqueue(bufferData);
        }

        private BufferData GetBufferData()
        {
            if(m_bufferDataPool.Count > 0)
            {
                return m_bufferDataPool.Dequeue();
            }

            return new BufferData();
        }

        private AsyncGPUReadbackRequest m_request;
        private bool m_requestReady;
        private void Update()
        {
            if (!m_initialized)
            {
                Initialize();
                return;
            }

            if (!isEnabled)
            {
                return;
            }

            m_frustumPlanes = GeometryUtility.CalculateFrustumPlanes(m_camera);
            if (m_frustumVector4 == null || m_frustumVector4.Length != m_frustumPlanes.Length)
            {
                m_frustumVector4 = new Vector4[m_frustumPlanes.Length];
            }

            for (int i = 0; i < m_frustumVector4.Length; i++)
            {
                m_frustumVector4[i] = m_frustumPlanes[i].ToVector4();
            }

            UpdateCPUFrustumCullings();
            UpdateGPUFrustumCullings();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if(drawFrustumPlanes)
            {
                Vector3 position;
                Quaternion rotation;
                UnityEditor.Handles.color = Color.red;
                Gizmos.color = Color.red;
                for (int i = 0; i < m_frustumVector4.Length; i++)
                {
                    position = -m_frustumPlanes[i].normal * m_frustumPlanes[i].distance;
                    rotation = Quaternion.FromToRotation(Vector3.up, m_frustumPlanes[i].normal);
                    UnityEditor.Handles.ArrowHandleCap(0, position, rotation, 10f, EventType.Repaint);
                    Gizmos.DrawSphere(position, 1f);
            }
            }
        }
#endif

        private bool m_cacheReseult;
        private void UpdateCPUFrustumCullings()
        {
            if(m_cpuFrustumCullings.Count > 0)
            {
                foreach (FrustumCullingBase frustumCullingBase in m_cpuFrustumCullings.Values)
                {
                    if (!frustumCullingBase.isStatic)
                    {
                        frustumCullingBase.center = frustumCullingBase.transform.position;
                    }

                    m_cacheReseult = FrustumCullingUtility.TestPlanesAABBInternalFast(m_frustumVector4, frustumCullingBase.center, frustumCullingBase.extents * EXTENTS_MULTIPLIER);
                    if(m_cacheReseult == frustumCullingBase.isVisible)
                    {
                        continue;
                    }

                    frustumCullingBase.SetVisible(m_cacheReseult);
                }
            }

            if(!SupportsComputeShaders())
            {
                if (m_gpuFrustumCullings.Count > 0)
                {
                    foreach (FrustumCullingBase[] frustumCullingBases in m_gpuFrustumCullings.Values)
                    {
                        foreach (FrustumCullingBase frustumCullingBase in frustumCullingBases)
                        {
                            m_cacheReseult = FrustumCullingUtility.TestPlanesAABBInternalFast(m_frustumVector4, frustumCullingBase.center, frustumCullingBase.extents * EXTENTS_MULTIPLIER);
                            frustumCullingBase.SetVisible(m_cacheReseult);
                        }
                    }
                }
            }
        }

        private void UpdateGPUFrustumCullings()
        {
            if(!SupportsComputeShaders())
            {
                return;
            }

            if (m_gpuFrustumCullings.Count == 0)
            {
                return;
            }

            if(!m_computeShaderLoaded)
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

            UpdateComputeShader();
        }

        private void UpdateComputeShader()
        {
            if (asyncGPUReadback)
            {
                if(m_asyncGPUReadbackRequests.Count < MAXIMUM_REQUEST_COUNT)
                {
                    m_computeShader.SetTexture(m_kernelID, FrustumCullingShaderIDs._HiZMap, m_hiZBuffer.hiZDepthTexture);
                    m_computeShader.SetVector(FrustumCullingShaderIDs._HiZTextureSize, m_hiZBuffer.textureSize);
                    m_computeShader.SetMatrix(FrustumCullingShaderIDs._UNITY_MATRIX_MVP, m_camera.projectionMatrix * m_camera.worldToCameraMatrix);
                    m_computeShader.SetVectorArray(FrustumCullingShaderIDs.frustumPlanes, m_frustumVector4);
                    m_computeShader.Dispatch(m_kernelID, m_threadGroups, THREAD_GROUP_Y, THREAD_GROUP_Z);
                    m_asyncGPUReadbackRequests.Enqueue(AsyncGPUReadback.Request(m_resultBuffer));
                }
            }
            else
            {
                m_computeShader.SetTexture(m_kernelID, FrustumCullingShaderIDs._HiZMap, m_hiZBuffer.hiZDepthTexture);
                m_computeShader.SetVector(FrustumCullingShaderIDs._HiZTextureSize, m_hiZBuffer.textureSize);
                m_computeShader.SetMatrix(FrustumCullingShaderIDs._UNITY_MATRIX_MVP, m_camera.projectionMatrix * m_camera.worldToCameraMatrix);
                m_computeShader.SetVectorArray(FrustumCullingShaderIDs.frustumPlanes, m_frustumVector4);
                m_computeShader.Dispatch(m_kernelID, m_threadGroups, THREAD_GROUP_Y, THREAD_GROUP_Z);

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
                m_cacheIndex = -1;
                foreach(FrustumCullingBase[] values in m_gpuFrustumCullings.Values)
                {
                    for(int i = 0; i < values.Length; i++)
                    {
                        m_cacheIndex++;
                        if(m_lastResultBuffers[m_cacheIndex] == resultBuffers[m_cacheIndex])
                        {
                            continue;
                        }

                        m_lastResultBuffers[m_cacheIndex] = resultBuffers[m_cacheIndex];
                        values[i].SetVisible(m_lastResultBuffers[m_cacheIndex]);
                    }
                }
            }
        }
    }
}