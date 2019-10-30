#ifndef __HIZ_INCLUDE__
#define __HIZ_INCLUDE__

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
    float4 scrPos : TEXCOORD1;
};

Texture2D _MainTex;
SamplerState sampler_MainTex;

Texture2D _CameraDepthTexture;
SamplerState sampler_CameraDepthTexture;

float4 _MainTex_TexelSize;

v2f vert(a2v i)
{
    v2f output;

    output.vertex = UnityObjectToClipPos(i.vertex.xyz);
    output.uv = i.uv;

    output.scrPos = ComputeScreenPos(output.vertex);

    return output;
}

float4 blit(v2f input) : SV_Target
{
    float cameraDepth = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, input.uv).r * 1.8;
    return float4(cameraDepth, 0, 0 ,0);
}

float4 reduce(v2f input) : SV_Target
{
    #if SHADER_API_METAL
        int2 xy = (int2) (input.uv * (_MainTex_TexelSize.zw - 1));
        float4 texels[2] = {
            float4(_MainTex.mips[0][xy].rg, _MainTex.mips[0][xy + int2(1, 0)].rg),
            float4(_MainTex.mips[0][xy + int2(0, 1)].rg, _MainTex.mips[0][xy + 1].rg)
        };
    
        float4 r = float4(texels[0].rb, texels[1].rb);
        float4 g = float4(texels[0].ga, texels[1].ga);
    #else
        float4 r = _MainTex.GatherRed  (sampler_MainTex, input.uv);
        float4 g = _MainTex.GatherGreen(sampler_MainTex, input.uv);
    #endif
    

    float minimum = min(min(min(r.x, r.y), r.z), r.w);
    float maximum = max(max(max(g.x, g.y), g.z), g.w);
    return float4(minimum, maximum, 1.0, 1.0);
}

#endif
