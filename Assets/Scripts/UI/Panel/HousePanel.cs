using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct RoomEntry
{
    public string buttonName;    // name of the UI button (without the "Button" suffix)
    public string prefabName;    // prefab to load when clicked
}

public class HousePanel : MonoBehaviour
{
    public RoomEntry[] rooms;    // map UI buttons to prefab names

    public Action<string> OnRoomClick;

    public Action OnBackClick
    {
        set { backButton.onClick.AddListener(() => value()); }
    }

    private Button backButton;

    
    public void Init()
    {
        rooms = new[]
        {
            new RoomEntry { buttonName = "Bedroom", prefabName = "Room" }
        };
        backButton = transform.Find("BackButton").GetComponent<Button>();
        foreach (var entry in rooms)
        {
            Transform t = transform.Find(entry.buttonName + "Button");
            if (t == null)
            {
                Debug.LogError($"HousePanel: '{entry.buttonName}Button' not found under {name}.");
                continue;
            }

            Button btn = t.GetComponent<Button>();
            string prefab = entry.prefabName;
            btn.onClick.AddListener(() => OnRoomClick?.Invoke(prefab));

            Image img = t.GetComponent<Image>();
            Sprite preview = RoomPreview.Get(prefab);
            if (img != null && preview != null)
                img.sprite = preview;
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}