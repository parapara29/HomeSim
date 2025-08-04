using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class StatsHUD : MonoBehaviour
{
    public static StatsHUD Instance { get; private set; }

    [SerializeField] Vector2 startPosition = new Vector2(10f, -10f);

    // Stats UI elements
    Text  moneyText;
    Image hungerBar;
    Image fatigueBar;
    float hungerBarMaxWidth;
    float fatigueBarMaxWidth;
    UnityEngine.Events.UnityAction<Scene, LoadSceneMode> sceneLoadedHandler;

    // Intro System elements
    private TMP_Text introText;
    private int currentIntroIndex = 0;
    private bool isIntroActive = false;
    private bool hasSeenIntro = false;

    // Intro panel data
    [System.Serializable]
    public struct PanelData
    {
        public Sprite background;
        public string text;
        public float holdTime;
    }

    [SerializeField] PanelData[] introPanels = new PanelData[]
    {
        new PanelData {
            background = null,
            text = "Hidden in the Emerald Canopy lives Kavi, a boy who can outrun panthers but has never seen a traffic light.",
            holdTime = 3f
        },
        new PanelData {
            background = null,
            text = "A small government hover-craft descends.\n\nSFX: Whr-rrr-rrr",
            holdTime = 2.5f
        },
        new PanelData {
            background = null,
            text = "Agent Rivera: \"Hey, Kavi! The Government of Naya City has picked you for our Urban Integration Program.\"",
            holdTime = 3f
        },
        new PanelData {
            background = null,
            text = "Agent Rivera: \"Here's your starter fund—$500. Enough to get you on your feet… if you're smart.\"",
            holdTime = 3f
        },
        new PanelData {
            background = null,
            text = "Agent Rivera: \"Work odd jobs, keep your hunger and energy up, and turn an empty flat into a real home.\"\n\nKavi: \"Sounds like the jungle—just with different predators.\"",
            holdTime = 4f
        },
        new PanelData {
            background = null,
            text = "Agent Rivera: \"Each hour of work pays $70. But skip meals or sleep and you'll crash. Spend wisely.\"\n\n(Money • Hunger • Fatigue = 0%)",
            holdTime = 4f
        },
        new PanelData {
            background = null,
            text = "Kavi: \"Deal. New jungle, new rules.\"",
            holdTime = 2.5f
        },
        new PanelData {
            background = null,
            text = "Guide Kavi through the concrete wilderness. Earn, eat, rest, and build the life he never knew he wanted.",
            holdTime = 4f
        }
    };

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
        
        // Only start intro if this is the first time
        if (PlayerPrefs.GetInt("IntroSeen", 0) == 0)
        {
            CheckAndStartIntro();
        }
    }

    /* ───────────────────────── BOOTSTRAP ───────────────────────── */

    // Commented out auto-creation to prevent creating new panels
    // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    // static void AutoCreate()
    // {
    //     CreateIfNeeded();          // uses default position
    // }

    /* ───────────────────────── UI BUILD ───────────────────────── */

    void BuildUI()
    {
        // Only work with existing StatsHUD elements
        FindExistingElements();
        
        // Create intro text (initially hidden)
        introText = CreateIntroText();
    }

    void FindExistingElements()
    {
        // Find existing money text - try different possible names
        moneyText = FindTextComponent("MoneyText") ?? 
                   FindTextComponent("Money") ?? 
                   FindTextComponent("Text") ?? 
                   transform.GetComponentInChildren<Text>();

        // Find existing hunger bar - try different possible names
        var hungerBarObj = FindImageComponent("HungerBar") ?? 
                          FindImageComponent("Hunger") ?? 
                          transform.GetComponentInChildren<Image>();
        
        if (hungerBarObj != null)
        {
            hungerBar = hungerBarObj;
            hungerBarMaxWidth = hungerBarObj.GetComponent<RectTransform>().rect.width;
        }

        // Find existing fatigue bar - try different possible names
        var fatigueBarObj = FindImageComponent("FatigueBar") ?? 
                           FindImageComponent("Fatigue") ?? 
                           transform.GetComponentInChildren<Image>();
        
        if (fatigueBarObj != null && fatigueBarObj != hungerBar)
        {
            fatigueBar = fatigueBarObj;
            fatigueBarMaxWidth = fatigueBarObj.GetComponent<RectTransform>().rect.width;
        }

        Debug.Log($"Found elements - Money: {moneyText != null}, Hunger: {hungerBar != null}, Fatigue: {fatigueBar != null}");
    }

    Text FindTextComponent(string name)
    {
        var obj = transform.Find(name);
        return obj?.GetComponent<Text>();
    }

    Image FindImageComponent(string name)
    {
        var obj = transform.Find(name);
        return obj?.GetComponent<Image>();
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

    Image CreateBar(string name, Vector2 pos, Color colour, out float maxWidth)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);

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

    TMP_Text CreateIntroText()
    {
        var go = new GameObject("IntroText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMP_Text));
        go.transform.SetParent(transform, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var text = go.GetComponent<TMP_Text>();
        text.fontSize = 12;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.text = "";
        text.fontStyle = FontStyles.Normal;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        
        // Initially hidden
        text.gameObject.SetActive(false);
        
        return text;
    }

    /* ───────────────────────── INTRO SYSTEM ───────────────────────── */

    void CheckAndStartIntro()
    {
        hasSeenIntro = PlayerPrefs.GetInt("IntroSeen", 0) == 1;
        
        if (!hasSeenIntro)
        {
            StartIntro();
        }
    }

    void StartIntro()
    {
        Debug.Log("Starting intro sequence...");
        isIntroActive = true;
        currentIntroIndex = 0;
        
        // Hide stats UI and show intro text
        HideStatsUI();
        ShowIntroText();
        ShowNextIntroPanel();
    }

    void HideStatsUI()
    {
        if (moneyText != null) moneyText.gameObject.SetActive(false);
        if (hungerBar != null) hungerBar.gameObject.SetActive(false);
        if (fatigueBar != null) fatigueBar.gameObject.SetActive(false);
    }

    void ShowStatsUI()
    {
        if (moneyText != null) moneyText.gameObject.SetActive(true);
        if (hungerBar != null) hungerBar.gameObject.SetActive(true);
        if (fatigueBar != null) fatigueBar.gameObject.SetActive(true);
    }

    void ShowIntroText()
    {
        if (introText != null)
        {
            introText.gameObject.SetActive(true);
        }
    }

    void HideIntroText()
    {
        if (introText != null)
        {
            introText.gameObject.SetActive(false);
        }
    }

    void ShowNextIntroPanel()
    {
        if (currentIntroIndex >= introPanels.Length)
        {
            EndIntro();
            return;
        }
        
        var panel = introPanels[currentIntroIndex];
        
        if (introText != null)
        {
            introText.text = panel.text;
            Debug.Log($"Showing intro panel {currentIntroIndex + 1}: {panel.text}");
        }
        else
        {
            Debug.LogError("Intro text component is null!");
        }
        
        // Auto-advance after hold time
        StartCoroutine(AutoAdvanceIntro(panel.holdTime));
    }

    IEnumerator AutoAdvanceIntro(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (isIntroActive)
        {
            currentIntroIndex++;
            ShowNextIntroPanel();
        }
    }

    void EndIntro()
    {
        isIntroActive = false;
        PlayerPrefs.SetInt("IntroSeen", 1);
        PlayerPrefs.Save();
        
        // Hide intro text and show stats UI
        HideIntroText();
        ShowStatsUI();
        
        // Start guidance system
        SendMessage("StartGuidance", SendMessageOptions.DontRequireReceiver);
    }

    public void SkipIntro()
    {
        if (isIntroActive)
        {
            currentIntroIndex = introPanels.Length - 1;
            ShowNextIntroPanel();
        }
    }

    /* ───────────────────────── DATA BIND ───────────────────────── */

    void Subscribe()
    {
        sceneLoadedHandler = (_, __) => UpdateUI();
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
        UpdateUI();
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
    }

    /* ───────────────────────── PUBLIC HELPERS ───────────────────────── */

    public void SetHudPosition(Vector2 pos)
    {
        var r = GetComponent<RectTransform>();
        if (r) r.anchoredPosition = pos;
    }

    public static StatsHUD CreateIfNeeded(Vector2? pos = null)
    {
        if (Instance != null)
        {
            if (pos.HasValue) Instance.SetHudPosition(pos.Value);
            return Instance;
        }

        // Check if there's already a StatsHUD in the scene
        var existingHUD = Object.FindObjectOfType<StatsHUD>();
        if (existingHUD != null)
        {
            Instance = existingHUD;
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

    /* ───────────────────────── INPUT HANDLING ───────────────────────── */

    void Update()
    {
        if (isIntroActive && introText != null)
        {
            // Handle click to advance intro
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                currentIntroIndex++;
                ShowNextIntroPanel();
            }
        }
    }

    /* ───────────────────────── TESTING HELPERS ───────────────────────── */

    [ContextMenu("Reset Intro Seen")]
    public void ResetIntroSeen()
    {
        PlayerPrefs.SetInt("IntroSeen", 0);
        PlayerPrefs.Save();
        Debug.Log("Intro seen flag reset! Intro will show again on next play.");
    }

    [ContextMenu("Mark Intro as Seen")]
    public void MarkIntroAsSeen()
    {
        PlayerPrefs.SetInt("IntroSeen", 1);
        PlayerPrefs.Save();
        Debug.Log("Intro marked as seen! Intro will be skipped on next play.");
    }

    [ContextMenu("Force Start Intro")]
    public void ForceStartIntro()
    {
        Debug.Log("Force starting intro...");
        isIntroActive = false;
        currentIntroIndex = 0;
        StartIntro();
    }

    [ContextMenu("Start Intro on Existing HUD")]
    public static void StartIntroOnExistingHUD()
    {
        var existingHUD = Object.FindObjectOfType<StatsHUD>();
        if (existingHUD != null)
        {
            existingHUD.ResetIntroSeen();
            existingHUD.ForceStartIntro();
        }
        else
        {
            Debug.LogWarning("No StatsHUD found in scene!");
        }
    }

    // Public method to start intro from Inspector or other scripts
    [ContextMenu("Start Intro Now")]
    public void StartIntroNow()
    {
        Debug.Log("Manually starting intro...");
        ResetIntroSeen();
        ForceStartIntro();
    }
}
