using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemSaveData
{
    public string prefab;
    public Vector3 position;
    public int direction;
}

[System.Serializable]
public class RoomSaveData
{
    public List<ItemSaveData> items = new List<ItemSaveData>();
}

public static class RoomSave
{
    private const string SaveKey = "RoomState";

    public static void Save(List<ItemSaveData> items)
    {
        RoomSaveData data = new RoomSaveData();
        data.items = items;
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    public static List<ItemSaveData> Load()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
            return new List<ItemSaveData>();
        string json = PlayerPrefs.GetString(SaveKey);
        RoomSaveData data = JsonUtility.FromJson<RoomSaveData>(json);
        if (data == null || data.items == null) return new List<ItemSaveData>();
        return data.items;
    }
}