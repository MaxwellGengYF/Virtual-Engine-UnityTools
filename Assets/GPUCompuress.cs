using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public unsafe class GPUCompress : System.IDisposable
{
    struct BufferBC6HBC7
    {
        int4 color;
    }
    const int TEX_COMPRESS_BC7_QUICK = 0x100000;
    const int TEX_COMPRESS_BC7_USE_3SUBSETS = 0x80000;
    ComputeBuffer err1Buffer;
    ComputeBuffer err2Buffer;
    ComputeBuffer outputBuffer;
    ComputeShader bc7Compute;
    ComputeShader bc6Compute;
    int mFlags;
    bool m_bc7_mode02;
    bool m_bc7_mode137;
    float mAlphaWeight;
    public GPUCompress(ComputeShader bc7Shader, ComputeShader bc6Shader, int width, int height, int flags, float alphaWeight)
    {
        bc7Compute = bc7Shader;
        bc6Compute = bc6Shader;
        mFlags = flags;
        mAlphaWeight = alphaWeight;
        if ((flags & TEX_COMPRESS_BC7_QUICK) != 0)
        {
            m_bc7_mode02 = false;
            m_bc7_mode137 = false;
        }
        else
        {
            m_bc7_mode02 = (flags & TEX_COMPRESS_BC7_USE_3SUBSETS) != 0;
            m_bc7_mode137 = true;
        }
        ulong xblocks = (ulong)max(1, (width + 3) >> 2);
        ulong yblocks = (ulong)max(1, (height + 3) >> 2);
        ulong num_blocks = xblocks * yblocks;
        err1Buffer = new ComputeBuffer((int)num_blocks, sizeof(BufferBC6HBC7));
        err2Buffer = new ComputeBuffer((int)num_blocks, sizeof(BufferBC6HBC7));
        outputBuffer = new ComputeBuffer((int)num_blocks, sizeof(BufferBC6HBC7));
    }
    private void RunComputeShader(
        ComputeShader cs,
        Texture inputTex,
        ComputeBuffer inbuffer,
        ComputeBuffer outBuffer,
        CommandBuffer cbuffer,
        int kernel, int dispatchCount)
    {
        if (inputTex) cbuffer.SetComputeTextureParam(cs, kernel, g_Input, inputTex, 0);
        if (inbuffer != null) cbuffer.SetComputeBufferParam(cs, kernel, g_InBuff, inbuffer);
        if (outBuffer != null) cbuffer.SetComputeBufferParam(cs, kernel, g_OutBuff, outBuffer);
        cbuffer.DispatchCompute(cs, kernel, dispatchCount, 1, 1);
    }
    static int g_tex_width = Shader.PropertyToID("g_tex_width");
    static int g_Input = Shader.PropertyToID("g_Input");
    static int g_InBuff = Shader.PropertyToID("g_InBuff");
    static int g_OutBuff = Shader.PropertyToID("g_OutBuff");
    static int g_num_block_x = Shader.PropertyToID("g_num_block_x");
    static int g_format = Shader.PropertyToID("g_format");
    static int g_mode_id = Shader.PropertyToID("g_mode_id");
    static int g_start_block_id = Shader.PropertyToID("g_start_block_id");
    static int g_num_total_blocks = Shader.PropertyToID("g_num_total_blocks");
    static int g_alpha_weight = Shader.PropertyToID("g_alpha_weight");
    public void Compress(Texture2D sourceTex, int width, int height, int mip, CommandBuffer cbuffer, bool isHDR)
    {
        cbuffer.SetGlobalInt("_TargetMip", mip);
        ulong xblocks = (ulong)max(1, (width + 3) >> 2);
        ulong yblocks = (ulong)max(1, (height + 3) >> 2);
        bool isbc7 = !isHDR;
        const int MAX_BLOCK_BATCH = 64;
        var num_total_blocks = (int)(xblocks * yblocks);
        int num_blocks = num_total_blocks;
        int start_block_id = 0;
        const int DXGI_FORMAT_BC6H_UF16 = 95;
        const int DXGI_FORMAT_BC7_UNORM = 97;
        int[] modes = new int[] { 1, 3, 7 };
        while (num_blocks > 0)
        {
            int n = min(num_blocks, MAX_BLOCK_BATCH);
            int uThreadGroupCount = n;
            var cs = isbc7 ? bc7Compute : bc6Compute;
            cbuffer.SetComputeIntParam(cs, g_tex_width, sourceTex.width);
            cbuffer.SetComputeIntParam(cs, g_num_block_x, (int)xblocks);
            cbuffer.SetComputeIntParam(cs, g_format, isbc7 ? DXGI_FORMAT_BC7_UNORM : DXGI_FORMAT_BC6H_UF16);
            cbuffer.SetComputeIntParam(cs, g_mode_id, 0);
            cbuffer.SetComputeIntParam(cs, g_start_block_id, start_block_id);
            cbuffer.SetComputeIntParam(cs, g_num_total_blocks, num_total_blocks);
            cbuffer.SetComputeFloatParam(cs, g_alpha_weight, mAlphaWeight);


            if (isbc7)
            {

                const int TryMode456CS_Kernel = 0;
                const int TryMode137CS_Kernel = 1;
                const int TryMode02CS_Kernel = 2;
                const int EncodeBlockCS_Kernel = 3;
                RunComputeShader(bc7Compute, sourceTex, null, err1Buffer, cbuffer, TryMode456CS_Kernel, max((uThreadGroupCount + 3) / 4, 1));
                if (m_bc7_mode137)
                {
                    modes[0] = 1;
                    modes[1] = 3;
                    modes[2] = 7;
                    for (uint i = 0; i < 3; ++i)
                    {
                        cbuffer.SetComputeIntParam(bc7Compute, g_mode_id, modes[i]);
                        RunComputeShader(bc7Compute, sourceTex, ((i & 1) != 0) ? err2Buffer : err1Buffer, ((i & 1) != 0) ? err1Buffer : err2Buffer, cbuffer, TryMode137CS_Kernel, uThreadGroupCount);
                    }
                }
                if (m_bc7_mode02)
                {
                    modes[0] = 0;
                    modes[1] = 2;
                    for (int i = 0; i < 2; ++i)
                    {
                        cbuffer.SetComputeIntParam(bc7Compute, g_mode_id, modes[i]);
                        RunComputeShader(
                            bc7Compute,
                            sourceTex,
                            ((i & 1) != 0) ? err1Buffer : err2Buffer,
                            ((i & 1) != 0) ? err2Buffer : err1Buffer,
                            cbuffer,
                            TryMode02CS_Kernel, uThreadGroupCount);

                    }
                }
                RunComputeShader(
                    bc7Compute,
                    sourceTex,
                    (m_bc7_mode02 || m_bc7_mode137) ? err2Buffer : err1Buffer,
                    outputBuffer,
                    cbuffer, EncodeBlockCS_Kernel,
                    max((uThreadGroupCount + 3) / 4, 1));

            }
            else
            {
                const int TryModeG10CS = 0;
                const int TryModeLE10CS = 1;
                const int EncodeBlockCS = 2;
                RunComputeShader(
                    bc6Compute,
                    sourceTex,
                    null,
                    err1Buffer,
                    cbuffer,
                    TryModeG10CS,
                    max((uThreadGroupCount + 3) / 4, 1));
                for (int i = 0; i < 10; ++i)
                {
                    cbuffer.SetComputeIntParam(bc6Compute, g_mode_id, i);
                    RunComputeShader(
                        bc6Compute,
                        sourceTex,
                        ((i & 1) != 0) ? err2Buffer : err1Buffer,
                        ((i & 1) != 0) ? err1Buffer : err2Buffer,
                        cbuffer,
                        TryModeLE10CS,
                        max((uThreadGroupCount + 1) / 2, 1));
                }
                RunComputeShader(
                    bc6Compute,
                    sourceTex,
                    err1Buffer,
                    outputBuffer,
                    cbuffer,
                    EncodeBlockCS,
                    max((uThreadGroupCount + 1) / 2, 1));
            }
            start_block_id += n;
            num_blocks -= n;
        }
    }

    public int GetData(int width, int height, uint4[] values)
    {
        ulong xblocks = (ulong)max(1, (width + 3) >> 2);
        ulong yblocks = (ulong)max(1, (height + 3) >> 2);
        int v = (int)(xblocks * yblocks);
        outputBuffer.GetData(values, 0, 0,v);
        return v;
    }
    public void Dispose()
    {
        if (err1Buffer != null) err1Buffer.Dispose();
        if (err2Buffer != null) err2Buffer.Dispose();
        err1Buffer = null;
        err2Buffer = null;
        bc7Compute = null;
    }
}
