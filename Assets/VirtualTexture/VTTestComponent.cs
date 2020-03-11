using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VTTestComponent : MonoBehaviour
{
    public Texture tex;
    private RenderTexture vtTex;
    private RenderTexture indirectTex;
    private ComputeBuffer cb;
    private void Start()
    {
        cb = new ComputeBuffer(1, sizeof(int) * 3);
        indirectTex = new RenderTexture(3, 3, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm, 0);
        RenderTextureDescriptor desc = new RenderTextureDescriptor
        {
            autoGenerateMips = false,
            bindMS = false,
            graphicsFormat = tex.graphicsFormat,
            depthBufferBits = 0,
            dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray,
            width = tex.width,
            height = tex.height,
            volumeDepth = 1,
            mipCount = 0,
            msaaSamples = 1
        };
        vtTex = new RenderTexture(desc);
        vtTex.filterMode = FilterMode.Point;
        Graphics.Blit(tex, vtTex, 0, 0);
        int[] arr = new int[] { 0, 0, 0 };
        cb.SetData(arr);
    }

    private void Update()
    {

        Shader.SetGlobalTexture("_VirtualTex", vtTex);
        Shader.SetGlobalBuffer("_IndirectBuffer", cb);
        Shader.SetGlobalTexture("_IndirectTex", indirectTex);
    }
}
