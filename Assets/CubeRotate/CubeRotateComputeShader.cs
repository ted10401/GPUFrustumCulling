using UnityEngine;
using UnityEngine.Rendering;

public class CubeRotateComputeShader : MonoBehaviour
{
    private const int THREAD_COUNT = 64;

    struct BufferData
    {
        public float rotateSpeed; //4 bytes
        public Vector3 eulerAngle; //12 bytes
    }

    public bool forceNotSupportsComputeShaders = false;
    public bool asyncGPUReadback = false;
    public ComputeShader computeShader;

    private bool m_supportsComputeShaders;
    private int m_kernelID;
    private Transform[] m_cubes;
    private int m_threadGroups;
    private ComputeBuffer m_computeBuffer;
    private BufferData[] m_bufferDatas;

    private void Awake()
    {
        m_supportsComputeShaders = SystemInfo.supportsComputeShaders;
        if(forceNotSupportsComputeShaders)
        {
            m_supportsComputeShaders = false;
        }

        m_kernelID = computeShader.FindKernel("CSMain");
    }

    public void CreateComputeBuffer(Transform[] cubes)
    {
        m_cubes = cubes;
        if(m_cubes == null || m_cubes.Length == 0)
        {
            return;
        }

        m_bufferDatas = new BufferData[m_cubes.Length];
        for (int i = 0; i < m_cubes.Length; i++)
        {
            BufferData bufferData = new BufferData();
            InitializeBufferData(m_cubes[i], ref bufferData);
            m_bufferDatas[i] = bufferData;
        }

        if (m_supportsComputeShaders)
        {
            m_threadGroups = Mathf.CeilToInt(m_cubes.Length / THREAD_COUNT);

            m_computeBuffer = new ComputeBuffer(m_cubes.Length, sizeof(float) + sizeof(float) * 3);
            m_computeBuffer.SetData(m_bufferDatas);
            computeShader.SetBuffer(m_kernelID, "buffer", m_computeBuffer);
        }
    }

    private void InitializeBufferData(Transform cube, ref BufferData bufferData)
    {
        bufferData.rotateSpeed = Random.Range(0, 2) == 0 ? 180f : -180f;
        bufferData.eulerAngle = cube.eulerAngles;
    }

    private void Update()
    {
        if (m_cubes == null || m_cubes.Length == 0)
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

        computeShader.SetFloat("deltaTime", Time.deltaTime);
        computeShader.Dispatch(m_kernelID, m_threadGroups, 1, 1);
        m_computeBuffer.GetData(m_bufferDatas, 0, 0, m_bufferDatas.Length);

        if(asyncGPUReadback)
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
        if(asyncGPUReadbackRequest.done && !asyncGPUReadbackRequest.hasError)
        {
            UpdateComputeBuffer();
        }
    }

    private void UpdateComputeBuffer()
    {
        if(!Application.isPlaying)
        {
            return;
        }

        for (int i = 0; i < m_cubes.Length; i++)
        {
            m_cubes[i].eulerAngles = m_bufferDatas[i].eulerAngle;
        }
    }

    private float m_deltaTime;
    private void UpdateGeneral()
    {
        m_deltaTime = Time.deltaTime;

        for (int i = 0; i < m_bufferDatas.Length; i++)
        {
            m_bufferDatas[i].eulerAngle.y += m_bufferDatas[i].rotateSpeed * m_deltaTime;
            m_cubes[i].eulerAngles = m_bufferDatas[i].eulerAngle;
        }
    }

    private void OnDisable()
    {
        ReleaseBuffer();
    }

    private void ReleaseBuffer()
    {
        if(m_computeBuffer != null)
        {
            m_computeBuffer.Release();
        }
    }
}
