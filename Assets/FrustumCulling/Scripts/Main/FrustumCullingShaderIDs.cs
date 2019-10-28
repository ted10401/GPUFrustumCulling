using UnityEngine;

namespace FS2.FrustumCulling
{
    public class FrustumCullingShaderIDs
    {
        public static readonly int boundsBuffer = Shader.PropertyToID("boundsBuffer");
        public static readonly int resultBuffer = Shader.PropertyToID("resultBuffer");
        public static readonly int frustumPlanes = Shader.PropertyToID("frustumPlanes");
        public static readonly int _Count = Shader.PropertyToID("_Count");
        public static readonly int _LastVp = Shader.PropertyToID("_LastVp");
    }
}