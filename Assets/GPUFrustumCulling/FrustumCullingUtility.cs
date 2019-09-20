
namespace UnityEngine
{
    public static class FrustumCullingUtility
    {
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
        private static float m_dot;
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

                m_dot = m_normal.x * m_vmin.x + m_normal.y * m_vmin.y + m_normal.z * m_vmin.z;
                if (m_dot + m_planeDistance < 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}