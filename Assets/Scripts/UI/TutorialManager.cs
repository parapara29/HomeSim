// TutorialManager.cs – robust camera reacquisition & continuous motion after scene changes
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    /* ───────────────────────────────────── Singleton ───────────────────────────────────── */
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

    /* ─────────────────────────────── Serialized Fields ─────────────────────────────── */
    [SerializeField] private MonoBehaviour playerController;

    [Header("Landmarks (assign in Inspector)")]
    [SerializeField] private Transform houseTarget;
    [SerializeField] private Transform workTarget;
    [SerializeField] private Transform foodTarget;

    [Header("Camera")]
    [SerializeField] private Camera tutorialCamera;       // optional override; MainCamera if null
    [SerializeField] private float cameraMoveTime = 2f;   // seconds per pan

    [Header("Isometric Settings")]
    [SerializeField] private float isoPitch    = 30f;
    [SerializeField] private float isoYaw      = 45f;
    [SerializeField] private float isoDistance = 30f;

    [Header("Tutorial UI")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private Text tutorialText;

    /* ─────────────────────────────── Runtime Helpers ─────────────────────────────── */
    readonly List<GameObject> overlayObjects   = new();
    readonly List<Behaviour>  disabledBehaviours = new();

    /* ─────────────────────────────── Public Entry ─────────────────────────────── */
    public void ShowTutorial() => StartCoroutine(RunTutorial());

    /* ─────────────────────────────── Main Coroutine ─────────────────────────────── */
    IEnumerator RunTutorial()
    {
        if (playerController) playerController.enabled = false;

        Camera cam = PrepareCamera();
        if (!cam)
        {
            Debug.LogWarning("TutorialManager: No active camera found");
            yield break;
        }

        /* Tour */
        yield return PanAndExplain(houseTarget, "This is your house.");
        yield return PanAndExplain(workTarget,  "This is where you work.");
        yield return PanAndExplain(foodTarget,  "Here you can get food.");

        /* HUD highlights */
        yield return ShowHudHighlights();

        /* Final message */
        ShowDialogue("Have Fun!");
        yield return WaitForClick();
        HideDialogue();

        EndTutorial();
    }

    /* ─────────────────────────────── Pan helpers ─────────────────────────────── */
    IEnumerator PanAndExplain(Transform target, string message)
    {
        yield return PanToTarget(target);
        ShowDialogue(message);
        yield return WaitForClick();
        HideDialogue();
    }

    IEnumerator PanToTarget(Transform target)
    {
        if (!target) yield break;

        /* compute desired iso pose once */
        Quaternion isoRot = Quaternion.Euler(isoPitch, isoYaw, 0f);
        Vector3 desiredPos = target.position + isoRot * Vector3.back * isoDistance;
        Quaternion desiredRot = Quaternion.LookRotation(target.position - desiredPos, Vector3.up);

        float elapsed = 0f;
        Camera activeCam = PrepareCamera();
        if (!activeCam) yield break;

        Vector3 startPos = activeCam.transform.position;
        Quaternion startRot = activeCam.transform.rotation;
        Debug.Log($"Panning from {startPos} to {desiredPos}  (target = {target.position})");

        while (elapsed < cameraMoveTime)
        {
            /* handle camera destruction or replacement */
            if (activeCam == null)
            {
                activeCam = PrepareCamera();
                if (!activeCam) yield break;   // still missing – abort
                // re‑anchor interpolation to new camera pose, keep elapsed progress
                startPos = activeCam.transform.position;
                startRot = activeCam.transform.rotation;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / cameraMoveTime);

            Vector3 newPos = Vector3.Lerp(startPos, desiredPos, t);
            Quaternion newRot = Quaternion.Slerp(startRot, desiredRot, t);
            activeCam.transform.SetPositionAndRotation(newPos, newRot);

            yield return null;
        }

        if (activeCam)
        {
            activeCam.transform.SetPositionAndRotation(desiredPos, desiredRot);
        }
    }

    /* ─────────────────────────────── HUD highlight ─────────────────────────────── */
    IEnumerator ShowHudHighlights()
    {
        var hud = StatsHUD.Instance;
        if (!hud) yield break;

        yield return HighlightWithDialogue(hud.MoneyTextTransform,  "This shows how much money you have.");
        yield return HighlightWithDialogue(hud.HungerBarTransform, "This shows your hunger level.");
        yield return HighlightWithDialogue(hud.FatigueBarTransform,"This shows your fatigue level.");
    }

    IEnumerator HighlightWithDialogue(Transform target, string message)
    {
        var hl = CreateHighlight(target);
        ShowDialogue(message);
        yield return WaitForClick();
        HideDialogue();
        if (hl) Destroy(hl);
    }

    /* ─────────────────────────────── UI helpers ─────────────────────────────── */
    void ShowDialogue(string msg)
    {
        if (tutorialText)  tutorialText.text = msg;
        if (tutorialPanel) tutorialPanel.SetActive(true);
    }
    void HideDialogue() { if (tutorialPanel) tutorialPanel.SetActive(false); }

    IEnumerator WaitForClick()
    {
        yield return new WaitUntil(() => Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began));
    }

    /* ─────────────────────────────── Overlay helpers ─────────────────────────────── */
    GameObject CreateHighlight(Transform target)
    {
        if (!target) return null;
        var go = new GameObject("TutorialHighlight", typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(target, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.transform.SetAsLastSibling();
        var img = go.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.25f);
        img.raycastTarget = false;
        overlayObjects.Add(go);
        return go;
    }

    /* ─────────────────────────────── Camera utilities ─────────────────────────────── */
    Camera PrepareCamera()
    {
        Camera cam = tutorialCamera ? tutorialCamera : (Camera.main ? Camera.main : (GameObject.Find("Main Camera")?.GetComponent<Camera>()));
        if (!cam) return null;

        // disable any motion scripts once per camera instance
        foreach (var b in cam.GetComponents<Behaviour>())
        {
            if (b != cam && b.enabled)
            {
                b.enabled = false;
                disabledBehaviours.Add(b);
            }
        }
        return cam;
    }

    /* ─────────────────────────────── Cleanup ─────────────────────────────── */
    void EndTutorial()
    {
        foreach (var o in overlayObjects) if (o) Destroy(o);
        overlayObjects.Clear();

        foreach (var beh in disabledBehaviours) if (beh) beh.enabled = true;
        disabledBehaviours.Clear();

        PlayerPrefs.SetInt("TutorialSeen", 1);
        PlayerPrefs.Save();

        if (playerController) playerController.enabled = true;
    }

    /* ─────────────────────────────── Bootstrap helper ─────────────────────────────── */
    public static TutorialManager CreateIfNeeded()
    {
        if (Instance != null)
        {
            if (PlayerPrefs.GetInt("TutorialSeen", 0) == 0) Instance.ShowTutorial();
            return Instance;
        }

        var existing = FindObjectOfType<TutorialManager>();
        if (existing)
        {
            Instance = existing;
            if (PlayerPrefs.GetInt("TutorialSeen", 0) == 0) existing.ShowTutorial();
            return existing;
        }

        var prefab = Resources.Load<TutorialManager>("Prefabs/TutorialManager");
        if (prefab)
        {
            var mgr = Instantiate(prefab);
            DontDestroyOnLoad(mgr.gameObject);
            Instance = mgr;
            if (PlayerPrefs.GetInt("TutorialSeen", 0) == 0) mgr.ShowTutorial();
            return mgr;
        }

        var go = new GameObject("TutorialManager");
        DontDestroyOnLoad(go);
        var newMgr = go.AddComponent<TutorialManager>();
        Instance = newMgr;
        if (PlayerPrefs.GetInt("TutorialSeen", 0) == 0) newMgr.ShowTutorial();
        return newMgr;
    }
}
