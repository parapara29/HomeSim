using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public struct PanelData
{
    public Sprite background;
    public string text;
    public float holdTime;
}

public class IntroSequence : MonoBehaviour
{
    [SerializeField] PanelData[] panels;
    [SerializeField] string panelPrefabPath = "Prefabs/IntroPanel";

    int currentIndex;
    bool hasSeenIntro = PlayerPrefs.GetInt("IntroSeen", 0) == 1;

    GameObject currentPanel;
    GameObject panelPrefab;

    void Start()
    {
        if (hasSeenIntro)
        {
            gameObject.SetActive(false);
            return;
        }

        currentIndex = -1;
        panelPrefab = Resources.Load<GameObject>(panelPrefabPath);
        ShowNextPanel();
    }

    void Update()
    {
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
        var cg = currentPanel.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 0f;

        var data = panels[currentIndex];
        var img = currentPanel.GetComponentInChildren<Image>();
        if (img != null) img.sprite = data.background;

        var txt = currentPanel.GetComponentInChildren<TMP_Text>();
        if (txt != null) txt.text = data.text;

        var skip = currentPanel.GetComponentInChildren<Button>();
        if (skip != null) skip.onClick.AddListener(Skip);

        yield return FadeIn(currentPanel);
        yield return new WaitForSeconds(data.holdTime);
    }

    IEnumerator FadeOut(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
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
        var cg = go.GetComponent<CanvasGroup>();
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

    void EndIntro()
    {
        PlayerPrefs.SetInt("IntroSeen", 1);
        PlayerPrefs.Save();
        SendMessage("StartGuidance", SendMessageOptions.DontRequireReceiver);
        gameObject.SetActive(false);
    }
}