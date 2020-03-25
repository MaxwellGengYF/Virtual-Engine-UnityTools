using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Mathematics;
using Random = UnityEngine.Random;
using Unity.Collections;
using VEngine;
public unsafe class Test : MonoBehaviour
{
    [EasyButtons.Button]
    void RunTest()
    {
        Debug.Log(MJsonUtility.JsonObjectSize());
    }
}
