using System.Collections.Generic;
using UnityEngine;

public static class RoomPreview
{
    private static Dictionary<string, Sprite> previews = new Dictionary<string, Sprite>();

    public static void Set(string roomName, Texture2D texture)
    {
        if (texture == null) return;
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        if (previews.ContainsKey(roomName))
            previews[roomName] = sprite;
        else
            previews.Add(roomName, sprite);
    }

    public static Sprite Get(string roomName)
    {
        Sprite sprite;
        previews.TryGetValue(roomName, out sprite);
        return sprite;
    }
}