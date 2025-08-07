using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[System.Serializable]
public struct FoodOption
{
    public string name;
    public int cost;
    [Range(0f, 1f)] public float hunger;
}

public class FoodMenu : MonoBehaviour
{
    [SerializeField] FoodOption[] normalFoods = new FoodOption[]
    {
        new FoodOption { name = "Rice",    cost = 100, hunger = 0.4f },
        new FoodOption { name = "Chicken", cost = 150, hunger = 0.6f },
    };

    [SerializeField] FoodOption[] fastFoods = new FoodOption[]
    {
        new FoodOption { name = "Burger", cost = 40, hunger = 0.2f },
        new FoodOption { name = "Pizza",  cost = 60, hunger = 0.25f },
    };

    GameObject panel;

    void OnMouseUpAsButton()
    {
        if (panel == null)
            BuildPanel();
    }

    void BuildPanel()
    {
        EnsureEventSystem();

        var canvasGo = new GameObject(
            "FoodPanel",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)
        );
        panel = canvasGo;

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var closeArea = new GameObject(
            "CloseArea",
            typeof(RectTransform), typeof(Image), typeof(Button)
        );
        closeArea.transform.SetParent(canvasGo.transform, false);
        var caRect = closeArea.GetComponent<RectTransform>();
        caRect.anchorMin = Vector2.zero;
        caRect.anchorMax = Vector2.one;
        caRect.offsetMin = caRect.offsetMax = Vector2.zero;
        closeArea.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        closeArea.GetComponent<Button>().onClick.AddListener(ClosePanel);

        var panelBg = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelBg.transform.SetParent(canvasGo.transform, false);
        var rect = panelBg.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(300, 260);
        panelBg.GetComponent<Image>().color = new Color(0, 0, 0, .6f);

        float y = -10f;
        y = AddSection(panelBg.transform, "Normal Food", normalFoods, y);
        AddSection(panelBg.transform, "Fast Food", fastFoods, y);
    }

    float AddSection(Transform parent, string heading, FoodOption[] options, float startY)
    {
        var header = new GameObject(heading, typeof(RectTransform), typeof(Text));
        header.transform.SetParent(parent, false);
        var hRect = header.GetComponent<RectTransform>();
        hRect.anchorMin = new Vector2(0, 1);
        hRect.anchorMax = new Vector2(1, 1);
        hRect.pivot = new Vector2(0, 1);
        hRect.anchoredPosition = new Vector2(0, startY);
        hRect.sizeDelta = new Vector2(0, 20);
        var hText = header.GetComponent<Text>();
        hText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hText.fontSize = 16;
        hText.alignment = TextAnchor.MiddleCenter;
        hText.color = Color.white;
        hText.text = heading;

        startY -= 30f;
        foreach (var food in options)
        {
            var btnGO = new GameObject(food.name + "Button",
                typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(parent, false);
            var btnRect = btnGO.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0, 1);
            btnRect.anchorMax = new Vector2(1, 1);
            btnRect.pivot = new Vector2(0, 1);
            btnRect.anchoredPosition = new Vector2(0, startY);
            btnRect.sizeDelta = new Vector2(0, 25);
            btnGO.GetComponent<Image>().color = Color.white;

            var txtGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtGO.transform.SetParent(btnGO.transform, false);
            var txtRect = txtGO.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = txtRect.offsetMax = Vector2.zero;
            var txt = txtGO.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.black;
            txt.text = $"{food.name} (${food.cost})";

            var captured = food;
            btnGO.GetComponent<Button>().onClick.AddListener(() => SelectFood(captured));

            startY -= 35f;
        }
        return startY;
    }

    void SelectFood(FoodOption food)
    {
        var stats = PlayerStats.Instance;
        if (stats != null && stats.Money >= food.cost)
        {
            stats.ChangeMoney(-food.cost);
            stats.SetHunger(Mathf.Clamp01(stats.Hunger + food.hunger));
            StatsHUD.Instance?.UpdateUI();
        }
        ClosePanel();
    }

    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
#if !UNITY_EDITOR && !UNITY_STANDALONE
            es.AddComponent<TouchInputModule>();
#endif
        }
    }

    void ClosePanel()
    {
        // In tutorial mode, disallow closing this panel until allowed by TutorialManager.
        if (TutorialManager.Instance != null && !TutorialManager.PanelCloseAllowed)
            return;
        if (panel != null)
        {
            Destroy(panel);
            panel = null;
        }
    }
}