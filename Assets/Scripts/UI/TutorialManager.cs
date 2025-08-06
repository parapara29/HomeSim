using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    [Tooltip("Optional list of highlight or overlay objects spawned during the tutorial")]
    [SerializeField] private List<GameObject> overlayObjects = new List<GameObject>();

    [Tooltip("Components that were disabled while the tutorial ran and should be re-enabled when finished")]
    [SerializeField] private List<Behaviour> disabledUIElements = new List<Behaviour>();

    [Tooltip("Player control script that should be re-enabled after the tutorial")]
    [SerializeField] private MonoBehaviour playerController;

    [Header("Camera Tour")]
    [SerializeField] private Camera tutorialCamera;
    [SerializeField] private Transform houseTarget;
    [SerializeField] private Transform workTarget;
    [SerializeField] private Transform foodTarget;
    [SerializeField] private float cameraMoveTime = 2f;

    [Header("Tutorial UI")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private Text tutorialText;

    /// <summary>
    /// Runs the tutorial sequence.  At the end of the tutorial we clean up any
    /// temporary visual elements, record that the player has seen the tutorial,
    /// and return control back to the player.
    /// </summary>
    public void ShowTutorial()
    {
        StartCoroutine(RunTutorial());
    }

    IEnumerator RunTutorial()
    {
        if (playerController != null)
            playerController.enabled = false;

        Camera cam = tutorialCamera != null ? tutorialCamera : Camera.main;

        yield return PanToTarget(cam, houseTarget, "This is your house.");
        yield return PanToTarget(cam, workTarget, "This is where you work.");
        yield return PanToTarget(cam, foodTarget, "Here you can get food.");

        var hud = StatsHUD.Instance;
        if (hud != null)
        {
            var highlight = CreateHighlight(hud.MoneyTextTransform);
            ShowDialogue("This shows how much money you have.");
            yield return WaitForClick();
            HideDialogue();
            if (highlight != null) Destroy(highlight);

            highlight = CreateHighlight(hud.HungerBarTransform);
            ShowDialogue("This shows your hunger level.");
            yield return WaitForClick();
            HideDialogue();
            if (highlight != null) Destroy(highlight);

            highlight = CreateHighlight(hud.FatigueBarTransform);
            ShowDialogue("This shows your fatigue level.");
            yield return WaitForClick();
            HideDialogue();
            if (highlight != null) Destroy(highlight);
        }
        

        EndTutorial();
    }

    IEnumerator PanToTarget(Camera cam, Transform target, string message)
    {
        if (cam == null || target == null)
            yield break;

        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;
        Vector3 endPos = target.position;
        Quaternion endRot = target.rotation;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / cameraMoveTime;
            cam.transform.position = Vector3.Lerp(startPos, endPos, t);
            cam.transform.rotation = Quaternion.Lerp(startRot, endRot, t);
            yield return null;
        }

        ShowDialogue(message);
        yield return WaitForClick();
        HideDialogue();
    }

    void ShowDialogue(string message)
    {
        if (tutorialText != null) tutorialText.text = message;
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
    }

    void HideDialogue()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
    }

    IEnumerator WaitForClick()
    {
        while (!Input.GetMouseButtonDown(0) && Input.touchCount == 0)
            yield return null;
    }

    public GameObject CreateHighlight(Transform target)
    {
        if (target == null) return null;

        var overlay = new GameObject("TutorialHighlight", typeof(RectTransform), typeof(Image));
        var rect = overlay.GetComponent<RectTransform>();
        rect.SetParent(target, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        overlay.transform.SetAsLastSibling();

        var img = overlay.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.25f);
        img.raycastTarget = false;

        overlayObjects.Add(overlay);
        return overlay;
    }
    void HighlightElement(RectTransform target)
    {
        if (target == null) return;

        var overlay = new GameObject("Overlay", typeof(RectTransform), typeof(Image), typeof(Outline));
        overlay.transform.SetParent(target, false);
        var rect = overlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var img = overlay.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        var outline = overlay.GetComponent<Outline>();
        outline.effectColor = Color.yellow;
        outline.effectDistance = new Vector2(5f, 5f);

        overlayObjects.Add(overlay);
    }
    /// <summary>
    /// Cleans up tutorial state and hands control back to the player.
    /// </summary>
    void EndTutorial()
    {
        // Remove any highlights/overlays that were created for the tutorial
        foreach (var overlay in overlayObjects)
        {
            if (overlay != null)
            {
                Destroy(overlay);
            }
        }
        overlayObjects.Clear();

        // Re-enable any UI elements that were disabled for the tutorial
        foreach (var ui in disabledUIElements)
        {
            if (ui != null)
            {
                ui.enabled = true;
            }
        }
        disabledUIElements.Clear();

        // Store that the tutorial has been seen so it doesn't run again
        PlayerPrefs.SetInt("TutorialSeen", 1);
        PlayerPrefs.Save();

        // Hand control back to the player
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        // Camera remains in its current position; no explicit changes needed
    }
    public static TutorialManager CreateIfNeeded()
    {
        if (Instance != null)
        {
            if (PlayerPrefs.GetInt("TutorialSeen", 0) == 0)
            {
                Instance.ShowTutorial();
            }
            return Instance;
        }

        var existing = FindObjectOfType<TutorialManager>();
        if (existing != null)
        {
            Instance = existing;
            if (PlayerPrefs.GetInt("TutorialSeen", 0) == 0)
            {
                Instance.ShowTutorial();
            }
            return Instance;
        }
        var prefab = Resources.Load<TutorialManager>("Prefabs/TutorialManager");
        if (prefab != null)
        {
            var manager = Instantiate(prefab);
            DontDestroyOnLoad(manager.gameObject);
            Instance = manager;
            if (PlayerPrefs.GetInt("TutorialSeen", 0) == 0)
            {
                manager.ShowTutorial();
            }
            return manager;
        }
        var go = new GameObject("TutorialManager");
        DontDestroyOnLoad(go);
        var newManager = go.AddComponent<TutorialManager>();
        Instance = newManager;
        if (PlayerPrefs.GetInt("TutorialSeen", 0) == 0)
        {
            newManager.ShowTutorial();
        }
        return newManager;
    }
}