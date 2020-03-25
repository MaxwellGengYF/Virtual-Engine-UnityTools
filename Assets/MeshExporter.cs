using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.IO;
using static Unity.Mathematics.math;
enum MeshDataType
{
    MeshDataType_Vertex = 0,
    MeshDataType_Index = 1,
    MeshDataType_Normal = 2,
    MeshDataType_Tangent = 3,
    MeshDataType_UV = 4,
    MeshDataType_UV2 = 5,
    MeshDataType_UV3 = 6,
    MeshDataType_UV4 = 7,
    MeshDataType_Color = 8,
    MeshDataType_BoneIndex = 9,
    MeshDataType_BoneWeight = 10,
    MeshDataType_BoundingBox = 11,
    MeshDataType_Num = 12
};
struct IndexSettings
{
   public enum IndexFormat
    {
        IndexFormat_16Bit = 0,
        IndexFormat_32Bit = 1
    };
    public IndexFormat indexFormat;
    public uint indexCount;
};

public unsafe class MeshExporter : MonoBehaviour
{
    public List<Mesh> meshes;

    [EasyButtons.Button]
    void Export()
    {
        List<string> str = new List<string>(meshes.Count);
        foreach(var i in meshes)
        {
            str.Add(i.name);
        }
        ExportAllMeshes(meshes, str);
    }
    public void ExportAllMeshes(List<Mesh> meshes, List<string> names)
    {
        List<byte> bytes = new List<byte>(1000);
        void InputInteger(uint inte)
        {
            byte* ptr = (byte*)inte.Ptr();
            bytes.Add(ptr[0]);
            bytes.Add(ptr[1]);
            bytes.Add(ptr[2]);
            bytes.Add(ptr[3]);
        }

        void InputHeader(uint3 header)
        {
            InputInteger(header.x);
            InputInteger(header.y);
            InputInteger(header.z);
        }

        void InputFloat(float flt)
        {
            byte* ptr = (byte*)flt.Ptr();
            bytes.Add(ptr[0]);
            bytes.Add(ptr[1]);
            bytes.Add(ptr[2]);
            bytes.Add(ptr[3]);
        }

        void InputVec2(Vector2 vec)
        {
            InputFloat(vec.x);
            InputFloat(vec.y);
            //InputFloat(vec.z);
        }

        void InputVec3(Vector3 vec)
        {
            InputFloat(vec.x);
            InputFloat(vec.y);
            InputFloat(vec.z);
        }

        void InputVec4(Vector4 vec)
        {
            InputFloat(vec.x);
            InputFloat(vec.y);
            InputFloat(vec.z);
            InputFloat(vec.w);
        }

        List<Vector3> vec3List = new List<Vector3>();
        List<Vector4> vec4List = new List<Vector4>();
        List<Vector2> vec2List = new List<Vector2>();
        void InputVec2List(MeshDataType type)
        {
            uint3 header = 0;
            header.x = (uint)type;
            header.y = (uint)vec2List.Count;
            InputHeader(header);
            foreach (var j in vec2List)
            {
                InputVec2(j);
            }
        }

        void InputVec3List(MeshDataType type)
        {
            uint3 header = 0;
            header.x = (uint)type;
            header.y = (uint)vec3List.Count;
            InputHeader(header);
            foreach (var j in vec3List)
            {
                InputVec3(j);
            }
        }
        void InputVec4List(MeshDataType type)
        {
            uint3 header = 0;
            header.x = (uint)type;
            header.y = (uint)vec4List.Count;
            InputHeader(header);
            foreach (var j in vec4List)
            {
                InputVec4(j);
            }
        }
        for(int c = 0; c < meshes.Count; c++)
        {
            var i = meshes[c];
            uint typeNum = 0;
            bytes.Clear();
            uint3 header = 0;
            BoneWeight[] weight = i.boneWeights;
            if (weight.Length > 0)
            {
                typeNum++;
                header.x = (uint)MeshDataType.MeshDataType_BoneIndex;
                header.y = (uint)weight.Length;
                InputHeader(header);
                foreach (var j in weight)
                {
                    InputInteger((uint)j.boneIndex0);
                    InputInteger((uint)j.boneIndex1);
                    InputInteger((uint)j.boneIndex2);
                    InputInteger((uint)j.boneIndex3);
                }
                typeNum++;
                header.x = (uint)MeshDataType.MeshDataType_BoneWeight;
                InputHeader(header);
                foreach (var j in weight)
                {
                    InputFloat(j.weight0);
                    InputFloat(j.weight1);
                    InputFloat(j.weight2);
                    InputFloat(j.weight3);
                }
            }
            typeNum++;
            header.x = (uint)MeshDataType.MeshDataType_BoundingBox;
            header.y = 0;
            InputHeader(header);
            InputVec3(i.bounds.center);
            InputVec3(i.bounds.extents);
            typeNum++;
            int[] triangles = i.triangles;
            header.x = (uint)MeshDataType.MeshDataType_Index;
            header.y = i.indexFormat == UnityEngine.Rendering.IndexFormat.UInt16 ? (uint)IndexSettings.IndexFormat.IndexFormat_16Bit : (uint)IndexSettings.IndexFormat.IndexFormat_32Bit;
            header.z = (uint)triangles.Length;
            InputHeader(header);
            if (header.y == (uint)IndexSettings.IndexFormat.IndexFormat_32Bit)
            {
                for (int j = 0; j < header.z; ++j)
                {
                    int value = triangles[j];
                    byte* ptr = (byte*)value.Ptr();
                    bytes.Add(ptr[0]);
                    bytes.Add(ptr[1]);
                    bytes.Add(ptr[2]);
                    bytes.Add(ptr[3]);
                }
            }
            else
            {
                for (int j = 0; j < header.z; ++j)
                {
                    ushort value = (ushort)triangles[j];
                    byte* ptr = (byte*)value.Ptr();
                    bytes.Add(ptr[0]);
                    bytes.Add(ptr[1]);
                }
            }
            i.GetVertices(vec3List);
            if(vec3List.Count > 0)
            {
                typeNum++;
                InputVec3List(MeshDataType.MeshDataType_Vertex);
            }
            i.GetNormals(vec3List);
            if (vec3List.Count > 0)
            {
                typeNum++;
                InputVec3List(MeshDataType.MeshDataType_Normal);
            }
            i.GetTangents(vec4List);
            if(vec4List.Count > 0)
            {
                typeNum++;
                InputVec4List(MeshDataType.MeshDataType_Tangent);
            }
            i.GetUVs(0, vec2List);
            if(vec2List.Count > 0)
            {
                typeNum++;
                InputVec2List(MeshDataType.MeshDataType_UV);
            }
            i.GetUVs(1, vec2List);
            if (vec2List.Count > 0)
            {
                typeNum++;
                InputVec2List(MeshDataType.MeshDataType_UV2);
            }
            i.GetUVs(2, vec2List);
            if (vec2List.Count > 0)
            {
                typeNum++;
                InputVec2List(MeshDataType.MeshDataType_UV3);
            }
            i.GetUVs(3, vec2List);
            if (vec2List.Count > 0)
            {
                typeNum++;
                InputVec2List(MeshDataType.MeshDataType_UV4);
            }
            
     
            using (FileStream sw = new FileStream(names[c], FileMode.CreateNew))
            {
                byte[] typeNumByte = new byte[4];
                *(uint*)typeNumByte.Ptr() = typeNum;
                sw.Write(typeNumByte, 0, 4);
                sw.Write(bytes.ToArray(), 0, bytes.Count);
            }
        }

    }
}
