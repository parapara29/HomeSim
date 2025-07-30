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
        rect.sizeDelta       = new Vector2(160, 60);

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
        hungerBar  = CreateBar  ("HungerBar" , new Vector2(0, -25), Color.green);
        fatigueBar = CreateBar  ("FatigueBar", new Vector2(0, -45), Color.cyan);
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

    Image CreateBar(string name, Vector2 pos, Color colour)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin       = new Vector2(0, 1);
        rect.anchorMax       = new Vector2(1, 1);
        rect.pivot           = new Vector2(0, 1);
        rect.anchoredPosition = pos;
        rect.sizeDelta       = new Vector2(0, 15);

        var img = go.GetComponent<Image>();
        img.color      = colour;
        img.type       = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillAmount = 1;
        return img;
    }

    /* ───────────────────────── DATA BIND ───────────────────────── */

    void Subscribe()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnStatsChanged += UpdateUI;

        // also refresh whenever a new scene loads (so the HUD survives)
        SceneManager.sceneLoaded += (_, __) => UpdateUI();

        UpdateUI();
    }

    void OnDestroy()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnStatsChanged -= UpdateUI;
        SceneManager.sceneLoaded -= (_, __) => UpdateUI();
    }

    void UpdateUI()
    {
        var s = PlayerStats.Instance;
        if (s == null) return;

        moneyText.text   = $"${s.Money}";
        hungerBar.fillAmount  = Mathf.Clamp01(s.Hunger);
        fatigueBar.fillAmount = Mathf.Clamp01(s.Fatigue);
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

        // find or create an overlay Canvas
        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var cGo = new GameObject("HUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cGo.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;                       // keep on top
            var scaler = cGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        // make sure PlayerStats exists
        PlayerStats.CreateIfNeeded();

        // build the HUD
        var hudGo = new GameObject("StatsHUD");
        hudGo.transform.SetParent(canvas.transform, false);
        var hud = hudGo.AddComponent<StatsHUD>();

        if (pos.HasValue) hud.SetHudPosition(pos.Value);
        return hud;
    }
}
