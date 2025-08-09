using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sorumi.Util;

public class ItemData : IData<ItemData>
{
    protected override string name { get; set; }

    public ItemData()
    {
        name = "item";
    }

    public static int Count()
    {
        return instance.csvArray.GetLength(0) - 1;
    }

    public static ItemPO GetByRow(int i) // 1 ~ Count
    {
        i = i + 1;
        ItemPO item = new ItemPO();
        item.name = instance.csvArray[i, 0];

        string type = instance.csvArray[i, 1];
        if (type == "h")
            item.type = ItemType.Horizontal;
        else if (type == "v")
            item.type = ItemType.Vertical;

        string[] sizeArray = instance.csvArray[i, 2].Split(',');
        int x = Int32.Parse(sizeArray[0]);
        int y = Int32.Parse(sizeArray[1]);
        int z = Int32.Parse(sizeArray[2]);
        item.size = new Vector3Int(x, y, z);

        item.isOccupied = instance.csvArray[i, 3] == "1";

        string[] offsetArray = instance.csvArray[i, 4].Split(',');
        float ox = float.Parse(offsetArray[0]);
        float oy = float.Parse(offsetArray[1]);
        float oz = float.Parse(offsetArray[2]);
        item.offset = new Vector3(ox, oy, oz);
        int.TryParse(instance.csvArray[i, 5], out item.cost);
        return item;
    }

    public static string GetNameByRow(int i)
    {
        return instance.csvArray[i + 1, 0];
    }

    public static ItemPO[] GetAll()
    {
        ItemPO[] items = new ItemPO[Count()];
        for (int i = 0; i < Count(); i++)
        {
            items[i] = GetByRow(i);
        }
        return items;
    }

    /// <summary>
    /// Loads all ItemPO entries for the specified room. Room names should
    /// correspond to prefab names used when opening a room (e.g. "Room",
    /// "Kitchen", "LivingRoom"). CSV files must be placed in
    /// Assets/Resources/Texts and named using the pattern
    /// "item_{roomName}.csv". If a matching CSV file cannot be found, the
    /// default item.csv will be used as a fallback. The CSV format is the
    /// same as the default: each row should contain name;type;size;is
    /// occupied;offset;cost.
    /// </summary>
    public static ItemPO[] GetAllForRoom(string roomName)
    {
        if (string.IsNullOrEmpty(roomName))
        {
            return GetAll();
        }

        // Determine the resource path for the CSV. Unity Resources lookup is
        // case‐sensitive on certain platforms, so preserve the room name
        // exactly as provided by the caller. If the file does not exist, we
        // fall back to the default item.csv.
        string csvResourcePath = $"Texts/item_{roomName}";
        TextAsset csvAsset = Resources.Load<TextAsset>(csvResourcePath);
        if (csvAsset == null)
        {
            Debug.LogWarning($"ItemData.GetAllForRoom: CSV for '{roomName}' not found at Resources/{csvResourcePath}.csv. Using default item list.");
            return GetAll();
        }

        // Parse the CSV text into a two‐dimensional array. We assume the same
        // delimiter and column structure as the default item.csv file.
        string[,] array = CSVHelper.SplitCsv(csvAsset.text);
        int count = array.GetLength(0) - 1;
        List<ItemPO> items = new List<ItemPO>(count);
        for (int i = 0; i < count; i++)
        {
            int row = i + 1;
            ItemPO item = new ItemPO();
            item.name = array[row, 0];
            string type = array[row, 1];
            item.type = type == "h" ? ItemType.Horizontal : ItemType.Vertical;

            string[] sizeArray = array[row, 2].Split(',');
            int x = Int32.Parse(sizeArray[0]);
            int y = Int32.Parse(sizeArray[1]);
            int z = Int32.Parse(sizeArray[2]);
            item.size = new Vector3Int(x, y, z);

            item.isOccupied = array[row, 3] == "1";

            string[] offsetArray = array[row, 4].Split(',');
            float ox = float.Parse(offsetArray[0]);
            float oy = float.Parse(offsetArray[1]);
            float oz = float.Parse(offsetArray[2]);
            item.offset = new Vector3(ox, oy, oz);

            int costValue;
            Int32.TryParse(array[row, 5], out costValue);
            item.cost = costValue;
            items.Add(item);
        }
        return items.ToArray();
    }

    public static ItemPO GetByName(string itemName)
    {
        for (int i = 0; i < Count(); i++)
        {
            if (instance.csvArray[i + 1, 0] == itemName)
                return GetByRow(i);
        }
        Debug.LogWarning($"ItemData.GetByName: '{itemName}' not found");
        return null;
    }

}