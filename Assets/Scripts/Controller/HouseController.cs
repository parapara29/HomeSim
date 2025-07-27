using UnityEngine;

[DefaultExecutionOrder(-100)]
public class HouseController : MonoBehaviour
{
    [Header("Resources/Prefabs/<folder>/<prefabName>")]
    [SerializeField] private string housePrefabPath = "Prefabs/House/House1";

    private GameObject house;
    private HousePanel   housePanel;
    private GameObject   studioPanel;          // UI object already in your Canvas
    private StudioController studioController;

    /* --------------------------------------------------------------------- */
    private void Awake ()
    {
        studioController = GetComponent<StudioController>();
        if (studioController != null)
        {
            studioController.initializeOnStart = false; // let us decide when to start
            studioController.enabled = false;           // keep it dormant
        }
    }

    /* --------------------------------------------------------------------- */
    private void Start ()
    {
        /* 1. Spawn the house prefab */
        GameObject prefab = Resources.Load<GameObject>(housePrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"HouseController: prefab not found at Resources/{housePrefabPath}");
            return;
        }
        house = Instantiate(prefab, Vector3.zero, Quaternion.identity);

        /* add / reuse HouseDragRotate (only one) */
        HouseDragRotate drag = house.GetComponent<HouseDragRotate>();
        if (drag == null) drag = house.AddComponent<HouseDragRotate>();
        drag.OnClick = ShowRoomPanel;

        /* 2. Locate UI panels */
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("HouseController: Canvas not found in scene.");
            return;
        }

        housePanel  = canvas.transform.Find("HousePanel") ?.GetComponent<HousePanel>();
        studioPanel = canvas.transform.Find("StudioPanel")?.gameObject;

        if (housePanel == null || studioPanel == null)
        {
            Debug.LogError("HouseController: HousePanel or StudioPanel missing in Canvas.");
            return;
        }

        housePanel.Init();
        housePanel.Hide();                     // start hidden
        housePanel.OnRoomClick = OpenRoom;

        studioPanel.SetActive(false);          // hide Studio HUD until inside a room
    }
    /* --------------------------------------------------------------------- */
    /*  called by HouseDragRotate when player      *
     *  clicks (quick tap, not drag-rotate)        */
    private void ShowRoomPanel ()
    {
        housePanel .Show();
        studioPanel.SetActive(false);
    }

    /*  called by HousePanel when a button pressed */
    private void OpenRoom (string prefabName)  // e.g. "Room" or "Bedroom"
    {
        if (house != null) Destroy(house);

        studioPanel.SetActive(true);           // show HUD first

        if (studioController != null && !studioController.enabled)
            studioController.enabled = true;   // Start() runs now

        studioController?.OpenRoom(prefabName); // ← pass raw name ONLY
        housePanel.Hide();
    }
}
