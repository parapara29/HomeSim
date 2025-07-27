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
    void Awake()
    {
        // cache StudioController that sits on the same GameObject
        studioController = GetComponent<StudioController>();

        // make sure StudioController keeps quiet until we enter a room
        if (studioController != null)
        {
            studioController.initializeOnStart = false;
            studioController.enabled = false;
        }
    }

    /* --------------------------------------------------------------------- */
    void Start ()
    {
        /* ───────── 1  Spawn the house prefab ───────── */
        var prefab = Resources.Load(housePrefabPath) as GameObject;
        if (prefab == null)
        {
            Debug.LogError($"HouseController: could not load prefab “{housePrefabPath}” from a Resources folder.");
            return;
        }
        house = Instantiate(prefab, Vector3.zero, Quaternion.identity);

        /* add click-to-rotate component */
        var dragRotates = house.GetComponents<HouseDragRotate>();
        HouseDragRotate dragRotate = null;
        if (dragRotates.Length > 0)
        {
            dragRotate = dragRotates[0];
            for (int i = 1; i < dragRotates.Length; i++)
                Destroy(dragRotates[i]);
        }
        if (dragRotate == null)
            dragRotate = house.AddComponent<HouseDragRotate>();
        dragRotate.OnClick = ShowRoomPanel;

        /* ───────── 2  Locate UI panels ───────── */
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("HouseController: ‘Canvas’ not found.");
            return;
        }

        housePanel = canvas.transform.Find("HousePanel")?.GetComponent<HousePanel>();
        if (housePanel == null)
        {
            Debug.LogError("HouseController: ‘HousePanel’ not found (even if inactive).");
            return;
        }

        housePanel.Init();      // wires buttons
        housePanel.Hide();      // keep hidden until player clicks the house
        housePanel.OnRoomClick = OpenRoom;

        /* ensure StudioPanel is hidden at start */
        if (studioPanel != null) studioPanel.SetActive(false);
    }

    /* --------------------------------------------------------------------- */
    /*  called by HouseDragRotate when player      *
     *  clicks (quick tap, not drag-rotate)        */
    private void ShowRoomPanel ()
    {
        if (housePanel  != null) housePanel.Show();
        if (studioPanel != null) studioPanel.SetActive(false);
    }

    /*  called by HousePanel when a button pressed */
    private void OpenRoom (string roomName)
{
    if (house != null) Destroy(house);

    /* 1️⃣  show the panel first (now GameObject.Find can see it) */
    if (studioPanel != null) studioPanel.SetActive(true);

    /* 2️⃣  enable the script – Start() will run once here */
    if (studioController != null)
    {
        studioController.enabled = true;      // Start() executes now
        studioController.OpenRoom(roomName);  // then load the room
    }

    if (housePanel != null) housePanel.Hide();
}

}
