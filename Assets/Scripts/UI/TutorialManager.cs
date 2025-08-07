// TutorialManager.cs – tag-driven landmarks, HUD explanations, and zoom-in camera pans
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    /* ───────────────────────────────────── Singleton ───────────────────────────────────── */
    public static TutorialManager Instance { get; private set; }
    // Controls whether external panels (work/food/furniture) can be closed by clicking outside.
    // When set to false, UI scripts should not destroy their panels until the tutorial
    // explicitly allows it. Defaults to true when no tutorial is running.
    public static bool PanelCloseAllowed { get; set; } = true;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /* ─────────────────────────────── Serialized Fields ─────────────────────────────── */
    [SerializeField] private MonoBehaviour playerController;

    [Header("Landmark Tags")]
    [SerializeField] private string houseTag = "House";
    [SerializeField] private string workTag  = "Work";
    [SerializeField] private string foodTag  = "Food";

    [Header("Camera")]
    [SerializeField] private Camera tutorialCamera;       // optional override; MainCamera if null
    [SerializeField] private float cameraMoveTime = 2f;   // seconds per pan
    [SerializeField] private float zoomInDistance = 5f;   // extra metres to move forward
    [SerializeField] private float zoomInTime = 0.6f;     // seconds for zoom-in

    [Header("Isometric Settings")]
    [SerializeField] private float isoPitch    = 30f;
    [SerializeField] private float isoYaw      = 45f;
    [SerializeField] private float isoDistance = 30f;

    [Header("Tutorial UI")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private Text tutorialText;

    /* ─────────────────────────────── Runtime Helpers ─────────────────────────────── */
    readonly List<GameObject> overlayObjects     = new();
    readonly List<Behaviour>  disabledBehaviours = new();

    Transform houseTarget, workTarget, foodTarget;

    /* ─────────────────────────────── Unity Hooks ─────────────────────────────── */
    void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    void Start()    => RebindLandmarks();
    void OnSceneLoaded(Scene sc, LoadSceneMode mode) => RebindLandmarks();

    /* ─────────────────────────────── Public Entry ─────────────────────────────── */
    public void ShowTutorial()
    {
        gameObject.SetActive(true);
        StartCoroutine(RunTutorial());
    }

    /* ─────────────────────────────── Main Coroutine ─────────────────────────────── */
    IEnumerator RunTutorial()
    {
        RebindLandmarks();                    // ensure tags resolved
        string citySceneName = SceneManager.GetActiveScene().name;
        if (playerController) playerController.enabled = false;

        Camera cam = PrepareCamera();
        if (!cam)
        {
            Debug.LogWarning("TutorialManager: No active camera found");
            yield break;
        }

        /* Tour */
        yield return PanAndExplain(houseTarget, "This is your house.");
        yield return WaitForBuildingClick();
        yield return ExplainHousePanel();
        yield return WaitForScene("Start");
        yield return ExplainStartScene();
        yield return WaitForScene(citySceneName);
        yield return PanAndExplain(workTarget, "This is where you work.");
        yield return ExplainWorkPanel();
        yield return PanAndExplain(foodTarget, "Here you can get food.");
        // Keep the food menu open once it appears until explicitly allowed to close.
        ShowDialogue("Click the food building to open the food menu.");
        // disallow closing the food panel until we instruct otherwise
        PanelCloseAllowed = false;
        yield return WaitForPanel("FoodPanel", true);
        HideDialogue();

        yield return ExplainButton(
            "RiceButton",
            "Rice costs 100 coins and restores a good amount of hunger."
        );
        yield return ExplainButton(
            "BurgerButton",
            "Burgers are cheaper at 40 coins but only restore a little hunger."
        );

        // allow the player to close the food menu now
        ShowDialogue("Close the food menu to continue.");
        PanelCloseAllowed = true;
        yield return WaitForPanel("FoodPanel", false);
        HideDialogue();

        /* HUD highlights */
        yield return ShowHudHighlights();

        /* Final message */
        ShowDialogue("That is all for the tutorial! Have fun!");
        yield return WaitForClick();
        HideDialogue();

        EndTutorial();
    }

    /* ─────────────────────────────── Pan helpers ─────────────────────────────── */
    IEnumerator PanAndExplain(Transform target, string message)
    {
        if (target)
        {
            Debug.Log(
                $"[Tutorial] About to pan:\n" +
                $" • target = \"{target.name}\" (scene = \"{target.gameObject.scene.name}\")\n" +
                $" • hierarchy = {PathInHierarchy(target)}\n" +
                $" • local   = {target.localPosition}\n" +
                $" • world   = {target.position}"
            );
        }

        yield return PanToTarget(target);
        ShowDialogue(message);
        yield return WaitForClick();
        HideDialogue();
    }

    IEnumerator PanToTarget(Transform target)
    {
        if (!target) yield break;

        /* compute desired iso pose once */
        Quaternion isoRot = Quaternion.Euler(isoPitch, isoYaw, 0f);
        Vector3 desiredPos = target.position + isoRot * Vector3.back * isoDistance;
        Quaternion desiredRot = Quaternion.LookRotation(target.position - desiredPos, Vector3.up);

        Debug.Log($"[Tutorial] Target \"{target.name}\" @ {target.position} ⇒ desired camera pos {desiredPos}, rot {desiredRot.eulerAngles}");

        float elapsed = 0f;
        Camera activeCam = PrepareCamera();
        if (!activeCam) yield break;

        Vector3 startPos = activeCam.transform.position;
        Quaternion startRot = activeCam.transform.rotation;
        Debug.Log($"[Tutorial] Panning from {startPos} to {desiredPos}");

        while (elapsed < cameraMoveTime)
        {
            /* handle camera destruction or replacement */
            if (activeCam == null)
            {
                activeCam = PrepareCamera();
                if (!activeCam) yield break;
                startPos = activeCam.transform.position;
                startRot = activeCam.transform.rotation;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / cameraMoveTime);

            Vector3 newPos = Vector3.Lerp(startPos, desiredPos, t);
            Quaternion newRot = Quaternion.Slerp(startRot, desiredRot, t);
            activeCam.transform.SetPositionAndRotation(newPos, newRot);

            yield return null;
        }

        if (activeCam)
        {
            activeCam.transform.SetPositionAndRotation(desiredPos, desiredRot);
            yield return ZoomIn(activeCam, desiredRot);
        }
    }

    /* small forward zoom after arriving */
    IEnumerator ZoomIn(Camera cam, Quaternion lookRot)
    {
        if (!cam || zoomInDistance <= 0f || zoomInTime <= 0f) yield break;

        Vector3 startPos = cam.transform.position;
        Vector3 endPos   = startPos + cam.transform.forward * zoomInDistance;

        float elapsed = 0f;
        while (elapsed < zoomInTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / zoomInTime);
            cam.transform.position = Vector3.Lerp(startPos, endPos, t);
            cam.transform.rotation = lookRot;       // keep rotation locked
            yield return null;
        }
        cam.transform.position = endPos;
    }
    IEnumerator WaitForBuildingClick()
    {
        ShowDialogue("Click the house to open its panel.");
        yield return new WaitUntil(() => GameObject.Find("RestPanel") != null);
        HideDialogue();
    }

    IEnumerator ExplainHousePanel()
    {
        var panel = GameObject.Find("RestPanel");
        if (!panel) yield break;

        var root = panel.transform.Find("Panel") ?? panel.transform;
        var editButton = root.Find("EditButton");
        var restButton = root.Find("RestButton");

        if (restButton)
            yield return HighlightWithDialogue(restButton, "Rest here to recover your energy.");
        if (editButton)
            yield return HighlightWithDialogue(editButton, "Edit furniture inside your house. Click on it to go to House View");
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "Start");


        ShowDialogue("Close the panel to continue.");
        yield return new WaitUntil(() => GameObject.Find("RestPanel") == null);
        HideDialogue();
    }

    IEnumerator ExplainStartScene()
    {
        ShowDialogue("This is your house, click on it to get access to your rooms.");
        yield return new WaitUntil(() => GameObject.Find("HousePanel") != null);
        HideDialogue();

        var panel = GameObject.Find("HousePanel");
        var bedroom = panel ? panel.transform.Find("BedroomButton") : null;
        if (bedroom)
            yield return HighlightWithDialogue(bedroom, "Click here to enter your bedroom.");
    }
    IEnumerator ShowStationPanel(
        string panelName,
        string clickMessage,
        string hoursPath,
        string hoursMessage,
        string confirmPath,
        string confirmMessage)
    {
        ShowDialogue(clickMessage);
        // prevent the station panel from closing prematurely
        PanelCloseAllowed = false;
        yield return new WaitUntil(() => GameObject.Find(panelName) != null);
        HideDialogue();

        var panel = GameObject.Find(panelName);
        var hours = panel ? panel.transform.Find(hoursPath) : null;
        if (hours) yield return HighlightWithDialogue(hours, hoursMessage);

        var confirm = panel ? panel.transform.Find(confirmPath) : null;
        if (confirm) yield return HighlightWithDialogue(confirm, confirmMessage);

        // allow the player to close the station panel now
        PanelCloseAllowed = true;
        yield return new WaitUntil(() => GameObject.Find(panelName) == null);
    }

    IEnumerator ExplainWorkPanel()
    {
        yield return ShowStationPanel(
            "WorkPanel",
            "Click the work building to open the work panel.",
            "Panel/HoursInput",
            "Enter how many hours you want to work.",
            "Panel/ConfirmButton",
            "Press Work to start working."
        );
    }

    IEnumerator WaitForPanel(string panelName, bool open)
    {
        yield return new WaitUntil(() => (GameObject.Find(panelName) != null) == open);
    }

    IEnumerator WaitForScene(string sceneName)
    {
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == sceneName);
    }

    IEnumerator ExplainButton(string buttonName, string message)
    {
        var t = GameObject.Find(buttonName)?.transform;
        if (t) yield return HighlightWithDialogue(t, message);
    }

    public IEnumerator StartScene()
    {
        yield return new WaitUntil(() => GameObject.Find("StudioPanel") != null);
        var panel = GameObject.Find("StudioPanel")?.transform;
        if (panel == null) yield break;
        // Prevent the furniture selection panel from closing while we guide the user
        PanelCloseAllowed = false;

        // Highlight and explain the reset button for camera view
        var reset = panel.Find("SetView/ResetButton");
        if (reset)
        {
            reset.gameObject.SetActive(true);
            yield return HighlightWithDialogue(reset, "This is the Reset button. Tap it to reset the camera view.");
        }

        // Locate various sub-views
        var itemButton = panel.Find("TypeView/ItemButton");
        var itemList = panel.Find("DragItemScrollView");
        // Step 1: Show how to open the furniture list
        if (itemButton)
        {
            yield return HighlightWithDialogue(itemButton, "This is where you choose furniture.");
            if (itemList)
            {
                // wait for the furniture list to be shown
                yield return new WaitUntil(() => itemList.gameObject.activeInHierarchy);
                // once visible, highlight the list and explain item costs
                yield return HighlightWithDialogue(itemList,
                    "Choose furniture from here according to your money. Each piece of furniture costs some money.");
            }
        }

        // Step 2: Encourage the player to pick a bed
        ShowDialogue("For now, choose a bed so you can rest in your house. Since this is the tutorial, the bed will be free.");
        yield return WaitForClick();
        HideDialogue();

        // Step 3: Instruct the player to drag the selected item into the room
        var rotateButton = panel.Find("EditView/RotateButton");
        if (rotateButton)
        {
            ShowDialogue("Drag the bed into the room to place it.");
            // Wait until the rotate button becomes active, signalling that an item has been placed for editing
            yield return new WaitUntil(() => rotateButton.gameObject.activeInHierarchy);
            HideDialogue();
            // Highlight the rotate button and explain its purpose
            yield return HighlightWithDialogue(rotateButton, "You can rotate the furniture using this.");
        }

        // Step 4: Highlight the place button so the player knows how to confirm placement
        var placeButton = panel.Find("EditView/PlaceButton");
        if (placeButton)
            yield return HighlightWithDialogue(placeButton, "Use this button to confirm placement of the furniture.");

        // Step 5: Explain how to delete furniture
        var deleteButton = panel.Find("EditView/DeleteButton");
        if (deleteButton)
            yield return HighlightWithDialogue(deleteButton, "Use this to remove furniture.");

        // Step 6: Explain the back button that exits edit mode
        var backButton = panel.Find("BackButton");
        if (backButton)
            yield return HighlightWithDialogue(backButton, "Exit edit mode with this button.");

        // Wait until the player actually exits edit mode (i.e., rotate button is no longer visible)
        var sp = panel.GetComponent<StudioPanel>();
        if (sp != null)
            yield return new WaitUntil(() => sp.GetMode() != StudioMode.EditItem);

        // After leaving edit mode, tell the player to click the house again to exit
        ShowDialogue("Click on the house again to exit.");
        yield return WaitForClick();
        HideDialogue();

        // Allow closing or backing out after returning to normal mode
        PanelCloseAllowed = true;

        // Finally, highlight the button that returns the player to the city
        var exitBtn = GameObject.Find("BackButtonMain")?.GetComponent<Button>();
        if (exitBtn)
        {
            var hl = CreateHighlight(exitBtn.transform);
            ShowDialogue("Use this button to return to the city view.");
            bool clicked = false;
            exitBtn.onClick.AddListener(() => clicked = true);
            while (!clicked) yield return null;
            HideDialogue();
            if (hl) Destroy(hl);
        }

        // Wait until we are back in the main city scene before continuing
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "DemoScene");
        RebindLandmarks();
    }
    /* ─────────────────────────────── HUD highlight ─────────────────────────────── */
    IEnumerator ShowHudHighlights()
    {
        var hud = StatsHUD.CreateIfNeeded();
        if (!hud) yield break;

        // ① Money
        yield return new WaitUntil(() => hud.MoneyTextTransform != null);
        yield return HighlightWithDialogue(
            hud.MoneyTextTransform,
            "Money shows how many coins you have. Earn more by working!"
        );

        // ② Hunger
        yield return new WaitUntil(() => hud.HungerBarTransform != null);
        yield return HighlightWithDialogue(
            hud.HungerBarTransform,
            "Hunger decreases while you work and refills when you eat food."
        );

        // ③ Fatigue
        yield return new WaitUntil(() => hud.FatigueBarTransform != null);
        yield return HighlightWithDialogue(
            hud.FatigueBarTransform,
            "Fatigue rises as you work and drops again when you rest."
        );
    }

    IEnumerator HighlightWithDialogue(Transform target, string message)
    {
        var hl = CreateHighlight(target);
        ShowDialogue(message);
        yield return WaitForClick();
        HideDialogue();
        if (hl) Destroy(hl);
    }

    /* ─────────────────────────────── UI helpers ─────────────────────────────── */
    void ShowDialogue(string msg)
    {
        if (tutorialText)  tutorialText.text = msg;
        if (tutorialPanel) tutorialPanel.SetActive(true);
    }
    void HideDialogue() { if (tutorialPanel) tutorialPanel.SetActive(false); }

    IEnumerator WaitForClick()
{
    // 1️⃣ be sure any old click / touch is fully released
    while (Input.GetMouseButton(0) || Input.touchCount > 0)
        yield return null;

    // 2️⃣ now wait for the NEXT click / touch
    yield return new WaitUntil(() =>
        Input.GetMouseButtonDown(0) ||
        (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began));
}


    /* ─────────────────────────────── Overlay helpers ─────────────────────────────── */
    GameObject CreateHighlight(Transform target)
    {
        if (!target) return null;
        var go = new GameObject("TutorialHighlight", typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(target, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.transform.SetAsLastSibling();
        var img = go.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.25f);
        img.raycastTarget = false;
        overlayObjects.Add(go);
        return go;
    }
    string PathInHierarchy(Transform t)
    {
        if (!t) return "<null>";
        string path = t.name;
        while (t.parent) { t = t.parent; path = $"{t.name}/{path}"; }
        return path;
    }

    /* ─────────────────────────────── Camera utilities ─────────────────────────────── */
    Camera PrepareCamera()
    {
        Camera cam = tutorialCamera ? tutorialCamera :
                     (Camera.main ? Camera.main :
                     (GameObject.Find("Main Camera")?.GetComponent<Camera>()));
        if (!cam) return null;

        Debug.Log($"[Tutorial] Using camera \"{cam.name}\" @ {cam.transform.position}");

        foreach (var b in cam.GetComponents<Behaviour>())
        {
            if (b != cam && b.enabled)
            {
                b.enabled = false;
                disabledBehaviours.Add(b);
            }
        }
        return cam;
    }

    /* ─────────────────────────────── Cleanup ─────────────────────────────── */
    void EndTutorial()
    {
        foreach (var o in overlayObjects) if (o) Destroy(o);
        overlayObjects.Clear();

        foreach (var beh in disabledBehaviours) if (beh) beh.enabled = true;
        disabledBehaviours.Clear();

        PlayerPrefs.SetInt("TutorialSeen", 1);
        PlayerPrefs.Save();

        // re-enable controller if it was disabled
        if (playerController) playerController.enabled = true;

        // Reset the static close flag so panels behave normally after the tutorial
        PanelCloseAllowed = true;

        // Destroy the tutorial UI panel and associated text to ensure no leftover UI remains
        if (tutorialPanel)
        {
            tutorialPanel.SetActive(false);
            // also destroy the text object if it still exists
            var textComp = tutorialText ? tutorialText.gameObject : null;
            if (textComp) Destroy(textComp);
            Destroy(tutorialPanel);
            tutorialPanel = null;
            tutorialText = null;
        }

        // Stop any remaining tutorial coroutines
        StopAllCoroutines();
        // Destroy this manager instance
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        StopAllCoroutines();
        if (Instance == this) Instance = null;
    }
    /* ─────────────────────────────── Landmark re-binding ─────────────────────────────── */
    void RebindLandmarks()
    {
        bool NeedsRebind(Transform t) =>
            !t || !t.gameObject.activeInHierarchy || t.gameObject.scene.name == "DontDestroyOnLoad";

        if (NeedsRebind(houseTarget))
            houseTarget = GameObject.FindWithTag(houseTag)?.transform;

        if (NeedsRebind(workTarget))
            workTarget  = GameObject.FindWithTag(workTag)?.transform;

        if (NeedsRebind(foodTarget))
            foodTarget  = GameObject.FindWithTag(foodTag)?.transform;
    }

    /* ─────────────────────────────── Bootstrap helper (unchanged) ─────────────────────────────── */
    public static TutorialManager CreateIfNeeded()
    {
        bool seen = PlayerPrefs.GetInt("TutorialSeen", 0) != 0;
        // If the tutorial has been seen, avoid instantiating a new manager.
        if (seen)
        {
            // If an instance already exists in the scene, destroy it completely.
            if (Instance != null)
            {
                Object.Destroy(Instance.gameObject);
                Instance = null;
                return null;
            }
            var existingMgr = FindObjectOfType<TutorialManager>();
            if (existingMgr)
            {
                // Destroy any lingering tutorial manager to fully clean up the DontDestroyOnLoad hierarchy
                Object.Destroy(existingMgr.gameObject);
                Instance = null;
                return null;
            }
            // No manager needed if tutorial already completed
            Instance = null;
            return null;
        }

        // If there's already an instance, ensure it runs the tutorial when appropriate
        if (Instance != null)
        {
            Instance.gameObject.SetActive(true);
            Instance.ShowTutorial();
            return Instance;
        }

        // Check for an existing TutorialManager in the scene
        var existing = FindObjectOfType<TutorialManager>();
        if (existing)
        {
            Instance = existing;
            existing.gameObject.SetActive(true);
            existing.ShowTutorial();
            return existing;
        }

        // Load the prefab and instantiate a new tutorial manager
        var prefab = Resources.Load<TutorialManager>("Prefabs/TutorialManager");
        if (prefab)
        {
            var mgr = Instantiate(prefab);
            DontDestroyOnLoad(mgr.gameObject);
            Instance = mgr;
            mgr.ShowTutorial();
            return mgr;
        }

        // As a fallback, create an empty tutorial manager
        var go = new GameObject("TutorialManager");
        DontDestroyOnLoad(go);
        var newMgr = go.AddComponent<TutorialManager>();
        Instance = newMgr;
        newMgr.ShowTutorial();
        return newMgr;
    }
}
