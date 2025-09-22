using System.Xml;
using UnityEngine;
using static UnityEditor.VersionControl.Asset;

[ExecuteAlways]

public class BoardTextureGenerator : MonoBehaviour


{
    


    [Header("Hook these up")]
    public Renderer boardRenderer;

    [Header("Board")]
    [Range(2, 32)] public int squares = 8;
    [Range(8, 256)] public int pixelsPerSquare = 64;

    [Header("Colors")]
    public Color lightSquare = new(0.85f, 0.85f, 0.85f);
    public Color darkSquare = new(0.25f, 0.25f, 0.25f);

    Texture2D tex;

    [ContextMenu("build Board Texture")]
    public void Build()
    {
        if (!boardRenderer) return;

        int W = squares * pixelsPerSquare;
        int H = squares * pixelsPerSquare;

        if (tex == null || tex.width != W || tex.height != H)
            tex = new Texture2D(W, H, TextureFormat.RGBA32, false);

        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int cx = x / pixelsPerSquare;
                int cy = y / pixelsPerSquare;
                bool dark = ((cx + cy) & 1) == 0;

                Color c = dark ? darkSquare : lightSquare;

                tex.SetPixel(x, y, c);
            }


        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply(false);

        var mat = boardRenderer.sharedMaterial;
        if (!mat) { Debug.LogWarning("Assign a material to the boardRenderer."); return; }

        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
    }

    void OnValidate() { if (isActiveAndEnabled) Build(); }



}
