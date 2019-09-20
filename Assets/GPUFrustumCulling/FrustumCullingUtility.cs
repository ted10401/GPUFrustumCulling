
namespace UnityEngine
{
    public static class FrustumCullingUtility
    {
        private static Vector3 m_center;
        private static Vector3 m_extents;
        private static Vector3 m_boundsMin;
        private static Vector3 m_boundsMax;
        private static Vector3 m_bounds;
        private static Vector3 m_normal;
        private static float m_planeDistance;
        private static float m_dot;
        public static bool TestPlanesAABBInternalFast(Vector4[] planes, Bounds bounds)
        {
            m_center = bounds.center;
            m_extents = bounds.extents;
            m_boundsMin.x = m_center.x - m_extents.x;
            m_boundsMin.y = m_center.y - m_extents.y;
            m_boundsMin.z = m_center.z - m_extents.z;
            m_boundsMax.x = m_center.x + m_extents.x;
            m_boundsMax.y = m_center.y + m_extents.y;
            m_boundsMax.z = m_center.z + m_extents.z;

            for (int planeIndex = 0; planeIndex < planes.Length; planeIndex++)
            {
                m_normal.x = planes[planeIndex].x;
                m_normal.y = planes[planeIndex].y;
                m_normal.z = planes[planeIndex].z;
                m_planeDistance = planes[planeIndex].w;

                m_bounds.x = m_normal.x >= 0 ? m_boundsMax.x : m_boundsMin.x;
                m_bounds.y = m_normal.y >= 0 ? m_boundsMax.y : m_boundsMin.y;
                m_bounds.z = m_normal.z >= 0 ? m_boundsMax.z : m_boundsMin.z;

                m_dot = m_normal.x * m_bounds.x + m_normal.y * m_bounds.y + m_normal.z * m_bounds.z;

                if (m_dot + m_planeDistance < 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}