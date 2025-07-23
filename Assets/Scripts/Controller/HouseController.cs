using UnityEngine;

[DefaultExecutionOrder(-100)]
public class HouseController : MonoBehaviour
{
    private GameObject house;
    private HousePanel housePanel;
    private StudioController studioController;

    void Awake()
    {
        studioController = GetComponent<StudioController>();
        if (studioController != null)
        {
            studioController.initializeOnStart = false;
        }
    }

    void Start()
    {
        house = Instantiate(Resources.Load("Prefabs/House")) as GameObject;
        var dragRotate = house.AddComponent<HouseDragRotate>();
        dragRotate.OnClick = ShowRoomPanel;

        housePanel = GameObject.Find("/Canvas/HousePanel").GetComponent<HousePanel>();
        housePanel.Init();
        housePanel.Hide();
        housePanel.OnRoomClick = OpenRoom;
    }

    private void ShowRoomPanel()
    {
        if (housePanel != null)
            housePanel.Show();
    }

    private void OpenRoom(string roomName)
    {
        if (house != null)
            Destroy(house);
        if (studioController != null)
        {
            studioController.enabled = true;
            studioController.OpenRoom(roomName);
        }
        if (housePanel != null)
            housePanel.Hide();
    }
}
