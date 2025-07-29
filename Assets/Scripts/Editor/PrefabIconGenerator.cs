using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class PrefabIconGenerator : MonoBehaviour
{
    private const string prefabFolder = "Assets/Resources/Prefabs/Items";
    private const string outputFolder = "Assets/Resources/Images/Items";
    private const int    iconSize     = 512;

    [MenuItem("Tools/Generate Isometric Prefab Icons")]
    public static void GenerateIcons()
    {
        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);

        string[] prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolder });
        if (prefabs.Length == 0) { Debug.LogWarning("No prefabs found."); return; }

        /* ---------- temporary scene objects ---------- */
        GameObject root   = new GameObject("~IconGen_Temp");
        Camera cam        = new GameObject("IconCamera").AddComponent<Camera>();
        cam.transform.SetParent(root.transform);

        cam.orthographic    = true;
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0);              // transparent
        cam.cullingMask     = ~0;
        cam.allowHDR        = false;
        cam.allowMSAA       = false;

        // soft key light (isometric direction)
        Light key = new GameObject("KeyLight").AddComponent<Light>();
        key.transform.SetParent(root.transform);
        key.type      = LightType.Directional;
        key.color     = Color.white;
        key.intensity = 0.9f;
        key.transform.rotation = Quaternion.Euler(50, -35, 0);    // matches iso view

        // store & override ambient so it’s not over-bright
        var prevAmbMode  = RenderSettings.ambientMode;
        var prevAmbColor = RenderSettings.ambientLight;
        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.25f, 0.25f, 0.25f);

        RenderTexture rt = new RenderTexture(iconSize, iconSize, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing  = 1;

        foreach (string guid in prefabs)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string fileName  = Path.GetFileNameWithoutExtension(assetPath);
            string outPath   = Path.Combine(outputFolder, fileName + "_512.png");

            GameObject prefab   = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            instance.transform.position   = Vector3.zero;
            instance.transform.rotation   = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            /* ---------- framing ---------- */
            Bounds b = CalculateBounds(instance);
            float  maxSide = Mathf.Max(b.size.x, b.size.y, b.size.z);

            Vector3 isoDir = new Vector3(-1, 1.3f, -1).normalized;
            cam.transform.position = b.center + isoDir * (maxSide * 2f);
            cam.transform.LookAt(b.center);
            cam.orthographicSize  = maxSide * 0.75f;

            // align key-light with camera so highlights match view direction
            key.transform.rotation = cam.transform.rotation;

            /* ---------- render ---------- */
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D tex = new Texture2D(iconSize, iconSize, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, iconSize, iconSize), 0, 0);
            tex.Apply();

            RenderTexture.active = prev;
            cam.targetTexture    = null;

            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Debug.Log($"📸  Saved icon: {outPath}");

            DestroyImmediate(instance);
        }

        /* ---------- cleanup ---------- */
        RenderSettings.ambientMode  = prevAmbMode;
        RenderSettings.ambientLight = prevAmbColor;
        DestroyImmediate(root);
        rt.Release();

        AssetDatabase.Refresh();
        Debug.Log("✅  Icons regenerated with soft shading.");
    }

    private static Bounds CalculateBounds(GameObject go)
    {
        var rends = go.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.1f);

        Bounds b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);
        return b;
    }
}
