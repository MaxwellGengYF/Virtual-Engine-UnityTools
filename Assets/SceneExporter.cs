using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using System;
[RequireComponent(typeof(TextureExporter))]
[RequireComponent(typeof(MaterialExporter))]
[RequireComponent(typeof(MeshExporter))]
public unsafe class SceneExporter : MonoBehaviour
{

    static void GetSceneData(Scene targetScene, Dictionary<Mesh, bool> meshDict, Dictionary<Material, bool> matDict, Dictionary<Texture2D, bool> texDict)
    {
        void RunLogic(GameObject i)
        {
            MeshFilter filter = null;
            MeshRenderer renderer = null;
            if ((filter = i.GetComponent<MeshFilter>()) && filter.sharedMesh != null)
            {
                meshDict[filter.sharedMesh] = true;
            }
            if (renderer = i.GetComponent<MeshRenderer>())
            {
                Material mat = renderer.sharedMaterial;
                if (mat)
                {
                    matDict[mat] = true;
                    Texture2D tex = null;
                    string[] names =
                    {
                        "_MainTex",
                        "_BumpMap",
                        "_SpecularMap",
                        "_HeightMap"
                    };
                    foreach (var str in names)
                    {
                        if (tex = mat.GetTexture(str) as Texture2D)
                        {
                            texDict[tex] = true;
                        }
                    }
                }
            }

        }
        void IterateObject(Transform tr)
        {
            RunLogic(tr.gameObject);
            for (int i = 0; i < tr.childCount; ++i)
            {
                IterateObject(tr.GetChild(i));
            }
        }
        GameObject[] rootGOs = targetScene.GetRootGameObjects();
        foreach (var i in rootGOs)
        {
            IterateObject(i.transform);
        }
    }
    public string folder = "Resource/";
    [EasyButtons.Button]
    void OutputWholeScene()
    {
        MaterialExporter matEx = GetComponent<MaterialExporter>();
        MeshExporter meshEx = GetComponent<MeshExporter>();
        TextureExporter texEx = GetComponent<TextureExporter>();
        Dictionary<Material, bool> matDict = new Dictionary<Material, bool>();
        Dictionary<Mesh, bool> meshDict = new Dictionary<Mesh, bool>();
        Dictionary<Texture2D, bool> texDict = new Dictionary<Texture2D, bool>();
        GetSceneData(
            SceneManager.GetActiveScene(),
            meshDict, matDict, texDict);
        void* jsonObjPtr = UnsafeUtility.Malloc((long)MJsonUtility.JsonObjectSize(), 16, Unity.Collections.Allocator.Temp);
        string jsonPath = folder + "AssetDatabase.json";
        fixed (char* c = jsonPath)
        {
            MJsonUtility.CreateJsonObject(c, (ulong)jsonPath.Length, jsonObjPtr);
        }
        List<Mesh> meshes = new List<Mesh>(meshDict.Count);
        List<string> meshNames = new List<string>(meshDict.Count);
        foreach (var i in meshDict)
        {
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(i.Key));
            string name = folder + i.Key.name + ".vmesh";
            meshes.Add(i.Key);
            meshNames.Add(name);
            MJsonUtility.UpdateKeyValue(jsonObjPtr, guid.Ptr(), (ulong)guid.Length, name.Ptr(), (ulong)name.Length);

        }
        foreach(var i in matDict)
        {
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(i.Key));
            string name = folder + i.Key.name + ".mat";
            MJsonUtility.UpdateKeyValue(jsonObjPtr, guid.Ptr(), (ulong)guid.Length, name.Ptr(), (ulong)name.Length);
            matEx.testMat = i.Key;
            matEx.PrintToFile(name);
        }
        
        foreach (var i in texDict)
        {
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(i.Key));
            string name = folder + i.Key.name + ".vtex";
            MJsonUtility.UpdateKeyValue(jsonObjPtr, guid.Ptr(), (ulong)guid.Length, name.Ptr(), (ulong)name.Length);
            texEx.texture = i.Key;
            texEx.path = name;
            texEx.useMipMap = true;
            texEx.tex2DFormat = TextureData.LoadFormat.LoadFormat_BC7;
            texEx.Print();
        }
        meshEx.ExportAllMeshes(meshes, meshNames);
        MJsonUtility.OutputJsonObject(jsonPath.Ptr(), (ulong)jsonPath.Length, jsonObjPtr);
        MJsonUtility.DisposeJsonObject(jsonObjPtr);
    }
}
