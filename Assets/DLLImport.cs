using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using Unity.Mathematics;
public static unsafe class DXTCompress
{
    [DllImport("Dll1")] public static extern void D3DXDecodeBC1(float4* pColor, byte* pBC);
    [DllImport("Dll1")] public static extern void D3DXDecodeBC2(float4* pColor, byte* pBC);
    [DllImport("Dll1")] public static extern void D3DXDecodeBC3(float4* pColor, byte* pBC);
    [DllImport("Dll1")] public static extern void D3DXDecodeBC4U(float4* pColor, byte* pBC);
    [DllImport("Dll1")] public static extern void D3DXDecodeBC4S(float4* pColor, byte* pBC);
    [DllImport("Dll1")] public static extern void D3DXDecodeBC5U(float4* pColor, byte* pBC);
    [DllImport("Dll1")] public static extern void D3DXDecodeBC5S(float4* pColor, byte* pBC);
    [DllImport("Dll1")] public static extern void D3DXDecodeBC6HU(float4* pColor, byte* pBC);
    [DllImport("Dll1")] public static extern void D3DXDecodeBC6HS(float4* pColor, byte* pBC);
    [DllImport("Dll1")] public static extern void D3DXDecodeBC7(float4* pColor, byte* pBC);

    [DllImport("Dll1")] public static extern void D3DXEncodeBC1(byte* pBC, float4* pColor, float threshold, uint flags);
    // BC1 requires one additional parameter, so it doesn't match signature of BC_ENCODE above

    [DllImport("Dll1")] public static extern void D3DXEncodeBC2(byte* pBC, float4* pColor, uint flags);
    [DllImport("Dll1")] public static extern void D3DXEncodeBC3(byte* pBC, float4* pColor, uint flags);
    [DllImport("Dll1")] public static extern void D3DXEncodeBC4U(byte* pBC, float4* pColor, uint flags);
    [DllImport("Dll1")] public static extern void D3DXEncodeBC4S(byte* pBC, float4* pColor, uint flags);
    [DllImport("Dll1")] public static extern void D3DXEncodeBC5U(byte* pBC, float4* pColor, uint flags);
    [DllImport("Dll1")] public static extern void D3DXEncodeBC5S(byte* pBC, float4* pColor, uint flags);
    [DllImport("Dll1")] public static extern void D3DXEncodeBC6HU(byte* pBC, float4* pColor, uint flags);
    [DllImport("Dll1")] public static extern void D3DXEncodeBC6HS(byte* pBC, float4* pColor, uint flags);
    [DllImport("Dll1")] public static extern void D3DXEncodeBC7(byte* pBC, float4* pColor, uint flags);
}