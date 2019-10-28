Shader "Hidden/HiZBufferDebugger"
{
    Properties
    {
       _MainTex ("Texture", 2D) = "white" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    struct Input
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct Varyings
    {
        float4 vertex : SV_POSITION;
        float2 uv : TEXCOORD0;
    };

    Texture2D _MainTex;
    SamplerState sampler_MainTex;

    float4 _MainTex_TexelSize;

    int _LOD;
	int _Mode;
	float _Multiplier;

    Varyings vertex(Input input)
    {
        Varyings output;

        output.vertex = UnityObjectToClipPos(input.vertex.xyz);
        output.uv = input.uv;

    #if UNITY_UV_STARTS_AT_TOP
        if (_MainTex_TexelSize.y < 0)
            output.uv.y = 1. - input.uv.y;
    #endif

        return output;
    }

    float4 fragment(in Varyings input) : SV_Target
    {
        float2 rg = _MainTex.SampleLevel(sampler_MainTex, input.uv, _LOD).rg * _Multiplier;
		return lerp(float4(rg.r, 0., 0., 1.), float4(0., rg.g, 0., 1.), _Mode);
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vertex
            #pragma fragment fragment
            ENDCG
        }
    }
}
