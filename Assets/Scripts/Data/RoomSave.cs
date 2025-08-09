using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemSaveData
{
    public string prefab;
    public Vector3 position;
    public int direction;
    // The placement type of the item (0=None,1=Wall,2=Floor,3=Item). This
    // is stored so that items placed on other objects can be correctly
    // restored without occupying floor space. Older save files will not
    // contain this field and will default to 0 (None).
    public int placeType;
}

[System.Serializable]
public class RoomSaveData
{
    public List<ItemSaveData> items = new List<ItemSaveData>();
}

public static class RoomSave
{
    private const string SaveKeyPrefix = "RoomState_";

    public static void Save(string roomName, List<ItemSaveData> items)
    {
        if (string.IsNullOrEmpty(roomName)) return;

        RoomSaveData data = new RoomSaveData();
        data.items = items;
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SaveKeyPrefix + roomName, json);
        PlayerPrefs.Save();
    }

    public static List<ItemSaveData> Load(string roomName)
    {
        if (string.IsNullOrEmpty(roomName))
            return new List<ItemSaveData>();

        string key = SaveKeyPrefix + roomName;
        if (!PlayerPrefs.HasKey(key))
            return new List<ItemSaveData>();

        string json = PlayerPrefs.GetString(key);
        RoomSaveData data = JsonUtility.FromJson<RoomSaveData>(json);
        if (data == null || data.items == null) return new List<ItemSaveData>();
        return data.items;
    }
}