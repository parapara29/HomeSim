using UnityEngine;
using UnityEditor;
using System.IO;

public class SelectedModelToPrefabConverter : MonoBehaviour
{
    private const string prefabFolder = "Assets/Resources/Prefabs/Items";

    [MenuItem("Tools/Generate Prefab from Selected Model")]
    public static void GeneratePrefabFromSelected()
    {
        // Ensure we have a selected model
        if (Selection.activeObject == null || !(Selection.activeObject is GameObject))
        {
            Debug.LogWarning("Please select a valid GameObject model in the Project view.");
            return;
        }

        GameObject selectedModel = (GameObject)Selection.activeObject;
        string modelPath = AssetDatabase.GetAssetPath(selectedModel);

        // Create the prefab folder if it doesn't exist
        if (!Directory.Exists(prefabFolder))
        {
            Directory.CreateDirectory(prefabFolder);
            AssetDatabase.Refresh();
        }

        string prefabName = Path.GetFileNameWithoutExtension(modelPath);
        string prefabPath = Path.Combine(prefabFolder, prefabName + ".prefab");

        // Instantiate the model and process it
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(selectedModel);
        if (instance == null)
        {
            Debug.LogError($"Failed to instantiate {selectedModel.name}");
            return;
        }

        // Fix materials if needed
        FixMaterialsRecursive(instance);

        // Add BoxCollider to root if it doesn't exist
        if (instance.GetComponent<Collider>() == null)
        {
            Bounds bounds = CalculateBounds(instance);
            BoxCollider box = instance.AddComponent<BoxCollider>();
            box.center = bounds.center - instance.transform.position;
            box.size = bounds.size;
        }

        // Save the prefab
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath, out bool success);
        DestroyImmediate(instance);

        if (success)
            Debug.Log($"✅ Prefab with BoxCollider created: {prefabPath}");
        else
            Debug.LogError($"❌ Failed to create prefab: {prefabPath}");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
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
