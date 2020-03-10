using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.IO;
using UnityEditor;
public class MaterialExporter : MonoBehaviour
{
    public struct StandardPBRMaterial
    {
        public float2 uvScale;
        public float2 uvOffset;
        //align
        public float3 albedo;
        public float metallic;
        public float3 emission;
        public float smoothness;
        //align
        public float occlusion;
    };
    public string MaterialExport(Material mat)
    {
        string str = "{";
        Vector4 tileOffset = mat.GetVector("_TileOffset");
        Vector4 albedo = mat.GetVector("_Color");
        float metallic = mat.GetFloat("_MetallicIntensity");
        Vector4 emissionColor = mat.GetVector("_EmissionColor");
        float emissionIntensity = mat.GetFloat("_EmissionMultiplier");
        float smoothness = mat.GetFloat("_Glossiness");
        float occlusion = mat.GetFloat("_Occlusion");
        Texture albedoTex = mat.GetTexture("_MainTex");
        Texture normalTex = mat.GetTexture("_BumpMap");
        Texture specTex = mat.GetTexture("_SpecularMap");
        //Texture heightTex = mat.GetTexture("_HeightMap");
        Texture emissionTex = mat.GetTexture("_EmissionMap");
        emissionColor *= emissionIntensity;
        str += "\"uvScale\":" + '\"' + tileOffset.x + ',' + tileOffset.y + "\",";
        str += "\"uvOffset\":" + '\"' + tileOffset.z + ',' + tileOffset.w + "\",";
        str += "\"albedo\":" + '\"' + albedo.x + ',' + albedo.y + ',' + albedo.z + "\",";
        str += "\"metallic\":" + '\"' + metallic + "\",";
        str += "\"emission\":" + '\"' + emissionColor.x + ',' + emissionColor.y + ',' + emissionColor.z + "\",";
        str += "\"smoothness\":" + '\"' + smoothness + "\",";
        str += "\"occlusion\":" + '\"' + occlusion + "\",";
        str += "\"albedoTexIndex\":" + '\"' + (albedoTex ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(albedoTex)) : "0") + "\",";
        str += "\"specularTexIndex\":" + '\"' + (specTex ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(specTex)) : "0") + "\",";
        str += "\"normalTexIndex\":" + '\"' + (normalTex ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(normalTex)) : "0") + "\",";
        str += "\"emissionTexIndex\":" + '\"' + (emissionTex ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(emissionTex)) : "0") + "\"";
        str += "}";
        return str;
    }
    public Material testMat;
    [EasyButtons.Button]
    void Test()
    {
        string s = MaterialExport(testMat);
        using (FileStream fs = new FileStream("Test.mat", FileMode.Create))
        {
            byte[] b = new byte[s.Length];
            for(uint i = 0; i < b.Length; ++i)
            {
                b[i] = (byte)s[(int)i];
            }
            fs.Write(b, 0, b.Length);
        }
    }
}
