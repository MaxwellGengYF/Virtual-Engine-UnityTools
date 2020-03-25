using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
[RequireComponent(typeof(TextureExporter))]
public class TerrainPacker : MonoBehaviour
{
    [System.Serializable]
    public struct Splat
    {
        public Texture2D splat0;
        public Texture2D indexMap;
    }
    [System.Serializable]
    public struct TexturePack
    {
        public Texture2D albedoTex;
        public Texture2D normalTex;
    }
    public Splat[] splatMaps;
    public int splatSize = 1;
    public TexturePack[] texturePackes;
    public string infoFile = "TerrainData.inf";
    [EasyButtons.Button]
    void OutputTerrainData()
    {
        if (splatSize * splatSize != splatMaps.Length)
        {
            Debug.LogError("Splat Size Not Equal To Splat Map!");
            return;
        }
        TextureExporter export = GetComponent<TextureExporter>();
        for (int i = 0; i < texturePackes.Length; ++i)
        {
            export.texture = texturePackes[i].albedoTex;
            export.tex2DFormat = TextureData.LoadFormat.LoadFormat_BC7;
            export.path = texturePackes[i].albedoTex.name + "_Albedo" + i.ToString() + ".vtex";
            export.useMipMap = true;
            export.Print();
            export.texture = texturePackes[i].normalTex;
            export.tex2DFormat = TextureData.LoadFormat.LoadFormat_BC7;
            export.path = texturePackes[i].normalTex.name + "_Normal" + i.ToString() + ".vtex";
            export.useMipMap = true;
            export.Print();
        }
        for (int i = 0; i < splatMaps.Length; ++i)
        {
            //export.texture = splatMaps[i];
            export.tex2DFormat = TextureData.LoadFormat.LoadFormat_BC7;
            //export.path = splatMaps[i].name + "_Splat" + i.ToString() + ".vtex";
            export.useMipMap = true;
            export.texture = splatMaps[i].splat0;
            export.path = splatMaps[i].splat0.name + "_Splat" + i.ToString() + ".vtex";
            export.Print();
            export.texture = splatMaps[i].indexMap;
            export.path = splatMaps[i].indexMap.name + "_Index" + i.ToString() + ".vtex";
            export.Print();
        }

        List<string> splatJsons = new List<string>();
        for (int y = 0, count = 0; y < splatSize; ++y)
            for (int x = 0; x < splatSize; ++x)
            {
                splatJsons.Add(
                    MJsonUtility.GetKeyValue(x.ToString() + ',' + y.ToString(),
                    MJsonUtility.MakeJsonObject(
                        MJsonUtility.GetKeyValue("splat", splatMaps[count].splat0.name + "_Splat" + count.ToString() + ".vtex", false),
                        MJsonUtility.GetKeyValue("index", splatMaps[count].indexMap.name + "_Index" + count.ToString() + ".vtex", false)
                    ), true)
                );
                count++;
            }
        string splatJson = MJsonUtility.MakeJsonObject(splatJsons);
        List<string> albedoJsons = new List<string>();
        for (int i = 0; i < texturePackes.Length; ++i)
        {
            albedoJsons.Add(
                MJsonUtility.GetKeyValue(i.ToString(),
                MJsonUtility.MakeJsonObject(
                    MJsonUtility.GetKeyValue("albedo", texturePackes[i].albedoTex.name + "_Albedo" + i.ToString() + ".vtex", false),
                    MJsonUtility.GetKeyValue("normal", texturePackes[i].normalTex.name + "_Normal" + i.ToString() + ".vtex", false)), true)
                );
        }
        string albedoJson = MJsonUtility.MakeJsonObject(albedoJsons);
        string result = MJsonUtility.MakeJsonObject(
            MJsonUtility.GetKeyValue("splat", splatJson, true),
            MJsonUtility.GetKeyValue("texture", albedoJson, true)
        );

        using (StreamWriter fsm = new StreamWriter(infoFile, false, System.Text.Encoding.ASCII))
        {
            fsm.Write(result);
        }
    }
}
