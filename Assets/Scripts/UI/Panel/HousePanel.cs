using System;
using UnityEngine;
using UnityEngine.UI;

public class HousePanel : MonoBehaviour
{
    private Button bedroomButton;

    public Action<string> OnRoomClick;

    public void Init ()
    {
        string[] rooms = { "Bedroom"};   // add more here

        foreach (string room in rooms)
        {
            Transform t = transform.Find(room + "Button");
            if (t == null)
            {
                Debug.LogError($"HousePanel: '{room}Button' not found under {name}.");
                continue;
            }

            Button btn = t.GetComponent<Button>();
            btn.onClick.AddListener(() => OnRoomClick?.Invoke(room));
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