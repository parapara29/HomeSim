using UnityEngine;
using UnityEditor;
using System.IO;

public class ModelToPrefabConverter : MonoBehaviour
{
    private const string modelsFolder = "Assets/Models";
    private const string prefabFolder = "Assets/Resources/Prefabs/Items";

    [MenuItem("Tools/Generate Prefabs from Models")]
    public static void GeneratePrefabs()
    {
        if (!Directory.Exists(prefabFolder))
        {
            Directory.CreateDirectory(prefabFolder);
            AssetDatabase.Refresh();
        }

        string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { modelsFolder });

        foreach (string guid in modelGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (model == null)
            {
                Debug.LogWarning($"Skipping {assetPath}, not a valid GameObject");
                continue;
            }

            string prefabName = Path.GetFileNameWithoutExtension(assetPath);
            string prefabPath = Path.Combine(prefabFolder, prefabName + ".prefab");

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            if (instance == null)
            {
                Debug.LogError($"Failed to instantiate {model.name}");
                continue;
            }

            // Fix materials if needed
            FixMaterialsRecursive(instance);

            // Add BoxCollider to root
            if (instance.GetComponent<Collider>() == null)
            {
                Bounds bounds = CalculateBounds(instance);
                BoxCollider box = instance.AddComponent<BoxCollider>();
                box.center = bounds.center - instance.transform.position;
                box.size = bounds.size;
            }

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath, out bool success);
            DestroyImmediate(instance);

            if (success)
                Debug.Log($"✅ Prefab with BoxCollider created: {prefabPath}");
            else
                Debug.LogError($"❌ Failed to create prefab: {prefabPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✔️ All prefabs generated with BoxColliders!");
    }

    private static void FixMaterialsRecursive(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.sharedMaterials)
            {
                if (mat != null && mat.mainTexture == null)
                {
                    Debug.LogWarning($"Missing texture in material: {mat.name}");
                }
            }
        }
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }
        return bounds;
    }
}
