using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using System.IO;
public unsafe class TransformExporter : MonoBehaviour
{
    [System.Serializable]
    private class TransformData
    {
        public float position_x;
        public float position_y;
        public float position_z;
        public float rotation_x;
        public float rotation_y;
        public float rotation_z;
        public float rotation_w;
        public float localscale_x;
        public float localscale_y;
        public float localscale_z;
    }
    private class TransformJson
    {
        public int parentID;
        public TransformData data;
    }
    public Transform[] outputTarget;
    public string pathName = "TestScene.json";
    [EasyButtons.Button]
    void Get()
    {
        string allTransforms = "{";
        for(int c = 0; c < outputTarget.Length; ++c)
        {
            var i = outputTarget[c];
            TransformJson data = new TransformJson();
            data.data = new TransformData();
            data.parentID = i.parent ? i.parent.GetInstanceID() : 0;
            float3 position = i.position;
            Quaternion rotation = i.rotation;
            float3 localScale = i.localScale;
            UnsafeUtility.MemCpy(data.data.position_x.Ptr(), position.Ptr(), sizeof(float3));
            UnsafeUtility.MemCpy(data.data.rotation_x.Ptr(), rotation.Ptr(), sizeof(Quaternion));
            UnsafeUtility.MemCpy(data.data.localscale_x.Ptr(), localScale.Ptr(), sizeof(float3));
            string s = JsonUtility.ToJson(data);
            allTransforms += '\"' + i.GetInstanceID().ToString() + "\":" + s;
            if(c != outputTarget.Length - 1)
            {
                allTransforms += ',';
            }
        }
        allTransforms += "}";
        using (FileStream fs = new FileStream(pathName, FileMode.Create, FileAccess.Write))
        {
            byte[] b = new byte[allTransforms.Length];
            for(int i = 0; i < allTransforms.Length; ++i)
            {
                b[i] = (byte)allTransforms[i];
            }
            fs.Write(b, 0, b.Length);
        }
    }
}
