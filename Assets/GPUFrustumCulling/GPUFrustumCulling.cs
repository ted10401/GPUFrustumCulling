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

    /// <summary>
    /// This is crappy performant, but easiest version of TestPlanesAABBFast to use.
    /// </summary>
    /// <param name="planes"></param>
    /// <param name="bounds"></param>
    /// <returns></returns>
    private static Vector3 m_center;
    private static Vector3 m_extents;
    private static Vector3 m_min;
    private static Vector3 m_max;
    public static bool TestPlanesAABBInternalFast(Vector4[] planes, Bounds bounds)
    {
        m_center = bounds.center;
        m_extents = bounds.extents;
        m_min.x = m_center.x - m_extents.x;
        m_min.y = m_center.y - m_extents.y;
        m_min.z = m_center.z - m_extents.z;
        m_max.x = m_center.x + m_extents.x;
        m_max.y = m_center.y + m_extents.y;
        m_max.z = m_center.z + m_extents.z;

        return TestPlanesAABBInternalFast(planes, m_min, m_max);
    }

    /// <summary>
    /// This is a faster AABB cull than brute force that also gives additional info on intersections.
    /// Calling Bounds.Min/Max is actually quite expensive so as an optimization you can precalculate these.
    /// http://www.lighthouse3d.com/tutorials/view-frustum-culling/geometric-approach-testing-boxes-ii/
    /// </summary>
    /// <param name="planes"></param>
    /// <param name="boundsMin"></param>
    /// <param name="boundsMax"></param>
    /// <returns></returns>
    private static Vector3 m_vmin;
    private static Vector3 m_vmax;
    private static Vector3 m_normal;
    private static float m_planeDistance;
    public static bool TestPlanesAABBInternalFast(Vector4[] planes, Vector3 boundsMin, Vector3 boundsMax)
    {
        for (int planeIndex = 0; planeIndex < planes.Length; planeIndex++)
        {
            m_normal.x = planes[planeIndex].x;
            m_normal.y = planes[planeIndex].y;
            m_normal.z = planes[planeIndex].z;
            m_planeDistance = planes[planeIndex].w;

            // X axis
            if (m_normal.x < 0)
            {
                m_vmin.x = boundsMin.x;
                m_vmax.x = boundsMax.x;
            }
            else
            {
                m_vmin.x = boundsMax.x;
                m_vmax.x = boundsMin.x;
            }

            // Y axis
            if (m_normal.y < 0)
            {
                m_vmin.y = boundsMin.y;
                m_vmax.y = boundsMax.y;
            }
            else
            {
                m_vmin.y = boundsMax.y;
                m_vmax.y = boundsMin.y;
            }

            // Z axis
            if (m_normal.z < 0)
            {
                m_vmin.z = boundsMin.z;
                m_vmax.z = boundsMax.z;
            }
            else
            {
                m_vmin.z = boundsMax.z;
                m_vmax.z = boundsMin.z;
            }

            var dot1 = m_normal.x * m_vmin.x + m_normal.y * m_vmin.y + m_normal.z * m_vmin.z;
            if (dot1 + m_planeDistance < 0)
                return false;
        }

        return true;
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
        Vector4[] frustumPlanes4 = new Vector4[frustumPlanes.Length];
        for(int i = 0; i < frustumPlanes4.Length; i++)
        {
            frustumPlanes4[i] = frustumPlanes[i].normal;
            frustumPlanes4[i].w = frustumPlanes[i].distance;
        }

        for (int i = 0; i < m_renderers.Length; i++)
        {
            //m_renderers[i].enabled = GeometryUtility.TestPlanesAABB(frustumPlanes, m_colliders[i].bounds);
            m_renderers[i].enabled = TestPlanesAABBInternalFast(frustumPlanes4, m_colliders[i].bounds);
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
