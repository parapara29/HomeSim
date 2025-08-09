using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

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
    private static readonly HashSet<string> registeredRooms = new HashSet<string>();
    private const string RoomListKey = "RoomList";

    public static void RegisterRoom(string roomName)
    {
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogError("RoomSave.Save: roomName is null or empty");
            return;
        }

        List<string> rooms = new List<string>();
        if (PlayerPrefs.HasKey(RoomListKey))
        {
            string existing = PlayerPrefs.GetString(RoomListKey);
            rooms = new List<string>(existing.Split(','));
        }

        if (!rooms.Contains(roomName))
        {
            rooms.Add(roomName);
            string updated = string.Join(",", rooms);
            PlayerPrefs.SetString(RoomListKey, updated);
            PlayerPrefs.Save();
        }
    }

    public static void Save(string roomName, List<ItemSaveData> items)
    {
        if (string.IsNullOrEmpty(roomName)) return;

        RoomSaveData data = new RoomSaveData();
        data.items = items;
        string json = JsonUtility.ToJson(data);
        Debug.Log($"RoomSave.Save: Saving room '{roomName}' with JSON length {json?.Length ?? 0}");
        PlayerPrefs.SetString(SaveKeyPrefix + roomName, json);

        PlayerPrefs.Save();
    }

    public static List<ItemSaveData> Load(string roomName)
    {
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogError("RoomSave.Load: roomName is null or empty");
            return new List<ItemSaveData>();
        }

        string key = SaveKeyPrefix + roomName;
        if (!PlayerPrefs.HasKey(key))
        {
            Debug.LogWarning($"RoomSave.Load: No save data for room '{roomName}'");
            return new List<ItemSaveData>();
        }

        string json = PlayerPrefs.GetString(key);
        Debug.Log($"RoomSave.Load: Loading room '{roomName}' with JSON length {json?.Length ?? 0}");

        RoomSaveData data = null;
        try
        {
            data = JsonUtility.FromJson<RoomSaveData>(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"RoomSave.Load: Failed to deserialize room '{roomName}': {ex}");
        }

        if (data == null || data.items == null)
        {
            Debug.LogError($"RoomSave.Load: Deserialized data is null for room '{roomName}'");
            return new List<ItemSaveData>();
        }
        return data.items;
    }
    
}