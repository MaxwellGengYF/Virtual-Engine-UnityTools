using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Random = UnityEngine.Random;
public unsafe class Test : MonoBehaviour
{

    [EasyButtons.Button]
    void GetForward()
    {
        float4* color = stackalloc float4[16];
        for (int i = 0; i < 16; ++i)
            color[i] = new float4(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        uint4 value =0 ;
        DXTCompress.D3DXEncodeBC7((byte*)value.Ptr(), color, 0);
        Debug.Log(value);
    }
}
