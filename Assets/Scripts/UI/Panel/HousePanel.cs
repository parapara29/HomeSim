using System;
using UnityEngine;
using UnityEngine.UI;

public class HousePanel : MonoBehaviour
{
    private Button bedroomButton;

    public Action<string> OnRoomClick;

    public void Init()
    {
        bedroomButton = transform.Find("BedroomButton").GetComponent<Button>();
        bedroomButton.onClick.AddListener(() =>
        {
            if (OnRoomClick != null)
                OnRoomClick("Bedroom");
        });
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
