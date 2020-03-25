
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using System.IO;
using static Unity.Mathematics.math;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Rendering;
public enum TextureType
{
    Tex2D = 0,
    Tex3D = 1,
    Cubemap = 2,
    Num = 3
};

public unsafe struct BC7CompressJob : IJobParallelFor
{
    [NativeDisableUnsafePtrRestriction]
    public uint4* dest;
    [NativeDisableUnsafePtrRestriction]
    public float4* source;
    public int width;
    public void Execute(int thread)
    {
        uint GetIndex(uint2 uv, uint width)
        {
            return uv.y * width + uv.x;
        }
        float4* b = stackalloc float4[4 * 4];
        int y = thread / (width / 4);
        int x = thread % (width / 4);
        for (uint i = 0, yy = 0; yy < 4; ++yy)
        {
            for (uint xx = 0; xx < 4; ++xx)
            {
                b[i] = source[GetIndex(uint2((uint)(x * 4 + xx), (uint)(y * 4 + yy)), (uint)width)];
                i++;
            }
        }
        DXTCompress.D3DXEncodeBC7(
           (byte*)(dest + thread),
           b,
           0);
    }
}

public unsafe struct BC6UHCompressJob : IJobParallelFor
{
    [NativeDisableUnsafePtrRestriction]
    public uint4* dest;
    [NativeDisableUnsafePtrRestriction]
    public float4* source;
    public int width;
    public void Execute(int thread)
    {
        uint GetIndex(uint2 uv, uint width)
        {
            return uv.y * width + uv.x;
        }
        float4* b = stackalloc float4[4 * 4];
        int y = thread / (width / 4);
        int x = thread % (width / 4);
        for (uint i = 0, yy = 0; yy < 4; ++yy)
        {
            for (uint xx = 0; xx < 4; ++xx)
            {
                b[i] = source[GetIndex(uint2((uint)(x * 4 + xx), (uint)(y * 4 + yy)), (uint)width)];
                i++;
            }
        }
        DXTCompress.D3DXEncodeBC6HU(
           (byte*)(dest + thread),
           b,
           0);
    }
}

public struct TextureData
{
    public uint width;
    public uint height;
    public uint depth;
    public TextureType textureType;
    public uint mipCount;
    public enum LoadFormat
    {
        LoadFormat_RGBA8 = 0,
        LoadFormat_RGBA16 = 1,
        LoadFormat_RGBAFloat16 = 2,
        LoadFormat_RGBAFloat32 = 3,
        LoadFormat_RGFLOAT16 = 4,
        LoadFormat_RG16 = 5,
        LoadFormat_BC7 = 6,
        LoadFormat_BC6H = 7,
        LoadFormat_UINT = 8,
        LoadFormat_UINT2 = 9,
        LoadFormat_UINT4 = 10
    };
    //TODO
    //Should Have Compress Type here
    public LoadFormat format;
};


public unsafe class TextureExporter : MonoBehaviour
{
    public ComputeShader readCS;
    public ComputeShader bc6Compress;
    public ComputeShader bc7Compress;
    public Texture texture;
    public string path = "Cubemap.vtex";
    public bool useMipMap = true;
    public TextureData.LoadFormat tex2DFormat = TextureData.LoadFormat.LoadFormat_RGBAFloat16;
    public bool isCubemapCompress = true;
    void PrintCubemap()
    {
        float3[][] forwd = new float3[6][];
        for (int i = 0; i < 6; ++i)
            forwd[i] = new float3[4];
        //Forward
        forwd[4][0] = normalize(float3(-1, 1, 1));
        forwd[4][1] = normalize(float3(1, 1, 1));
        forwd[4][2] = normalize(float3(-1, -1, 1));
        forwd[4][3] = normalize(float3(1, -1, 1));
        //Left
        forwd[1][0] = normalize(float3(-1, 1, -1));
        forwd[1][1] = normalize(float3(-1, 1, 1));
        forwd[1][2] = normalize(float3(-1, -1, -1));
        forwd[1][3] = normalize(float3(-1, -1, 1));
        //Back
        forwd[5][0] = normalize(float3(1, 1, -1));
        forwd[5][1] = normalize(float3(-1, 1, -1));
        forwd[5][2] = normalize(float3(1, -1, -1));
        forwd[5][3] = normalize(float3(-1, -1, -1));

        //Right
        forwd[0][0] = normalize(float3(1, 1, 1));
        forwd[0][1] = normalize(float3(1, 1, -1));
        forwd[0][2] = normalize(float3(1, -1, 1));
        forwd[0][3] = normalize(float3(1, -1, -1));

        //up
        forwd[2][0] = normalize(float3(-1, 1, -1));
        forwd[2][1] = normalize(float3(1, 1, -1));
        forwd[2][2] = normalize(float3(-1, 1, 1));
        forwd[2][3] = normalize(float3(1, 1, 1));

        //down
        forwd[3][0] = normalize(float3(-1, -1, 1));
        forwd[3][1] = normalize(float3(1, -1, 1));
        forwd[3][2] = normalize(float3(-1, -1, -1));
        forwd[3][3] = normalize(float3(1, -1, -1));
        uint2 size = uint2(max((uint)texture.width, 1024), max((uint)texture.height, 1024));

        ComputeBuffer cb = new ComputeBuffer((int)size.x * (int)size.y, sizeof(float4), ComputeBufferType.Default);
        int mipCount = useMipMap ? (int)(log2(size.x / 16) + 0.1) : 1;
        TextureData data = new TextureData
        {
            depth = 6,
            width = size.x,
            height = size.y,
            mipCount = (uint)mipCount,
            format = TextureData.LoadFormat.LoadFormat_RGBAFloat16,
            textureType = TextureType.Cubemap
        };
        readCS.SetTexture(0, "_MainTex", texture);
        readCS.SetBuffer(0, "_ResultBuffer", cb);
        float4[] readbackValues = new float4[size.x * size.y];
        NativeList<byte> lst = new NativeList<byte>((int)(size.x * size.y * 1.4), Unity.Collections.Allocator.Temp);
        byte* headerPtr = (byte*)data.Ptr();
        for (int i = 0; i < sizeof(TextureData); ++i)
        {
            lst.Add(headerPtr[i]);
        }
        Vector4[] setterArray = new Vector4[4];
        for (int face = 0; face < 6; ++face)
        {
            size = uint2(max((uint)texture.width, 1024), max((uint)texture.height, 1024));
            for (int i = 0; i < mipCount; ++i)
            {
                readCS.SetInt("_TargetMipLevel", i);
                readCS.SetInt("_Count", (int)size.x);
                for (int j = 0; j < 4; ++j)
                {
                    setterArray[j] = (Vector3)forwd[face][j];
                }
                readCS.SetVectorArray("_Directions", setterArray);
                readCS.Dispatch(0, max(1, Mathf.CeilToInt(size.x / 8f)), max(1, Mathf.CeilToInt(size.y / 8f)), 1);
                int cum = (int)(size.x * size.y);
                cb.GetData(readbackValues, 0, 0, cum);
                int pixelSize = 0;
                if (isCubemapCompress)
                {
                    pixelSize = 1;
                    NativeArray<byte> compressedData = new NativeArray<byte>(cum, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                    BC6UHCompressJob job;
                    job.dest = (uint4*)compressedData.GetUnsafePtr();
                    job.source = readbackValues.Ptr();
                    job.width = (int)size.x;
                    JobHandle handle = job.Schedule((cum / 16), max((cum / 16) / 20, 1));
                    handle.Complete();
                    for (int a = 0; a < compressedData.Length; ++a)
                    {
                        lst.Add(compressedData[a]);
                    }
                }
                else
                {
                    pixelSize = sizeof(half4);
                    for (int j = 0; j < cum; ++j)
                    {
                        half4 hlfResult = (half4)readbackValues[j];
                        byte* b = (byte*)hlfResult.Ptr();
                        for (int z = 0; z < sizeof(half4); ++z)
                        {
                            lst.Add(b[z]);
                        }
                    }

                }
                for (int j = cum * pixelSize; j < 512; ++j)
                {
                    lst.Add(0);
                }
                size /= 2;
                size = max(size, 1);
            }
        }
        byte[] finalArray = new byte[lst.Length];
        UnsafeUtility.MemCpy(finalArray.Ptr(), lst.unsafePtr, lst.Length);
        using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        {
            fs.Write(finalArray, 0, lst.Length);
        }
    }
    void PrintTex2D()
    {
        uint2 size = uint2((uint)texture.width, (uint)texture.height);
        int mipCount = useMipMap ? min((int)(log2(size.x / 32) + 0.1), (int)(log2(size.y / 32) + 0.1)) : 1;
        Debug.Log(mipCount);
        TextureData data;
        data.width = size.x;
        data.height = size.y;
        data.depth = 1;
        data.textureType = TextureType.Tex2D;
        data.mipCount = (uint)mipCount;
        data.format = tex2DFormat;
        NativeList<byte> lst = new NativeList<byte>((int)(size.x * size.y * 1.4 * 8), Unity.Collections.Allocator.Temp);
        byte* headerPtr = (byte*)data.Ptr();
        for (int i = 0; i < sizeof(TextureData); ++i)
        {
            lst.Add(headerPtr[i]);
        }

        if (tex2DFormat == TextureData.LoadFormat.LoadFormat_BC6H)
        {
            uint4[] datas = new uint4[size.x * size.y];
            CommandBuffer cbuffer = new CommandBuffer();
            GPUCompress compress = new GPUCompress(bc7Compress, bc6Compress, (int)size.x, (int)size.y, 0x80000, 0);
            for (int i = 0; i < mipCount; ++i)
            {
                compress.Compress(texture as Texture2D, (int)size.x, (int)size.y, i, cbuffer, true);
                Graphics.ExecuteCommandBuffer(cbuffer);
                var len = compress.GetData((int)size.x, (int)size.y, datas);
                for (int a = 0; a < len; ++a)
                {
                    uint4 value = datas[a];
                    byte* ptr = (byte*)value.Ptr();
                    for (int b = 0; b < sizeof(uint4); ++b)
                    {
                        lst.Add(ptr[b]);
                    }
                }
                for (int a = datas.Length * sizeof(uint4); a < 512; ++a)
                {
                    lst.Add(0);
                }
                size /= 2;
                size = max(size, 1);
            }

            compress.Dispose();
            cbuffer.Dispose();
        }
        else if (tex2DFormat == TextureData.LoadFormat.LoadFormat_BC7)
        {
            uint4[] datas = new uint4[size.x * size.y];
            CommandBuffer cbuffer = new CommandBuffer();
            GPUCompress compress = new GPUCompress(bc7Compress, bc6Compress, (int)size.x, (int)size.y, 0x80000, 0);
            for (int i = 0; i < mipCount; ++i)
            {
                compress.Compress(texture as Texture2D, (int)size.x, (int)size.y, i, cbuffer, false);
                Graphics.ExecuteCommandBuffer(cbuffer);
                var len = compress.GetData((int)size.x, (int)size.y, datas);
                for (int a = 0; a < len; ++a)
                {
                    uint4 value = datas[a];
                    byte* ptr = (byte*)value.Ptr();
                    for (int b = 0; b < sizeof(uint4); ++b)
                    {
                        lst.Add(ptr[b]);
                    }
                }
                for (int a = datas.Length * sizeof(uint4); a < 512; ++a)
                {
                    lst.Add(0);
                }
                size /= 2;
                size = max(size, 1);
            }

            compress.Dispose();
            cbuffer.Dispose();
        }
        else if ((int)tex2DFormat >= 8 && (int)tex2DFormat <= 10)
        {
            //integer texture

            int pass = 0;
            uint[] dataArray = null;
            readCS.SetVector("_TextureSize", float4(size.x - 0.5f, size.y - 0.5f, size.x, size.y));
            readCS.SetInt("_Count", (int)size.x);
            ComputeBuffer cb = null;
            switch (tex2DFormat)
            {
                case TextureData.LoadFormat.LoadFormat_UINT:
                    pass = 2;
                    dataArray = new uint[size.x * size.y];
                    cb = new ComputeBuffer(dataArray.Length, sizeof(int));
                    readCS.SetTexture(pass, "_UIntTexture", texture);
                    readCS.SetBuffer(pass, "_ResultInt1Buffer", cb);
                    break;
                case TextureData.LoadFormat.LoadFormat_UINT2:
                    pass = 3;
                    dataArray = new uint[size.x * size.y * 2];
                    cb = new ComputeBuffer(dataArray.Length, sizeof(int));
                    readCS.SetTexture(pass, "_UInt2Texture", texture);
                    readCS.SetBuffer(pass, "_ResultInt2Buffer", cb);
                    break;
                case TextureData.LoadFormat.LoadFormat_UINT4:
                    pass = 4;
                    dataArray = new uint[size.x * size.y * 4];
                    cb = new ComputeBuffer(dataArray.Length, sizeof(int));
                    readCS.SetTexture(pass, "_UInt4Texture", texture);
                    readCS.SetBuffer(pass, "_ResultInt4Buffer", cb);
                    break;
            }
            readCS.Dispatch(pass, (int)size.x / 8, (int)size.y / 8, 1);
            cb.GetData(dataArray);
            foreach (var i in dataArray)
            {
                uint p = i;
                byte* ptr = (byte*)p.Ptr();
                for (uint a = 0; a < sizeof(int); ++a)
                {
                    lst.Add(ptr[a]);
                }
            }

            cb.Dispose();
        }
        else
        {
            ComputeBuffer cb = new ComputeBuffer((int)size.x * (int)size.y, sizeof(float4), ComputeBufferType.Default);
            readCS.SetTexture(1, "_MainTex2D", texture);
            readCS.SetBuffer(1, "_ResultBuffer", cb);
            float4[] readbackValues = new float4[size.x * size.y];
            float* byteArray = stackalloc float[4];
            for (int i = 0; i < mipCount; ++i)
            {
                readCS.SetVector("_TextureSize", float4(size.x - 0.5f, size.y - 0.5f, size.x, size.y));
                readCS.SetInt("_Count", (int)size.x);
                readCS.SetInt("_TargetMipLevel", i);
                readCS.Dispatch(1, max(1, Mathf.CeilToInt(size.x / 8f)), max(1, Mathf.CeilToInt(size.y / 8f)), 1);
                int cum = (int)(size.x * size.y);
                cb.GetData(readbackValues, 0, 0, cum);
                switch (tex2DFormat)
                {
                    case TextureData.LoadFormat.LoadFormat_RGBAFloat16:
                        for (int j = 0; j < cum; ++j)
                        {
                            half4 hlfResult = (half4)readbackValues[j];
                            byte* b = (byte*)hlfResult.Ptr();
                            for (int z = 0; z < sizeof(half4); ++z)
                            {
                                lst.Add(b[z]);
                            }
                        }
                        for (int j = cum * sizeof(half4); j < 512; ++j)
                        {
                            lst.Add(0);
                        }
                        break;
                    case TextureData.LoadFormat.LoadFormat_RGFLOAT16:
                        for (int j = 0; j < cum; ++j)
                        {
                            half2 hlfResult = (half2)readbackValues[j].xy;
                            byte* b = (byte*)hlfResult.Ptr();
                            for (int z = 0; z < sizeof(half2); ++z)
                            {
                                lst.Add(b[z]);
                            }
                        }
                        for (int j = cum * sizeof(half2); j < 512; ++j)
                        {
                            lst.Add(0);
                        }
                        break;
                    case TextureData.LoadFormat.LoadFormat_RGBAFloat32:
                        for (int j = 0; j < cum; ++j)
                        {
                            float4 hlfResult = readbackValues[j];
                            byte* b = (byte*)hlfResult.Ptr();
                            for (int z = 0; z < sizeof(float4); ++z)
                            {
                                lst.Add(b[z]);
                            }
                        }
                        for (int j = cum * sizeof(float4); j < 512; ++j)
                        {
                            lst.Add(0);
                        }
                        break;
                    case TextureData.LoadFormat.LoadFormat_RGBA16:
                        for (int j = 0; j < cum; ++j)
                        {
                            ushort* shorts = (ushort*)byteArray;
                            float* flt = (float*)readbackValues[j].Ptr();
                            for (int bb = 0; bb < 4; ++bb)
                                shorts[bb] = (ushort)(flt[bb] * 65535);
                            byte* b = (byte*)shorts;
                            for (int z = 0; z < sizeof(ushort) * 4; ++z)
                            {
                                lst.Add(b[z]);
                            }
                        }
                        for (int j = cum * sizeof(ushort) * 4; j < 512; ++j)
                        {
                            lst.Add(0);
                        }
                        break;
                    case TextureData.LoadFormat.LoadFormat_RG16:
                        for (int j = 0; j < cum; ++j)
                        {
                            ushort* shorts = (ushort*)byteArray;
                            float* flt = (float*)readbackValues[j].Ptr();
                            for (int bb = 0; bb < 2; ++bb)
                                shorts[bb] = (ushort)(flt[bb] * 65535);
                            byte* b = (byte*)shorts;
                            for (int z = 0; z < sizeof(ushort) * 2; ++z)
                            {
                                lst.Add(b[z]);
                            }
                        }
                        for (int j = cum * sizeof(ushort) * 2; j < 512; ++j)
                        {
                            lst.Add(0);
                        }
                        break;
                    case TextureData.LoadFormat.LoadFormat_RGBA8:
                        for (int j = 0; j < cum; ++j)
                        {
                            byte* shorts = (byte*)byteArray;
                            float* flt = (float*)readbackValues[j].Ptr();
                            for (int bb = 0; bb < 4; ++bb)
                                shorts[bb] = (byte)(flt[bb] * 255);
                            for (int z = 0; z < sizeof(byte) * 4; ++z)
                            {
                                lst.Add(shorts[z]);
                            }
                        }
                        for (int j = cum * sizeof(byte) * 4; j < 512; ++j)
                        {
                            lst.Add(0);
                        }
                        break;
                        /*  case TextureData.LoadFormat.LoadFormat_BC7:
                              {


                                  NativeArray<byte> compressedData = new NativeArray<byte>(cum, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                                  BC7CompressJob job;
                                  job.dest = (uint4*)compressedData.GetUnsafePtr();
                                  job.source = readbackValues.Ptr();
                                  job.width = (int)size.x;
                                  JobHandle handle = job.Schedule((cum / 16), max((cum / 16) / 20, 1));
                                  handle.Complete();
                                  for (int a = 0; a < compressedData.Length; ++a)
                                  {
                                      lst.Add(compressedData[a]);
                                  }
                                  for (int j = cum * sizeof(byte); j < 512; ++j)
                                  {
                                      lst.Add(0);
                                  }
                }
                break;
                    case TextureData.LoadFormat.LoadFormat_BC6H:
                        {
                            NativeArray<byte> compressedData = new NativeArray<byte>(cum, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                            BC6UHCompressJob job;
                            job.dest = (uint4*)compressedData.GetUnsafePtr();
                            job.source = readbackValues.Ptr();
                            job.width = (int)size.x;
                            JobHandle handle = job.Schedule((cum / 16), max((cum / 16) / 20, 1));
                            handle.Complete();
                            for (int a = 0; a < compressedData.Length; ++a)
                            {
                                lst.Add(compressedData[a]);
                            }
                            for (int j = cum * sizeof(byte); j < 512; ++j)
                            {
                                lst.Add(0);
                            }
                        }
                        break;*/
                }

                size /= 2;
                size = max(size, 1);
            }
            cb.Dispose();
        }
        byte[] finalArray = new byte[lst.Length];
        UnsafeUtility.MemCpy(finalArray.Ptr(), lst.unsafePtr, lst.Length);
        using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        {
            fs.Write(finalArray, 0, lst.Length);
        }
    }
    [EasyButtons.Button]
    public void Print()
    {
        if (texture.dimension == UnityEngine.Rendering.TextureDimension.Cube)
            PrintCubemap();
        if (texture.dimension == UnityEngine.Rendering.TextureDimension.Tex2D)
            PrintTex2D();

    }
}
