#ifndef __VIRTUAL_TEXTURE_SAMPLER_INCLUDE__
#define __VIRTUAL_TEXTURE_SAMPLER_INCLUDE__

struct VTIndices
{
    int albedoIndex;
    int specularIndex;
    int normalIndex;
};
struct VTResult
{
    float3 albedo;
    float3 specular;
    float3 normal;
};
Texture2DArray<float4> _VirtualTex; SamplerState sampler_VirtualTex;
float4 _VirtualTex_TexelSize;
Texture2D<float4> _IndirectTex;
StructuredBuffer<VTIndices> _IndirectBuffer;

VTResult GetResult(float2 pixel, float indexFloat, float mip, out VTIndices indcs)
{
    uint index = indexFloat * 65535;
    VTResult r;
    indcs = _IndirectBuffer[index];
    indcs.albedoIndex = 0;
    indcs.specularIndex = 0;
     indcs.normalIndex = 0;
    r.albedo = indcs.albedoIndex >= 0 ? _VirtualTex.Load(uint4(pixel, indcs.albedoIndex, mip)).xyz : 1;
    r.specular = indcs.specularIndex >= 0 ? _VirtualTex.Load(uint4(pixel, indcs.specularIndex, mip)).xyz : 1;
    r.normal = indcs.normalIndex >= 0 ? _VirtualTex.Load(uint4(pixel, indcs.normalIndex, mip)).xyz : 1;
    return r;
}

VTResult GetIndResult( VTIndices indcs, float2 pixel, float mip)
{
    VTResult r;
    r.albedo = indcs.albedoIndex >= 0 ? _VirtualTex.Load(uint4(pixel, indcs.albedoIndex, mip)).xyz : 1;
    r.specular = indcs.specularIndex >= 0 ? _VirtualTex.Load(uint4(pixel, indcs.specularIndex, mip)).xyz : 1;
    r.normal = indcs.normalIndex >= 0 ? _VirtualTex.Load(uint4(pixel, indcs.normalIndex, mip)).xyz : 1;
    return r;
}

VTResult GetSearchResult(float2 localUV, uint2 chunk, float mip, float compareScale, float maxMip)
{
    float4 indirect = float4(0,0,1,0);//_IndirectTex[chunk];
    float2 pixel = localUV * indirect.z + indirect.xy;
    mip += log2(indirect.z / compareScale);
    mip = min(mip, maxMip);
    uint index = indirect.w * 65535;
    VTResult r;
    VTIndices indcs = _IndirectBuffer[index];
     indcs.albedoIndex = 0;
    indcs.specularIndex = 0;
     indcs.normalIndex = 0;
    r.albedo = indcs.albedoIndex >= 0 ? _VirtualTex.SampleLevel(sampler_VirtualTex, float3(pixel, indcs.albedoIndex), mip).xyz : 1;
    r.specular = indcs.specularIndex >= 0 ?  _VirtualTex.SampleLevel(sampler_VirtualTex, float3(pixel, indcs.specularIndex), mip).xyz : 1;
    r.normal = indcs.normalIndex >= 0 ?  _VirtualTex.SampleLevel(sampler_VirtualTex, float3(pixel, indcs.normalIndex), mip).xyz : 1;
    return r;
}

VTResult LerpVT(VTResult a, VTResult b, float w)
{
    VTResult r;
    r.albedo = lerp(a.albedo, b.albedo, w);
    r.specular = lerp(a.specular, b.specular, w);
    r.normal = lerp(a.normal, b.normal, w);
    return r;
}

VTResult SampleBilinear(uint2 chunk, float2 realNormalizedUV, float4 currentIndirect, float2 originSize, float mip, float maxMip)
{
    //originSize = originSize / pow(2, mip);
    float2 realAbsoluteUV = realNormalizedUV * originSize;
    float2 fracedAbsoluteUV = frac(realAbsoluteUV);
    bool2 valueCrossHalf = fracedAbsoluteUV > 0.5;
    float2 nextUV = realAbsoluteUV + (valueCrossHalf ? 1 : -1);
    float2 lerpWeight = abs(fracedAbsoluteUV - 0.5);
    VTIndices indcs;
    VTResult originPart = GetResult(realAbsoluteUV, currentIndirect.w, mip, indcs);
    //Start Other Three
    bool2 nextTest = nextUV > (originSize - 0.5);
    uint2 nextChunk = nextTest ? chunk + 1 : chunk;
    nextUV = nextTest ? (nextUV - originSize) : nextUV;
    bool2 nextTest1 = nextUV < 0;
    nextChunk = nextTest1 ? nextChunk - 1 : nextChunk;
    nextUV = nextTest1 ? (nextUV + originSize) : nextUV;
    float2 nextLocalUV = nextUV / originSize;
    nextTest = nextTest || nextTest1;
    //Sample Others
    VTResult xOriginYNext, xNextYOrigin, xNextYNext;
    [branch]
    if(nextTest.y)
        xOriginYNext = GetSearchResult(float2(realNormalizedUV.x, nextLocalUV.y), uint2(chunk.x, nextChunk.y), mip, currentIndirect.z, maxMip);
    else 
        xOriginYNext = GetIndResult(indcs, float2(realAbsoluteUV.x, nextUV.y), mip);
    [branch]
    if(nextTest.x)
        xNextYOrigin = GetSearchResult(float2(nextLocalUV.x, realNormalizedUV.y), uint2(nextChunk.x, chunk.y), mip, currentIndirect.z, maxMip);
    else
        xNextYOrigin = GetIndResult(indcs, float2(nextUV.x, realAbsoluteUV.y), mip);
    [branch]
    if(nextTest.x || nextTest.y)
        xNextYNext =  GetSearchResult(nextLocalUV, nextChunk, mip, currentIndirect.z, maxMip);
    else
        xNextYNext = GetIndResult(indcs, nextUV, mip);
    VTResult xOrigin = LerpVT(originPart, xOriginYNext, lerpWeight.y);
    VTResult xNext = LerpVT(xNextYOrigin, xNextYNext, lerpWeight.y);
    return LerpVT(xOrigin, xNext, lerpWeight.x);
}

VTResult SampleTrilinear(uint2 chunk, float2 localUV, float2 originSize, float maxMip)
{
    float4 indirect =  float4(0,0,1,0);
    float2 realNormalizedUV = localUV * indirect.z + indirect.xy;
    float2 realAbsoluteUV = realNormalizedUV * originSize;
    float2 chunkFloat = (float2)chunk;
    float4 dd = abs(float4(ddx(realAbsoluteUV), ddy(realAbsoluteUV)) + float4(ddx(chunkFloat) * originSize, ddy(chunkFloat) * originSize));
    dd.xy = max(dd.xy, dd.zw);
    dd.x = max(dd.x, dd.y);
    float mip = 0.5 * log2(dd.x);
    mip = min(maxMip, mip);
    float downMip = floor(mip);
    float upMip = ceil(mip);
    VTResult downResult = SampleBilinear(chunk, realNormalizedUV, indirect, originSize, downMip, maxMip);
    VTResult upResult = SampleBilinear(chunk, realNormalizedUV, indirect, originSize, upMip, maxMip);
    
    return LerpVT(downResult, upResult, frac(mip));
}

#endif