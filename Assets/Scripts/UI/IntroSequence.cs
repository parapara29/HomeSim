using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

[System.Serializable]
public struct PanelData
{
    public Sprite background;
    public Sprite character;          // leave empty if no character
    public string text;
    public float  holdTime;
}

public class IntroSequence : MonoBehaviour
{
    [SerializeField] PanelData[] panels;

    [SerializeField] string panelPrefabPath = "Prefabs/IntroPanel";

    [Header("Auto-size settings")]
    [SerializeField] float minFontSize = 24f;
    [SerializeField] float maxFontSize = 48f;

    int  currentIndex;
    bool hasSeenIntro;

    GameObject currentPanel;
    GameObject panelPrefab;
    Image   backgroundImage;
    Image   characterImage;
    TMP_Text dialogueText;

    /* ───────────────────────────────────────────── */
    void Awake()
    {
        hasSeenIntro = PlayerPrefs.GetInt("IntroSeen", 0) == 1;
    }

    void Start()
    {
        if (hasSeenIntro)
        {
            SceneManager.LoadScene("DemoScene");
            return;
        }

        currentIndex = -1;
        panelPrefab  = Resources.Load<GameObject>(panelPrefabPath);
        ShowNextPanel();
    }

    void Update()
    {
        if (hasSeenIntro) return;

        bool mouseDown = Input.GetMouseButtonDown(0);
        bool touchDown = (Input.touchCount > 0 &&
                        Input.GetTouch(0).phase == TouchPhase.Began);

        // advance only if the tap/click is NOT over a UI element
        if ((mouseDown || touchDown) && !IsPointerOverUI())
            ShowNextPanel();
    }

    /* ───────────────────────────────────────────── */
    public void ShowNextPanel()
    {
        if (hasSeenIntro) return;

        StopAllCoroutines();
        StartCoroutine(Transition());
    }

    IEnumerator Transition()
    {
        /* fade-out & destroy previous */
        if (currentPanel != null)
        {
            yield return FadeOut(currentPanel);
            Destroy(currentPanel);
        }

        /* ---------- advance index ---------- */
        currentIndex++;
        if (currentIndex >= panels.Length) { EndIntro(); yield break; }

        if (panelPrefab == null) yield break;

        /* ---------- build new panel ---------- */
        currentPanel = Instantiate(panelPrefab, transform);
        var cg = currentPanel.GetComponentInChildren<CanvasGroup>();
        if (cg != null) cg.alpha = 0f;

        backgroundImage = currentPanel.transform.Find("Canvas/Background")?.GetComponent<Image>();
        characterImage  = currentPanel.transform.Find("Canvas/Character") ?.GetComponent<Image>();
        dialogueText    = currentPanel.transform.Find("Canvas/DialogueText")?.GetComponent<TMP_Text>();

        var skip = currentPanel.GetComponentInChildren<Button>();
        if (skip != null) skip.onClick.AddListener(Skip);

        /* ---------- populate ---------- */
        var data = panels[currentIndex];

        if (backgroundImage != null) backgroundImage.sprite = data.background;

        if (characterImage != null)
        {
            if (data.character != null)
            {
                characterImage.sprite  = data.character;
                characterImage.enabled = true;
            }
            else  characterImage.enabled = false;
        }

        if (dialogueText != null)
        {
            dialogueText.text = data.text;
            dialogueText.maxVisibleCharacters = 0;
            PrepareAutoSize(dialogueText);          // << NEW
        }

        /* ---------- run transitions ---------- */
        yield return FadeIn(currentPanel);
        if (dialogueText != null)
            yield return RevealText(dialogueText);

        yield return new WaitForSeconds(data.holdTime);
        ShowNextPanel();
    }

    /* ───────── helpers ───────── */

    /// Uses TMP’s auto-size once, then locks the result so
    /// the font size stays constant during the typing effect.
    void PrepareAutoSize(TMP_Text txt)
    {
        txt.enableAutoSizing = true;
        txt.fontSizeMin      = minFontSize;
        txt.fontSizeMax      = maxFontSize;

        txt.maxVisibleCharacters = int.MaxValue;   // show full text
        txt.ForceMeshUpdate();                     // let TMP calculate
        float fittedSize = txt.fontSize;           // read result

        txt.enableAutoSizing    = false;           // lock it
        txt.fontSize            = fittedSize;
        txt.maxVisibleCharacters = 0;              // hide again
    }

    IEnumerator FadeOut(GameObject go)
    {
        var cg = go.GetComponentInChildren<CanvasGroup>();
        if (cg == null) yield break;

        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            cg.alpha = 1f - t;
            yield return null;
        }
        cg.alpha = 0f;
    }

    IEnumerator FadeIn(GameObject go)
    {
        var cg = go.GetComponentInChildren<CanvasGroup>();
        if (cg == null) yield break;

        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            cg.alpha = t;
            yield return null;
        }
        cg.alpha = 1f;
    }

    public void Skip()
    {
        currentIndex = panels.Length - 1;
        ShowNextPanel();
    }
    bool IsPointerOverUI()
    {
    #if UNITY_EDITOR || UNITY_STANDALONE
        return EventSystem.current.IsPointerOverGameObject();
    #else                                   // touch devices
        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(
                Input.GetTouch(0).fingerId);
        return false;
    #endif
    }

    IEnumerator RevealText(TMP_Text txt)
    {
        txt.ForceMeshUpdate();
        int total = txt.textInfo.characterCount;

        for (int i = 0; i <= total; i++)
        {
            txt.maxVisibleCharacters = i;
            yield return new WaitForSeconds(0.02f);
        }
    }

    /* ───────────────────────────────────────────── */
    void EndIntro()
    {
        PlayerPrefs.SetInt("IntroSeen", 1);
        PlayerPrefs.Save();
        hasSeenIntro = true;

        SceneManager.LoadScene("DemoScene");
        TutorialManager.CreateIfNeeded();
    }
}
