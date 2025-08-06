// TutorialManager.cs – tag-driven landmarks, HUD explanations, and zoom-in camera pans
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

    [Header("Landmark Tags")]
    [SerializeField] private string houseTag = "House";
    [SerializeField] private string workTag  = "Work";
    [SerializeField] private string foodTag  = "Food";

    [Header("Camera")]
    [SerializeField] private Camera tutorialCamera;       // optional override; MainCamera if null
    [SerializeField] private float cameraMoveTime = 2f;   // seconds per pan
    [SerializeField] private float zoomInDistance = 5f;   // extra metres to move forward
    [SerializeField] private float zoomInTime = 0.6f;     // seconds for zoom-in

    [Header("Isometric Settings")]
    [SerializeField] private float isoPitch    = 30f;
    [SerializeField] private float isoYaw      = 45f;
    [SerializeField] private float isoDistance = 30f;

    [Header("Tutorial UI")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private Text tutorialText;

    /* ─────────────────────────────── Runtime Helpers ─────────────────────────────── */
    readonly List<GameObject> overlayObjects     = new();
    readonly List<Behaviour>  disabledBehaviours = new();

    Transform houseTarget, workTarget, foodTarget;

    /* ─────────────────────────────── Unity Hooks ─────────────────────────────── */
    void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    void Start()    => RebindLandmarks();
    void OnSceneLoaded(Scene sc, LoadSceneMode mode) => RebindLandmarks();

    /* ─────────────────────────────── Public Entry ─────────────────────────────── */
    public void ShowTutorial()
    {
        gameObject.SetActive(true);
        StartCoroutine(RunTutorial());
    }

    /* ─────────────────────────────── Main Coroutine ─────────────────────────────── */
    IEnumerator RunTutorial()
    {
        RebindLandmarks();                    // ensure tags resolved

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
        if (target)
        {
            Debug.Log(
                $"[Tutorial] About to pan:\n" +
                $" • target = \"{target.name}\" (scene = \"{target.gameObject.scene.name}\")\n" +
                $" • hierarchy = {PathInHierarchy(target)}\n" +
                $" • local   = {target.localPosition}\n" +
                $" • world   = {target.position}"
            );
        }

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

        Debug.Log($"[Tutorial] Target \"{target.name}\" @ {target.position} ⇒ desired camera pos {desiredPos}, rot {desiredRot.eulerAngles}");

        float elapsed = 0f;
        Camera activeCam = PrepareCamera();
        if (!activeCam) yield break;

        Vector3 startPos = activeCam.transform.position;
        Quaternion startRot = activeCam.transform.rotation;
        Debug.Log($"[Tutorial] Panning from {startPos} to {desiredPos}");

        while (elapsed < cameraMoveTime)
        {
            /* handle camera destruction or replacement */
            if (activeCam == null)
            {
                activeCam = PrepareCamera();
                if (!activeCam) yield break;
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
            yield return ZoomIn(activeCam, desiredRot);
        }
    }

    /* small forward zoom after arriving */
    IEnumerator ZoomIn(Camera cam, Quaternion lookRot)
    {
        if (!cam || zoomInDistance <= 0f || zoomInTime <= 0f) yield break;

        Vector3 startPos = cam.transform.position;
        Vector3 endPos   = startPos + cam.transform.forward * (-zoomInDistance);

        float elapsed = 0f;
        while (elapsed < zoomInTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / zoomInTime);
            cam.transform.position = Vector3.Lerp(startPos, endPos, t);
            cam.transform.rotation = lookRot;       // keep rotation locked
            yield return null;
        }
        cam.transform.position = endPos;
    }

    /* ─────────────────────────────── HUD highlight ─────────────────────────────── */
    IEnumerator ShowHudHighlights()
    {
        var hud = StatsHUD.CreateIfNeeded();
        if (!hud) yield break;

        // ① Money
        yield return new WaitUntil(() => hud.MoneyTextTransform != null);
        yield return HighlightWithDialogue(
            hud.MoneyTextTransform,
            "Money shows how many coins you have. Earn more by working!"
        );

        // ② Hunger
        yield return new WaitUntil(() => hud.HungerBarTransform != null);
        yield return HighlightWithDialogue(
            hud.HungerBarTransform,
            "Hunger decreases while you work and refills when you eat food."
        );

        // ③ Fatigue
        yield return new WaitUntil(() => hud.FatigueBarTransform != null);
        yield return HighlightWithDialogue(
            hud.FatigueBarTransform,
            "Fatigue rises as you work and drops again when you rest."
        );
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
        yield return new WaitUntil(() => Input.GetMouseButtonDown(0) ||
               (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began));
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
    string PathInHierarchy(Transform t)
    {
        if (!t) return "<null>";
        string path = t.name;
        while (t.parent) { t = t.parent; path = $"{t.name}/{path}"; }
        return path;
    }

    /* ─────────────────────────────── Camera utilities ─────────────────────────────── */
    Camera PrepareCamera()
    {
        Camera cam = tutorialCamera ? tutorialCamera :
                     (Camera.main ? Camera.main :
                     (GameObject.Find("Main Camera")?.GetComponent<Camera>()));
        if (!cam) return null;

        Debug.Log($"[Tutorial] Using camera \"{cam.name}\" @ {cam.transform.position}");

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
        if (tutorialPanel && tutorialPanel.activeSelf)
            tutorialPanel.SetActive(false);

        gameObject.SetActive(false);
    }

    /* ─────────────────────────────── Landmark re-binding ─────────────────────────────── */
    void RebindLandmarks()
    {
        bool NeedsRebind(Transform t) =>
            !t || !t.gameObject.activeInHierarchy || t.gameObject.scene.name == "DontDestroyOnLoad";

        if (NeedsRebind(houseTarget))
            houseTarget = GameObject.FindWithTag(houseTag)?.transform;

        if (NeedsRebind(workTarget))
            workTarget  = GameObject.FindWithTag(workTag)?.transform;

        if (NeedsRebind(foodTarget))
            foodTarget  = GameObject.FindWithTag(foodTag)?.transform;
    }

    /* ─────────────────────────────── Bootstrap helper (unchanged) ─────────────────────────────── */
    public static TutorialManager CreateIfNeeded()
    {
        bool seen = PlayerPrefs.GetInt("TutorialSeen", 0) != 0;
        if (Instance != null)
        {
            if (!seen) Instance.ShowTutorial();
            else Instance.gameObject.SetActive(false);
            return Instance;
        }

        var existing = FindObjectOfType<TutorialManager>();
        if (existing)
        {
            Instance = existing;
            if (!seen) existing.ShowTutorial();
            else existing.gameObject.SetActive(false);
            return existing;
        }

        var prefab = Resources.Load<TutorialManager>("Prefabs/TutorialManager");
        if (prefab)
        {
            var mgr = Instantiate(prefab);
            DontDestroyOnLoad(mgr.gameObject);
            Instance = mgr;
            if (!seen) mgr.ShowTutorial();
            else mgr.gameObject.SetActive(false);
            return mgr;
        }

        var go = new GameObject("TutorialManager");
        DontDestroyOnLoad(go);
        var newMgr = go.AddComponent<TutorialManager>();
        Instance = newMgr;
        if (!seen) newMgr.ShowTutorial();
        else newMgr.gameObject.SetActive(false);
        return newMgr;
    }
}
