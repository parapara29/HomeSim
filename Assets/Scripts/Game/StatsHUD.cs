using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StatsHUD : MonoBehaviour
{
    public static StatsHUD Instance { get; private set; }

    [SerializeField] Vector2 startPosition = new Vector2(10f, -10f);

    Text  moneyText;
    Image hungerBar;
    Image fatigueBar;
    Image suspicionBar;
    public Transform MoneyTextTransform  { get; private set; }
    public Transform HungerBarTransform  { get; private set; }
    public Transform FatigueBarTransform { get; private set; }
    public Transform SuspicionBarTransform { get; private set; }
    float hungerBarMaxWidth;
    float fatigueBarMaxWidth;
    float suspicionBarMaxWidth;

    // Display element for showing the current time (e.g. "HH:mm").
    Text timeText;

    // A button to open Agent Miller's suspicion log
    Button logButton;
    // Panel that displays the suspicion log; created lazily
    GameObject logPanel;

    // Expose log button transform so tutorials can highlight it
    public Transform LogButtonTransform { get; private set; }
    UnityEngine.Events.UnityAction<Scene, LoadSceneMode> sceneLoadedHandler;

    /* ───────────────────────── LIFECYCLE ───────────────────────── */

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildUI();
        Subscribe();
    }

    /* ───────────────────────── BOOTSTRAP ───────────────────────── */

    // Runs automatically before the first scene is loaded
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        CreateIfNeeded();          // uses default position
    }

    /* ───────────────────────── UI BUILD ───────────────────────── */

    void BuildUI()
    {
        // Root RectTransform
        var rect = gameObject.AddComponent<RectTransform>();
        rect.anchorMin       = new Vector2(0, 1);
        rect.anchorMax       = new Vector2(0, 1);
        rect.pivot           = new Vector2(0, 1);
        rect.anchoredPosition = startPosition;
        // expand height to accommodate the new suspicion bar
        // Increase the HUD height to accommodate the suspicion bar, log button and time display
        rect.sizeDelta       = new Vector2(160, 100);

        /* background */
        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(transform, false);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0, 0, 0, 0.4f);

        /* elements */
        moneyText  = CreateText ("MoneyText" , new Vector2(0, -5));
        MoneyTextTransform = moneyText.transform;
        hungerBar  = CreateBar  ("HungerBar" , new Vector2(0, -25), Color.green, out hungerBarMaxWidth, out var hungerTransform);
        HungerBarTransform = hungerTransform;
        fatigueBar = CreateBar  ("FatigueBar", new Vector2(0, -45), Color.cyan, out fatigueBarMaxWidth, out var fatigueTransform);
        FatigueBarTransform = fatigueTransform;
        // suspicion bar below fatigue bar, tinted magenta/red for visibility
        suspicionBar = CreateBar("SuspicionBar", new Vector2(0, -65), new Color(1f, 0.2f, 0.2f), out suspicionBarMaxWidth, out var suspicionTransform);
        SuspicionBarTransform = suspicionTransform;

        // Agent log button below the suspicion bar
        var logBtnGO = new GameObject("LogButton", typeof(RectTransform), typeof(Image), typeof(Button));
        logBtnGO.transform.SetParent(transform, false);
        var logRect = logBtnGO.GetComponent<RectTransform>();
        logRect.anchorMin = new Vector2(0, 1);
        logRect.anchorMax = new Vector2(0, 1);
        logRect.pivot     = new Vector2(0, 1);
        // Position it below the suspicion bar with some padding
        logRect.anchoredPosition = new Vector2(0, -85);
        logRect.sizeDelta        = new Vector2(GetComponent<RectTransform>().rect.width, 18);
        var logImg = logBtnGO.GetComponent<Image>();
        logImg.color = new Color(0.2f, 0.2f, 0.4f, 0.8f);
        var logButtonComponent = logBtnGO.GetComponent<Button>();
        // Create text child for the button
        var logTxtGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        logTxtGO.transform.SetParent(logBtnGO.transform, false);
        var logTxtRect = logTxtGO.GetComponent<RectTransform>();
        logTxtRect.anchorMin = Vector2.zero;
        logTxtRect.anchorMax = Vector2.one;
        logTxtRect.offsetMin = logTxtRect.offsetMax = Vector2.zero;
        var logTxt = logTxtGO.GetComponent<Text>();
        logTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        logTxt.text = "Agent Log";
        logTxt.color = Color.white;
        logTxt.alignment = TextAnchor.MiddleCenter;
        // Assign click handler
        logButtonComponent.onClick.AddListener(ToggleLogPanel);
        logButton = logBtnGO.GetComponent<Button>();
        LogButtonTransform = logBtnGO.transform;

        // Time display at the very bottom of the HUD
        var timeGO = new GameObject("TimeText", typeof(RectTransform), typeof(Text));
        timeGO.transform.SetParent(transform, false);
        var timeRect = timeGO.GetComponent<RectTransform>();
        timeRect.anchorMin = new Vector2(0, 1);
        timeRect.anchorMax = new Vector2(0, 1);
        timeRect.pivot     = new Vector2(0, 1);
        timeRect.anchoredPosition = new Vector2(0, -100);
        timeRect.sizeDelta        = new Vector2(GetComponent<RectTransform>().rect.width, 18);
        timeText = timeGO.GetComponent<Text>();
        timeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        timeText.color = Color.white;
        timeText.alignment = TextAnchor.UpperLeft;
        timeText.text = "";
    }

    Text CreateText(string name, Vector2 pos)
    {
        var go   = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(transform, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin       = new Vector2(0, 1);
        rect.anchorMax       = new Vector2(1, 1);
        rect.pivot           = new Vector2(0, 1);
        rect.anchoredPosition = pos;
        rect.sizeDelta       = new Vector2(0, 20);

        var t = go.GetComponent<Text>();
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // ✅ valid font
        t.fontSize  = 14;
        t.alignment = TextAnchor.UpperLeft;
        t.color     = Color.white;                                     // ✅ visible colour
        t.text      = "";
        return t;
    }

    Image CreateBar(string name, Vector2 pos, Color colour, out float maxWidth, out Transform rootTransform)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        rootTransform = go.transform;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0, 1);
        rect.anchorMax        = new Vector2(0, 1);
        rect.pivot            = new Vector2(0, 1);
        rect.anchoredPosition = pos;
        rect.sizeDelta        = new Vector2(GetComponent<RectTransform>().rect.width, 15);

        // background to show empty portion
        var bg = go.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.3f);

        maxWidth = rect.rect.width;

        // create fill image as child
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(go.transform, false);
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(0, 1);
        fillRect.pivot     = new Vector2(0, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(maxWidth, 0);

        var img = fill.GetComponent<Image>();
        img.color = colour;
        return img;
    }

    /* ───────────────────────── DATA BIND ───────────────────────── */

    void Subscribe()
    {
        sceneLoadedHandler = OnSceneLoaded;
        SceneManager.sceneLoaded += sceneLoadedHandler;
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged += UpdateUI; // update whenever stats change
        }
        else
        {
            StartCoroutine(WaitForPlayerStats());
        }
        // also refresh whenever a new scene loads (so the HUD survives)

        UpdateUI();
    }
    IEnumerator WaitForPlayerStats()
    {
        while (PlayerStats.Instance == null)
            yield return null;

        PlayerStats.Instance.OnStatsChanged += UpdateUI;
        UpdateUI();
    }

    void OnDestroy()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnStatsChanged -= UpdateUI;
        if (sceneLoadedHandler != null)
            SceneManager.sceneLoaded -= sceneLoadedHandler;

        if (Instance == this)
            Instance = null;
    }

    void OnSceneLoaded(Scene _, LoadSceneMode __)
    {
        var sceneName = SceneManager.GetActiveScene().name;
        bool isIntro = sceneName == "IntroStart" || sceneName == "IntoStart";
        gameObject.SetActive(!isIntro);
        if (!isIntro) UpdateUI();
    }

    public void UpdateUI()
    {
        var s = PlayerStats.Instance;
        if (s == null) return;

        if (moneyText != null)
            moneyText.text = $"${s.Money}";
        if (hungerBar != null)
            {
            float w = hungerBarMaxWidth * Mathf.Clamp01(s.Hunger);
            hungerBar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
        }
        if (fatigueBar != null)
            {
            float w = fatigueBarMaxWidth * Mathf.Clamp01(s.Fatigue);
            fatigueBar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
        }
        if (suspicionBar != null)
        {
            float w = suspicionBarMaxWidth * Mathf.Clamp01(s.Suspicion);
            suspicionBar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
            // Change the colour of the suspicion bar based on thresholds
            if (s.Suspicion < 0.5f)
            {
                // Yellow for low suspicion
                suspicionBar.color = new Color(1f, 0.9f, 0.1f, 1f);
            }
            else if (s.Suspicion < 0.75f)
            {
                // Orange for medium suspicion
                suspicionBar.color = new Color(1f, 0.5f, 0.1f, 1f);
            }
            else
            {
                // Red for high suspicion
                suspicionBar.color = new Color(1f, 0f, 0f, 1f);
            }
        }

        // Update the time display each time the UI refreshes
        if (timeText != null)
        {
            timeText.text = System.DateTime.Now.ToString("HH:mm");
        }
    }

    // Optionally update the time display every frame to make it responsive
    void Update()
    {
        if (timeText != null)
        {
            timeText.text = System.DateTime.Now.ToString("HH:mm");
        }
    }

    /* ───────────────────────── PUBLIC HELPERS ───────────────────────── */

    public void SetHudPosition(Vector2 pos)
    {
        var r = GetComponent<RectTransform>();
        if (r) r.anchoredPosition = pos;
    }

    /// <summary>
    /// Toggles the Agent Miller log panel. If the panel is currently open it
    /// will be closed, otherwise it will be created and populated with the
    /// current suspicion log entries.
    /// </summary>
    private void ToggleLogPanel()
    {
        if (logPanel != null)
        {
            Destroy(logPanel);
            logPanel = null;
            return;
        }
        CreateLogPanel();
    }

    /// <summary>
    /// Creates a floating panel displaying all logged suspicious activities.
    /// The panel is a child of the HUD canvas and contains scrollable text
    /// for ease of reading. Includes a close button to dismiss the panel.
    /// </summary>
    private void CreateLogPanel()
    {
        // find or create an overlay canvas (reuse HUD's canvas)
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = Object.FindObjectOfType<Canvas>();
        }
        logPanel = new GameObject("AgentLogPanel", typeof(RectTransform), typeof(Image));
        logPanel.transform.SetParent(canvas.transform, false);
        var rect = logPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(300, 200);
        logPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);
        // scroll view root
        var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGO.transform.SetParent(logPanel.transform, false);
        var scrollRect = scrollGO.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.05f, 0.15f);
        scrollRect.anchorMax = new Vector2(0.95f, 0.95f);
        scrollRect.offsetMin = scrollRect.offsetMax = Vector2.zero;
        scrollGO.GetComponent<Image>().color = new Color(0, 0, 0, 0.2f);
        // Add a RectMask2D so content is clipped to the bounds of the scroll area
        scrollGO.AddComponent<RectMask2D>();
        var scroll = scrollGO.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        // content container
        var contentGO = new GameObject("Content", typeof(RectTransform), typeof(Text));
        contentGO.transform.SetParent(scrollGO.transform, false);
        var contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot     = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);
        var contentText = contentGO.GetComponent<Text>();
        contentText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        contentText.alignment = TextAnchor.UpperLeft;
        contentText.color = Color.white;
        contentText.text = SuspicionLogger.GetLogText();
        contentText.horizontalOverflow = HorizontalWrapMode.Wrap;
        contentText.verticalOverflow   = VerticalWrapMode.Overflow;
        scroll.content = contentRect;
        // update content height based on number of lines
        Canvas.ForceUpdateCanvases();
        float height = contentText.preferredHeight;
        contentRect.sizeDelta = new Vector2(0, height);
        scroll.viewport = scrollRect;
        // close button
        var closeBtnGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnGO.transform.SetParent(logPanel.transform, false);
        var closeRect = closeBtnGO.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.8f, 0.02f);
        closeRect.anchorMax = new Vector2(0.95f, 0.12f);
        closeRect.offsetMin = closeRect.offsetMax = Vector2.zero;
        closeBtnGO.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
        var closeTxtGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        closeTxtGO.transform.SetParent(closeBtnGO.transform, false);
        var closeTxtRect = closeTxtGO.GetComponent<RectTransform>();
        closeTxtRect.anchorMin = Vector2.zero;
        closeTxtRect.anchorMax = Vector2.one;
        closeTxtRect.offsetMin = closeTxtRect.offsetMax = Vector2.zero;
        var closeTxt = closeTxtGO.GetComponent<Text>();
        closeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        closeTxt.text = "Close";
        closeTxt.color = Color.white;
        closeTxt.alignment = TextAnchor.MiddleCenter;
        closeBtnGO.GetComponent<Button>().onClick.AddListener(() =>
        {
            Destroy(logPanel);
            logPanel = null;
        });
    }

    public static StatsHUD CreateIfNeeded(Vector2? pos = null)
    {
        if (Instance != null)
        {
            if (pos.HasValue) Instance.SetHudPosition(pos.Value);
            return Instance;
        }

        // find or create an overlay Canvas
        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var cGo = new GameObject("HUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;                       // keep on top
            var scaler = cGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            DontDestroyOnLoad(cGo);
            
        }

        // make sure PlayerStats exists
        PlayerStats.CreateIfNeeded();

        // build the HUD
        var hudGo = new GameObject("StatsHUD");
        DontDestroyOnLoad(hudGo);
        hudGo.transform.SetParent(canvas.transform, false);
        var hud = hudGo.AddComponent<StatsHUD>();

        if (pos.HasValue) hud.SetHudPosition(pos.Value);
        return hud;
    }
}
