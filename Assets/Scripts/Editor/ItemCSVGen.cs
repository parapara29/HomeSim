using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class ItemCsvGenerator
{
    // Folder where per-room CSVs live (created if missing)
    private const string CsvFolderRel = "Assets/Resources/Texts";

    [MenuItem("Tools/Add Selected Prefab(s) To Item CSV")]
    public static void AddSelectedPrefabs()
    {
        Object[] selection = Selection.objects;
        if (selection == null || selection.Length == 0)
        {
            Debug.LogError("[ItemCsvGenerator] No selection. Please select one or more prefabs in the Project view.");
            return;
        }

        var prefabs = new List<GameObject>();
        foreach (var obj in selection)
        {
            // Project-view prefabs are GameObjects with a prefab asset type
            var go = obj as GameObject;
            if (go == null) continue;

            var type = PrefabUtility.GetPrefabAssetType(go);
            if (type != PrefabAssetType.Regular &&
                type != PrefabAssetType.Variant &&
                type != PrefabAssetType.Model)
            {
                Debug.LogWarning($"[ItemCsvGenerator] Skipping non-prefab selection: {go.name} (type={type})");
                continue;
            }
            prefabs.Add(go);
        }

        if (prefabs.Count == 0)
        {
            Debug.LogError("[ItemCsvGenerator] No valid prefabs in selection. Select prefab assets in the Project view.");
            return;
        }

        // ── Ask once for item type ────────────────────────────────────────────────
        int option = EditorUtility.DisplayDialogComplex(
            "Item Type (applied to all selected)",
            $"Selected prefabs: {prefabs.Count}\n\nChoose item type for all of them:",
            "Horizontal",
            "Vertical",
            "Cancel");

        if (option == 2) return; // Cancel
        ItemType typeChoice = option == 0 ? ItemType.Horizontal : ItemType.Vertical;

        // ── Ask once for room target ─────────────────────────────────────────────
        int roomOption = EditorUtility.DisplayDialogComplex(
            "Target Room CSV (applied to all selected)",
            "Which room CSV should be updated?",
            "Room (Bedroom)",
            "Kitchen",
            "Bathroom");

        if (roomOption < 0 || roomOption > 2) return;

        string roomFileName;
        switch (roomOption)
        {
            case 0: roomFileName = "item_Room.csv";       break;  // Bedroom
            case 1: roomFileName = "item_Kitchen.csv";    break;
            default: roomFileName = "item_Bathroom.csv";  break;  // <- fixed from LivingRoom
        }

        // ── Ensure folder & CSV header ───────────────────────────────────────────
        string csvFolderAbs = Path.Combine(Application.dataPath, "Resources/Texts");
        if (!Directory.Exists(csvFolderAbs))
            Directory.CreateDirectory(csvFolderAbs);

        string fullPath = Path.Combine(csvFolderAbs, roomFileName);
        if (!File.Exists(fullPath))
        {
            string header = "name;type(h/v);size;is occupied;offset;cost";
            File.WriteAllText(fullPath, header);
            Debug.Log($"[ItemCsvGenerator] Created CSV with header: {fullPath}");
        }

        // ── Process all prefabs ──────────────────────────────────────────────────
        int added = 0;
        foreach (var prefab in prefabs)
        {
            try
            {
                // Instantiate TEMPORARILY to compute bounds in editor
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if (instance == null)
                {
                    Debug.LogWarning($"[ItemCsvGenerator] Could not instantiate prefab: {prefab.name}");
                    continue;
                }

                Bounds b = CalculateBounds(instance);
                Vector3 offset = b.center - instance.transform.position;
                Object.DestroyImmediate(instance);

                Vector3Int size = new Vector3Int(
                    Mathf.RoundToInt(b.size.x),
                    Mathf.RoundToInt(b.size.y),
                    Mathf.RoundToInt(b.size.z));

                string line = string.Format(
                    "{0};{1};{2},{3},{4};1;{5},{6},{7}",
                    prefab.name,
                    typeChoice == ItemType.Horizontal ? "h" : "v",
                    size.x, size.y, size.z,
                    offset.x, offset.y, offset.z
                );

                File.AppendAllText(fullPath, "\n" + line);
                added++;

                Debug.Log($"[ItemCsvGenerator] Added: {line} -> {roomFileName}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ItemCsvGenerator] Error processing '{prefab.name}': {ex.Message}");
            }
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Done",
            $"Updated {roomFileName}\n\nPrefabs processed: {prefabs.Count}\nLines added: {added}",
            "OK");
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);
        return bounds;
    }

    private enum ItemType { Horizontal, Vertical }
}
