using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LoadSceneOnClick : MonoBehaviour
{
    [SerializeField] string sceneName = "Start";
    bool loading;
    GameObject panel;

    void OnMouseUpAsButton()
    {
        if (panel == null)
            BuildPanel();
    }

    void BuildPanel()
    {
        EnsureEventSystem();

        /* Canvas -------------------------------------------------- */
        var canvasGo = new GameObject(
            "RestPanel",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)
        );
        panel = canvasGo;

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        /* Close area ---------------------------------------------- */
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

        /* Panel background --------------------------------------- */
        var panelBg = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelBg.transform.SetParent(canvasGo.transform, false);

        var rect = panelBg.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(350, 200);
        panelBg.GetComponent<Image>().color = new Color(0, 0, 0, .6f);

        /* Input field -------------------------------------------- */
        var inputGO = new GameObject("HoursInput",
            typeof(RectTransform), typeof(Image), typeof(InputField));
        inputGO.transform.SetParent(panelBg.transform, false);

        var inputRect = inputGO.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(.1f, .6f);
        inputRect.anchorMax = new Vector2(.9f, .9f);
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
        inputField.contentType     = InputField.ContentType.IntegerNumber;
        inputField.characterValidation = InputField.CharacterValidation.Integer;
        inputField.keyboardType    = TouchScreenKeyboardType.NumberPad;
        inputField.lineType        = InputField.LineType.SingleLine;
        inputField.ActivateInputField();

        /* Rest button -------------------------------------------- */
        var restGO = new GameObject("RestButton",
            typeof(RectTransform), typeof(Image), typeof(Button));
        restGO.transform.SetParent(panelBg.transform, false);

        var restRect = restGO.GetComponent<RectTransform>();
        restRect.anchorMin = new Vector2(.1f, .1f);
        restRect.anchorMax = new Vector2(.45f, .4f);
        restRect.offsetMin = restRect.offsetMax = Vector2.zero;
        restGO.GetComponent<Image>().color = Color.white;

        var restTxtGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        restTxtGO.transform.SetParent(restGO.transform, false);

        var restTxtRect = restTxtGO.GetComponent<RectTransform>();
        restTxtRect.anchorMin = Vector2.zero;
        restTxtRect.anchorMax = Vector2.one;
        restTxtRect.offsetMin = restTxtRect.offsetMax = Vector2.zero;

        var restTxt = restTxtGO.GetComponent<Text>();
        restTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        restTxt.text = "Rest";
        restTxt.color = Color.black;
        restTxt.alignment = TextAnchor.MiddleCenter;

        restGO.GetComponent<Button>().onClick.AddListener(() => ConfirmRest(inputField));

        /* Edit button -------------------------------------------- */
        var editGO = new GameObject("EditButton",
            typeof(RectTransform), typeof(Image), typeof(Button));
        editGO.transform.SetParent(panelBg.transform, false);

        var editRect = editGO.GetComponent<RectTransform>();
        editRect.anchorMin = new Vector2(.55f, .1f);
        editRect.anchorMax = new Vector2(.9f, .4f);
        editRect.offsetMin = editRect.offsetMax = Vector2.zero;
        editGO.GetComponent<Image>().color = Color.white;

        var editTxtGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        editTxtGO.transform.SetParent(editGO.transform, false);

        var editTxtRect = editTxtGO.GetComponent<RectTransform>();
        editTxtRect.anchorMin = Vector2.zero;
        editTxtRect.anchorMax = Vector2.one;
        editTxtRect.offsetMin = editTxtRect.offsetMax = Vector2.zero;

        var editTxt = editTxtGO.GetComponent<Text>();
        editTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        editTxt.text = "Edit Furniture";
        editTxt.color = Color.black;
        editTxt.alignment = TextAnchor.MiddleCenter;

        editGO.GetComponent<Button>().onClick.AddListener(EditFurniture);
    }

    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem),
                                     typeof(StandaloneInputModule));
#if !UNITY_EDITOR && !UNITY_STANDALONE
            es.AddComponent<TouchInputModule>();
#endif
        }
    }

    void ClosePanel()
    {
        if (panel != null)
        {
            Destroy(panel);
            panel = null;
        }
    }

    void ConfirmRest(InputField input)
    {
        if (!int.TryParse(input.text, out int hours)) hours = 0;
        hours = Mathf.Clamp(hours, 0, 24);
        var stats = PlayerStats.Instance;
        if (stats != null)
        {
            float normalized = hours / 24f;
            stats.SetFatigue(Mathf.Clamp01(stats.Fatigue - normalized));
            StatsHUD.Instance?.UpdateUI();
        }

        input.DeactivateInputField();
        EventSystem.current.SetSelectedGameObject(null);
        ClosePanel();
    }

    void EditFurniture()
    {
        if (loading || SceneManager.GetActiveScene().name == sceneName)
            return;

        Time.timeScale = 1f;                   // make sure we’re not paused
        loading = true;
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
    }
}
