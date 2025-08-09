using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Enhances the existing StatsHUD without modifying its source file. This
/// helper expands the size of the status bars, adds a game time display
/// anchored to the top centre, allows the player to view the current day
/// by clicking the time, and hides the original time display. It attaches
/// itself to a separate GameObject and persists across scenes. All
/// modifications occur at runtime after the HUD has been created.
/// </summary>
public class StatsHUDEnhancer : MonoBehaviour
{
    public static StatsHUDEnhancer Instance { get; private set; }

    // Custom time text displayed on the HUD
    private Text customTimeText;
    // Panel showing the current day; created lazily
    private GameObject dayPanel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        CreateIfNeeded();
    }

    /// <summary>
    /// Ensures an enhancer instance exists and attaches it to a new
    /// GameObject. The enhancer will locate the StatsHUD and modify its
    /// layout once the scene has loaded.
    /// </summary>
    public static StatsHUDEnhancer CreateIfNeeded()
    {
        if (Instance != null) return Instance;
        var hud = StatsHUD.CreateIfNeeded();
        if (hud == null) return null;
        var go = new GameObject("StatsHUDEnhancer");
        Object.DontDestroyOnLoad(go);
        Instance = go.AddComponent<StatsHUDEnhancer>();
        return Instance;
    }

    void Start()
    {
        EnhanceHUD();
    }

    void Update()
    {
        // Update the custom time text each frame using reflection to avoid
        // compile‑time dependency on GameClock. If GameClock or its methods
        // cannot be found, default to an empty string.
        if (customTimeText != null)
        {
            try
            {
                var clockType = System.Type.GetType("GameClock");
                if (clockType != null)
                {
                    var instProp = clockType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var clockInstance = instProp?.GetValue(null, null);
                    var getTime = clockType.GetMethod("GetTimeString", System.Type.EmptyTypes);
                    if (clockInstance != null && getTime != null)
                    {
                        customTimeText.text = (string)getTime.Invoke(clockInstance, null);
                    }
                }
            }
            catch
            {
                // On any error, clear the time string
                customTimeText.text = string.Empty;
            }
        }
    }

    private void EnhanceHUD()
    {
        var hud = StatsHUD.Instance;
        if (hud == null) return;
        var hudRect = hud.GetComponent<RectTransform>();
        if (hudRect == null) return;

        // Increase the HUD height to accommodate larger bars and our custom time display
        hudRect.sizeDelta = new Vector2(hudRect.sizeDelta.x, 150f);

        // Hide the original time display if present
        var timeObj = hud.transform.Find("TimeText");
        if (timeObj != null)
        {
            timeObj.gameObject.SetActive(false);
        }
        var timeBtnObj = hud.transform.Find("TimeButton");
        if (timeBtnObj != null)
        {
            timeBtnObj.gameObject.SetActive(false);
        }

        // Resize and reposition the bars. Increase height and space them evenly
        AdjustBar(hud.transform, "HungerBar", -30f);
        AdjustBar(hud.transform, "FatigueBar", -60f);
        AdjustBar(hud.transform, "SuspicionBar", -90f);

        // Reposition the log button if it exists
        var logBtn = hud.transform.Find("LogButton");
        if (logBtn != null)
        {
            var rect = logBtn.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0f, -120f);
        }

        // Create our custom time display as a button anchored to the top centre
        var timeGO = new GameObject("GameTimeDisplay", typeof(RectTransform), typeof(Image), typeof(Button));
        timeGO.transform.SetParent(hud.transform, false);
        var timeRect = timeGO.GetComponent<RectTransform>();
        timeRect.anchorMin = new Vector2(0.5f, 1f);
        timeRect.anchorMax = new Vector2(0.5f, 1f);
        timeRect.pivot     = new Vector2(0.5f, 1f);
        timeRect.anchoredPosition = new Vector2(0f, -5f);
        timeRect.sizeDelta = new Vector2(hudRect.rect.width, 28f);
        var timeImg = timeGO.GetComponent<Image>();
        timeImg.color = new Color(0f, 0f, 0f, 0f);
        var btn = timeGO.GetComponent<Button>();
        btn.onClick.AddListener(ToggleDayPanel);
        // Create child text for time
        var txtGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtGO.transform.SetParent(timeGO.transform, false);
        var txtRect = txtGO.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = txtRect.offsetMax = Vector2.zero;
        customTimeText = txtGO.GetComponent<Text>();
        customTimeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        customTimeText.fontSize = 18;
        customTimeText.alignment = TextAnchor.MiddleCenter;
        customTimeText.color = Color.white;
        customTimeText.text = string.Empty;
    }

    /// <summary>
    /// Adjusts the size and vertical position of a bar element within the HUD.
    /// </summary>
    private void AdjustBar(Transform hudRoot, string name, float yPosition)
    {
        var t = hudRoot.Find(name);
        if (t == null) return;
        var rt = t.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, 20f);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, yPosition);
        }
    }

    /// <summary>
    /// Toggles the day panel display. When opened, displays the current day.
    /// </summary>
    private void ToggleDayPanel()
    {
        if (dayPanel != null)
        {
            Destroy(dayPanel);
            dayPanel = null;
            return;
        }
        // Find the canvas to parent the day panel
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = Object.FindObjectOfType<Canvas>();
        dayPanel = new GameObject("DayPanel", typeof(RectTransform), typeof(Image));
        dayPanel.transform.SetParent(canvas.transform, false);
        var rect = dayPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(200f, 100f);
        dayPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);
        // Day text
        var dayTxtGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        dayTxtGO.transform.SetParent(dayPanel.transform, false);
        var dayTxtRect = dayTxtGO.GetComponent<RectTransform>();
        dayTxtRect.anchorMin = new Vector2(0f, 0.3f);
        dayTxtRect.anchorMax = new Vector2(1f, 0.7f);
        dayTxtRect.offsetMin = dayTxtRect.offsetMax = Vector2.zero;
        var dayTxt = dayTxtGO.GetComponent<Text>();
        dayTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        dayTxt.fontSize = 20;
        dayTxt.alignment = TextAnchor.MiddleCenter;
        dayTxt.color = Color.white;
        // Determine the day string using reflection
        string dayString = "Day 1";
        try
        {
            var clockType = System.Type.GetType("GameClock");
            if (clockType != null)
            {
                var instProp = clockType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var clockInstance = instProp?.GetValue(null, null);
                var getDay = clockType.GetMethod("GetDayString", System.Type.EmptyTypes);
                if (clockInstance != null && getDay != null)
                {
                    dayString = (string)getDay.Invoke(clockInstance, null);
                }
            }
        }
        catch { }
        dayTxt.text = dayString;
        // Close button
        var closeBtnGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnGO.transform.SetParent(dayPanel.transform, false);
        var closeRect = closeBtnGO.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.7f, 0.05f);
        closeRect.anchorMax = new Vector2(0.95f, 0.2f);
        closeRect.offsetMin = closeRect.offsetMax = Vector2.zero;
        var closeImg = closeBtnGO.GetComponent<Image>();
        closeImg.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
        var closeTxtGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        closeTxtGO.transform.SetParent(closeBtnGO.transform, false);
        var closeTxtRect = closeTxtGO.GetComponent<RectTransform>();
        closeTxtRect.anchorMin = Vector2.zero;
        closeTxtRect.anchorMax = Vector2.one;
        closeTxtRect.offsetMin = closeTxtRect.offsetMax = Vector2.zero;
        var closeTxt = closeTxtGO.GetComponent<Text>();
        closeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        closeTxt.fontSize = 14;
        closeTxt.alignment = TextAnchor.MiddleCenter;
        closeTxt.color = Color.white;
        closeTxt.text = "Close";
        closeBtnGO.GetComponent<Button>().onClick.AddListener(() =>
        {
            Destroy(dayPanel);
            dayPanel = null;
        });
    }
}