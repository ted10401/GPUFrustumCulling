
namespace UnityEngine
{
    public static class FrustumCullingUtility
    {
        private static Vector3 m_boundsMin;
        private static Vector3 m_boundsMax;
        private static Vector3 m_bounds;
        private static Vector3 m_normal;
        private static float m_planeDistance;
        private static float m_dot;
        public static bool TestPlanesAABBInternalFast(Vector4[] planes, Vector3 center, Vector3 extents)
        {
            m_boundsMin.x = center.x - extents.x;
            m_boundsMin.y = center.y - extents.y;
            m_boundsMin.z = center.z - extents.z;
            m_boundsMax.x = center.x + extents.x;
            m_boundsMax.y = center.y + extents.y;
            m_boundsMax.z = center.z + extents.z;

            for (int i = 0; i < planes.Length; i++)
            {
                m_normal.x = planes[i].x;
                m_normal.y = planes[i].y;
                m_normal.z = planes[i].z;
                m_planeDistance = planes[i].w;

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

        public static Vector4 ToVector4(this Plane plane)
        {
            Vector4 vector4 = plane.normal;
            vector4.w = plane.distance;
            return vector4;
        }
    }
}