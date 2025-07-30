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

        item.cost = 0; // default price until cost data is provided

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