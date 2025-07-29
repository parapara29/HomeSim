using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class SpriteUpscaler512 : MonoBehaviour
{
    private const string spriteFolder = "Assets/Resources/Images/Items";

    [MenuItem("Tools/Upscale Sprites to 512")]
    public static void UpscaleSprites()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { spriteFolder });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D src   = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

            if (src == null)
                continue;

            // Skip if one side already ≥ 512 (no up-scale needed)
            if (Mathf.Max(src.width, src.height) >= 512)
                continue;

            // ------- 1. Compute new size (aspect-preserving) -------
            float scaleFactor   = 512f / Mathf.Max(src.width, src.height);
            int   newW          = Mathf.RoundToInt(src.width  * scaleFactor);
            int   newH          = Mathf.RoundToInt(src.height * scaleFactor);

            // ------- 2. Upscale using RenderTexture + bilinear -------
            RenderTexture rt = RenderTexture.GetTemporary(newW, newH);
            Graphics.Blit(src, rt);                      // bilinear filtering
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active   = rt;

            Texture2D upscaled = new Texture2D(newW, newH, TextureFormat.RGBA32, false);
            upscaled.ReadPixels(new Rect(0, 0, newW, newH), 0, 0);
            upscaled.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            // ------- 3. Encode back to PNG & overwrite file -------
            byte[] png = upscaled.EncodeToPNG();         // loss-less
            File.WriteAllBytes(assetPath, png);

            // ------- 4. Re-import with the same sprite settings ----
            TextureImporter imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (imp != null)
            {
                imp.textureType      = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled    = false;
                imp.alphaIsTransparency = true;
                imp.filterMode       = FilterMode.Bilinear;
                imp.SaveAndReimport();
            }

            Debug.Log($"🆙  Upscaled  {Path.GetFileName(assetPath)}  →  {newW}×{newH}");
        }

        AssetDatabase.Refresh();
        Debug.Log("✅  Upscaling pass complete.");
    }
}
