using UnityEngine;
using UnityEditor;
using System.IO;

public static class ItemCsvGenerator
{
    private const string CsvPath = "Assets/Resources/Texts/item.csv";

    [MenuItem("Tools/Add Selected Prefab To Item CSV")]
    public static void AddSelectedPrefab()
    {
        GameObject prefab = Selection.activeObject as GameObject;
        if (prefab == null || PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.NotAPrefab)
        {
            Debug.LogError("Please select a prefab in the Project view.");
            return;
        }

        int option = EditorUtility.DisplayDialogComplex(
            "Item Type",
            "Select the item type for '" + prefab.name + "'",
            "Horizontal",
            "Vertical",
            "Cancel");
        if (option == 2) return; // Cancel

        ItemType type = option == 0 ? ItemType.Horizontal : ItemType.Vertical;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Bounds b = CalculateBounds(instance);
        Object.DestroyImmediate(instance);

        Vector3Int size = new Vector3Int(
            Mathf.RoundToInt(b.size.x),
            Mathf.RoundToInt(b.size.y),
            Mathf.RoundToInt(b.size.z));

        string line = string.Format("{0};{1};{2},{3},{4};1",
            prefab.name,
            type == ItemType.Horizontal ? "h" : "v",
            size.x, size.y, size.z);

        string fullPath = Path.Combine(Application.dataPath, "Resources/Texts/item.csv");
        File.AppendAllText(fullPath, "\n" + line);
        Debug.Log("Added to CSV: " + line);
        AssetDatabase.Refresh();
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);
        return bounds;
    }
}