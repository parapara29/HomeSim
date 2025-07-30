using UnityEngine;
using UnityEngine.UI;


public class StatsHUD : MonoBehaviour
{
    public static StatsHUD Instance { get; private set; }

    [SerializeField]
    Vector2 startPosition = new Vector2(10f, -10f);

    Text moneyText;
    Image hungerBar;
    Image fatigueBar;

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

    void BuildUI()
    {
        RectTransform rect = gameObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = startPosition;
        rect.sizeDelta = new Vector2(160f, 60f);

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(transform, false);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0f,0f,0f,0.4f);

        moneyText = CreateText("MoneyText", new Vector2(0f, -5f));
        hungerBar = CreateBar("HungerBar", new Vector2(0f, -25f), Color.green);
        fatigueBar = CreateBar("FatigueBar", new Vector2(0f, -45f), Color.cyan);
    }

    Text CreateText(string name, Vector2 pos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(transform, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(0f, 20f);
        var t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 14;
        t.alignment = TextAnchor.UpperLeft;
        t.text = "";
        return t;
    }

    Image CreateBar(string name, Vector2 pos, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(0f, 15f);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillAmount = 1f;
        return img;
    }

    void Subscribe()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnStatsChanged += UpdateUI;
        UpdateUI();
    }

    void OnDestroy()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnStatsChanged -= UpdateUI;
    }

    public void SetHudPosition(Vector2 pos)
    {
        var rect = GetComponent<RectTransform>();
        if (rect != null)
            rect.anchoredPosition = pos;
    }

    void UpdateUI()
    {
        var stats = PlayerStats.Instance;
        if (stats == null) return;
        if (moneyText != null)
            moneyText.text = "$" + stats.Money.ToString();
        if (hungerBar != null)
            hungerBar.fillAmount = stats.Hunger;
        if (fatigueBar != null)
            fatigueBar.fillAmount = stats.Fatigue;
    }

    public static StatsHUD CreateIfNeeded(Vector2? position = null)
    {
        if (Instance != null)
        {
            if (position.HasValue) Instance.SetHudPosition(position.Value);
            return Instance;
        }

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = canvasGO.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas = c;
        }

        PlayerStats.CreateIfNeeded();


        GameObject go = new GameObject("StatsHUD");
        go.transform.SetParent(canvas.transform, false);
        var hud = go.AddComponent<StatsHUD>();
        if (position.HasValue) hud.SetHudPosition(position.Value);
        return hud;
    }
}

