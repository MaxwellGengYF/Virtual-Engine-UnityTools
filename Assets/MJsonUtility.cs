using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public static unsafe class MJsonUtility
{
    public static string GetKeyValue(string key, string value, bool isValueObject)
    {
        if (isValueObject)
            return '\"' + key + "\":" + value;
        else
            return '\"' + key + "\":\"" + value + '\"';
    }
    public static string GetKeyValue(string key, int value)
    {
        return '\"' + key + "\":" + value.ToString();
    }
    public static string GetKeyValue(string key, double value)
    {
        return '\"' + key + "\":" + value.ToString();
    }
    public static string MakeJsonObject(List<string> jsons)
    {
        if (jsons.Count == 0) return new string(' ', 0);
        string value = "{";
        for (int i = 0; i < jsons.Count - 1; ++i)
        {
            value += jsons[i] + ',';
        }
        value += jsons[jsons.Count - 1] + '}';
        return value;
    }
    public static string MakeJsonObject(params string[] jsons)
    {
        if (jsons.Length == 0) return new string(' ', 0);
        string value = "{";
        for (int i = 0; i < jsons.Length - 1; ++i)
        {
            value += jsons[i] + ',';
        }
        value += jsons[jsons.Length - 1] + '}';
        return value;
    }

    [DllImport("JsonUtility")] public static extern ulong JsonObjectSize();
    [DllImport("JsonUtility")] public static extern void CreateJsonObject(char* filePath, ulong pathLen, void* targetPtr);
    [DllImport("JsonUtility")] public static extern void DisposeJsonObject(void* jsonObj);
    [DllImport("JsonUtility")] public static extern void OutputJsonObject(char* str, ulong strCount, void* jsonObj);
    [DllImport("JsonUtility")] public static extern void UpdateKeyValue(void* cjson, char* key, ulong keyLength, char* value, ulong valueLength);
    [DllImport("JsonUtility")] public static extern void DeleteUnusedPath(void* cjsonPtr);
}
