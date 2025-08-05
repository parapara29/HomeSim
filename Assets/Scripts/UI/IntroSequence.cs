using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

[System.Serializable]
public struct PanelData
{
    public Sprite background;
    public Sprite character;
    public string text;
    public float holdTime;
}

public class IntroSequence : MonoBehaviour
{
    [SerializeField] PanelData[] panels;
    [SerializeField] string panelPrefabPath = "Prefabs/IntroPanel";

    int currentIndex;
    bool hasSeenIntro;

    GameObject currentPanel;
    GameObject panelPrefab;
    Image backgroundImage;
    Image characterImage;
    TMP_Text dialogueText;

    void Awake()
    {
        // Safe place for PlayerPrefs
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
        panelPrefab = Resources.Load<GameObject>(panelPrefabPath);
        ShowNextPanel();
    }

    void Update()
    {
        if (hasSeenIntro)
            return;

        if (Input.GetMouseButtonDown(0) ||
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            ShowNextPanel();
        }
    }

    public void ShowNextPanel()
    {
        if (hasSeenIntro)
            return;

        StopAllCoroutines();
        StartCoroutine(Transition());
    }

    IEnumerator Transition()
    {
        if (currentPanel != null)
        {
            yield return FadeOut(currentPanel);
            Destroy(currentPanel);
        }

        currentIndex++;
        if (currentIndex >= panels.Length)
        {
            EndIntro();
            yield break;
        }

        if (panelPrefab == null)
            yield break;

        currentPanel = Instantiate(panelPrefab, transform);
        var cg = currentPanel.GetComponentInChildren<CanvasGroup>();
        if (cg != null) cg.alpha = 0f;

        backgroundImage = currentPanel.transform.Find("Background")?.GetComponent<Image>();
        characterImage = currentPanel.transform.Find("Character")?.GetComponent<Image>();
        dialogueText = currentPanel.transform.Find("DialogueText")?.GetComponent<TMP_Text>();

        var skip = currentPanel.GetComponentInChildren<Button>();
        if (skip != null) skip.onClick.AddListener(Skip);

        var data = panels[currentIndex];
        if (backgroundImage != null) backgroundImage.sprite = data.background;
        if (characterImage != null) characterImage.sprite = data.character;
        if (dialogueText != null)
        {
            dialogueText.text = data.text;
            dialogueText.maxVisibleCharacters = 0;
        }

        yield return FadeIn(currentPanel);
        if (dialogueText != null)
            yield return RevealText(dialogueText);
        yield return new WaitForSeconds(data.holdTime);
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

    void EndIntro()
    {
        PlayerPrefs.SetInt("IntroSeen", 1);
        PlayerPrefs.Save();
        hasSeenIntro = true;
        SceneManager.LoadScene("DemoScene");
        StatsHUD.CreateIfNeeded();
    }
}