using UnityEngine;

namespace FS2.FrustumCulling
{
    public class FrustumCullingShaderIDs
    {
        public static readonly int boundsBuffer = Shader.PropertyToID("boundsBuffer");
        public static readonly int resultBuffer = Shader.PropertyToID("resultBuffer");
        public static readonly int frustumPlanes = Shader.PropertyToID("frustumPlanes");
        public static readonly int _Count = Shader.PropertyToID("_Count");
        public static readonly int _HiZTextureSize = Shader.PropertyToID("_HiZTextureSize");
        public static readonly int _UNITY_MATRIX_MVP = Shader.PropertyToID("_UNITY_MATRIX_MVP");
        public static readonly int _HiZMap = Shader.PropertyToID("_HiZMap");
    }
}