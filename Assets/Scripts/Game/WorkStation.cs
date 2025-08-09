using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;   // <- added

public class WorkStation : MonoBehaviour
{
    [Header("Work settings")]
    [SerializeField] int   wagePerHour = 70;

    GameObject panel;

    /* ────────────────────────────────────────────────────────────── */
    void OnMouseUpAsButton()        // taps also reach here on Android
    {
        if (panel == null)
            {
            var stats = PlayerStats.Instance;
            if (stats != null &&  stats.Fatigue >= 1f)
                BuildRestPopup();
            else
                BuildPanel();
        }
    }

    /* ────────────────────────────────────────────────────────────── */
    void BuildPanel()
    {
        EnsureEventSystem();        // << key change

        /* Canvas -------------------------------------------------- */
        var canvasGo = new GameObject(
            "WorkPanel",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)
        );
        panel = canvasGo;

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
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

        // transparent by default, but still receives clicks
        closeArea.GetComponent<Image>().color = new Color(0, 0, 0, 0);

        closeArea.GetComponent<Button>().onClick.AddListener(ClosePanel);

        /* Panel background --------------------------------------- */
        var panelBg = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelBg.transform.SetParent(canvasGo.transform, false);

        var rect = panelBg.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(300, 150);
        panelBg.GetComponent<Image>().color = new Color(0, 0, 0, .6f);

        /* Input field -------------------------------------------- */
        var inputGO = new GameObject("HoursInput",
            typeof(RectTransform), typeof(Image), typeof(InputField));
        inputGO.transform.SetParent(panelBg.transform, false);

        var inputRect = inputGO.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(.1f, .5f);
        inputRect.anchorMax = new Vector2(.9f, .8f);
        inputRect.offsetMin = inputRect.offsetMax = Vector2.zero;

        /* Placeholder */
        var phGO = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
        phGO.transform.SetParent(inputGO.transform, false);

        var phRect = phGO.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = new Vector2(10, 6);
        phRect.offsetMax = new Vector2(-10, -7);

        var phText = phGO.GetComponent<Text>();
        phText.font       = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        phText.text       = "Hours";
        phText.color      = new Color(.5f, .5f, .5f, .8f);
        phText.alignment  = TextAnchor.MiddleLeft;

        /* Actual text */
        var txtGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtGO.transform.SetParent(inputGO.transform, false);

        var txtRect = txtGO.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = new Vector2(10, 6);
        txtRect.offsetMax = new Vector2(-10, -7);

        var txt = txtGO.GetComponent<Text>();
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.color     = Color.black;
        txt.alignment = TextAnchor.MiddleLeft;

        /* Configure InputField */
        var inputField       = inputGO.GetComponent<InputField>();
        inputField.textComponent   = txt;
        inputField.placeholder     = phText;
        inputField.contentType     = InputField.ContentType.IntegerNumber;   // only digits
        inputField.characterValidation = InputField.CharacterValidation.Integer;
        inputField.keyboardType    = TouchScreenKeyboardType.NumberPad;      // Android num-pad
        inputField.lineType        = InputField.LineType.SingleLine;
        inputField.ActivateInputField();   // focus immediately

        /* Confirm button ----------------------------------------- */
        var btnGO = new GameObject("ConfirmButton",
            typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(panelBg.transform, false);

        var btnRect = btnGO.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(.3f, .1f);
        btnRect.anchorMax = new Vector2(.7f, .3f);
        btnRect.offsetMin = btnRect.offsetMax = Vector2.zero;
        btnGO.GetComponent<Image>().color = Color.white;

        var btnTxtGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        btnTxtGO.transform.SetParent(btnGO.transform, false);

        var btnTxtRect = btnTxtGO.GetComponent<RectTransform>();
        btnTxtRect.anchorMin = Vector2.zero;
        btnTxtRect.anchorMax = Vector2.one;
        btnTxtRect.offsetMin = btnTxtRect.offsetMax = Vector2.zero;

        var btnTxt = btnTxtGO.GetComponent<Text>();
        btnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnTxt.text = "Work";
        btnTxt.color = Color.black;
        btnTxt.alignment = TextAnchor.MiddleCenter;

        btnGO.GetComponent<Button>().onClick.AddListener(() => ConfirmWork(inputField));
    }

    void BuildRestPopup()
    {
        EnsureEventSystem();

        var canvasGo = new GameObject(
            "RestPopup",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)
        );
        panel = canvasGo;

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
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
        rect.sizeDelta = new Vector2(300, 150);
        panelBg.GetComponent<Image>().color = new Color(0, 0, 0, .6f);

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(panelBg.transform, false);

        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);

        var text = textGO.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = "Please rest before working";
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
    }

    /* ────────────────────────────────────────────────────────────── */
    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem),
                                     typeof(StandaloneInputModule)); // handles mouse & touch
#if !UNITY_EDITOR && !UNITY_STANDALONE
            // Optional: explicit touch module for clarity on mobile builds
            es.AddComponent<TouchInputModule>();
#endif
        }
    }

    /* ────────────────────────────────────────────────────────────── */

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

        var houseDrag = FindObjectOfType<HouseDragRotate>();
        if (houseDrag != null) houseDrag.enabled = true;

        var roomCam = FindObjectOfType<RoomCamera>();
        if (roomCam != null) roomCam.enabled = true;

        var controller = FindObjectOfType<CharacterController>();
        if (controller != null) controller.enabled = true;

        FindObjectOfType<StudioController>()?.ClearOverUI();
    }
    void ConfirmWork(InputField input)
    {
        if (!int.TryParse(input.text, out int hours)) hours = 0;
        hours = Mathf.Clamp(hours, 0, 12);
        var stats = PlayerStats.Instance;
        if (stats != null)
        {
            stats.ChangeMoney(hours * wagePerHour);
            float normalized = hours / 12f;
            stats.SetHunger(Mathf.Clamp01(stats.Hunger - normalized));
            stats.SetFatigue(Mathf.Clamp01(stats.Fatigue + normalized));

            // If the player works excessive hours (over 8), they risk raising
            // suspicion. Working long hours without proper rest is unusual for a
            // human and will increment the suspicion meter. The added suspicion
            // scales with how many extra hours were worked beyond 8.
            if (stats.Suspicion < 1f && hours > 8)
            {
                // Normalise the extra hours into a small suspicion increase
                float extra = Mathf.Clamp(hours - 8, 0, 4) / 4f; // 0–1 over 4 hrs
                stats.ChangeSuspicion(0.05f * extra);
            }
            StatsHUD.Instance?.UpdateUI();
        }

        input.DeactivateInputField();
        EventSystem.current.SetSelectedGameObject(null);
        ClosePanel();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

    }
}
