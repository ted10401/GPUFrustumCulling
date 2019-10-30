Shader "Hidden/HiZ/DebugHiZBuffer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    struct a2v
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct v2f
    {
        float4 vertex : SV_POSITION;
        float2 uv : TEXCOORD0;
    };

    Texture2D _MainTex;
    SamplerState sampler_MainTex;

    float4 _MainTex_TexelSize;
    Texture2D _CameraDepthTexture;
    SamplerState sampler_CameraDepthTexture;

    int _LOD;
	float _Strength;

    v2f vert(a2v input)
    {
        v2f output;

        output.vertex = UnityObjectToClipPos(input.vertex.xyz);
        output.uv = input.uv;

    #if UNITY_UV_STARTS_AT_TOP
        if (_MainTex_TexelSize.y < 0)
            output.uv.y = 1. - input.uv.y;
    #endif

        return output;
    }

    float4 frag(v2f input) : SV_Target
    {
        float4 output = _MainTex.SampleLevel(sampler_MainTex, input.uv, _LOD);
        return output.r * _Strength;
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    }
}
