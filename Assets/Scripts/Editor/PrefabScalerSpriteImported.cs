using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class PrefabScalerAndSpriteImporter : MonoBehaviour
{
    private const string prefabFolder = "Assets/Resources/Prefabs/Items";
    private const string spriteTargetFolder = "Assets/Resources/Images/Items";
    private const string sourceImageFolder = @"C:\Freelancing\kenney_furniture-kit\Isometric";

    [MenuItem("Tools/Resize Prefabs and Import _SW Sprites")]
    public static void ProcessPrefabsAndSprites()
    {
        ResizePrefabs();
        ImportAndResizeSprites();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅ All done: prefabs scaled & sprites imported.");
    }

    private static void ResizePrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolder });

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            instance.transform.localScale = Vector3.one * 0.5f;

            PrefabUtility.SaveAsPrefabAsset(instance, path);
            GameObject.DestroyImmediate(instance);

            Debug.Log($"🔧 Resized: {path}");
        }
    }

    private static void ImportAndResizeSprites()
{
    if (!Directory.Exists(spriteTargetFolder))
        Directory.CreateDirectory(spriteTargetFolder);

    string[] files = Directory.GetFiles(sourceImageFolder, "*_SW.*")
                              .Where(f => f.EndsWith(".png") || f.EndsWith(".jpg"))
                              .ToArray();

    foreach (string sourcePath in files)
    {
        string originalName = Path.GetFileNameWithoutExtension(sourcePath); // e.g., bedDouble_SW
        string newName = originalName.Replace("_SW", "_512") + ".png";
        string destPath = Path.Combine(spriteTargetFolder, newName);

        // Load original image
        byte[] imageData = File.ReadAllBytes(sourcePath);
        Texture2D originalTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        originalTex.LoadImage(imageData);

        // Resize to 512x512 using RenderTexture + ReadPixels
        Texture2D resizedTex = ResizeTexture(originalTex, 512, 512);

        // Save resized as PNG
        byte[] resizedData = resizedTex.EncodeToPNG();
        File.WriteAllBytes(destPath, resizedData);
        Debug.Log($"🖼️ Resized and saved: {newName}");
    }

    AssetDatabase.Refresh();

    // Mark all resized textures as single-mode sprites
    string[] importedAssets = AssetDatabase.FindAssets("t:Texture2D", new[] { spriteTargetFolder })
                                           .Select(AssetDatabase.GUIDToAssetPath)
                                           .ToArray();

    foreach (string assetPath in importedAssets)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            Debug.Log($"🎯 Imported sprite: {assetPath}");
        }
    }
}

private static Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
{
    // Decide if we actually need to scale
    bool needsDownscale = source.width  > targetWidth ||
                          source.height > targetHeight;

    Texture2D scaledTex;

    if (needsDownscale)
    {
        /* ---------- 1) Down-scale to fit inside 512×512 ---------- */
        float sourceRatio  = (float)source.width / source.height;
        float targetRatio  = (float)targetWidth / targetHeight;

        int newW, newH;
        if (sourceRatio > targetRatio)
        {
            newW = targetWidth;
            newH = Mathf.RoundToInt(targetWidth / sourceRatio);
        }
        else
        {
            newH = targetHeight;
            newW = Mathf.RoundToInt(targetHeight * sourceRatio);
        }

        RenderTexture rt = RenderTexture.GetTemporary(newW, newH);
        Graphics.Blit(source, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        scaledTex = new Texture2D(newW, newH, TextureFormat.RGBA32, false);
        scaledTex.ReadPixels(new Rect(0, 0, newW, newH), 0, 0);
        scaledTex.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
    }
    else
    {
        /* ---------- 2) Keep original pixels (no scaling) ---------- */
        scaledTex = source;
    }

    /* ---------- 3) Pad into a full 512×512 transparent canvas ---------- */
    Texture2D finalTex = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
    // Fill with transparent pixels
    var clear = new Color32(0, 0, 0, 0);
    Color32[] clearPixels = Enumerable.Repeat(clear, targetWidth * targetHeight).ToArray();
    finalTex.SetPixels32(clearPixels);

    int offX = (targetWidth  - scaledTex.width)  / 2;
    int offY = (targetHeight - scaledTex.height) / 2;
    finalTex.SetPixels(offX, offY, scaledTex.width, scaledTex.height, scaledTex.GetPixels());
    finalTex.Apply();

    return finalTex;
}


}
