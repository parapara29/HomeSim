using UnityEngine;

[DefaultExecutionOrder(-1000)]   // run before everyone else
public class SceneSetup : MonoBehaviour
{
    [Tooltip("Show the cursor in this scene?")]
    [SerializeField] bool showCursor = true;

    void Awake()
    {
        // ───────── Cursor ─────────
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = showCursor;

        // ───────── TimeScale (safety) ─────────
        if (Time.timeScale == 0) Time.timeScale = 1f;

        // ───────── HUD & Player Stats ─────────
        PlayerStats.CreateIfNeeded();

        // ───────── Tutorial cleanup ─────────
        // If the tutorial has been completed, proactively remove any leftover
        // TutorialManager instances that may still exist in the scene or in the
        // DontDestroyOnLoad area. Without this, a TutorialManager prefab baked
        // into a scene can survive across scene transitions even when the
        // tutorial is no longer required, causing UI artifacts to appear in the
        // DemoScene. This check runs at very early execution order to ensure
        // managers are purged before other scripts query TutorialManager.Instance.
        bool tutorialSeen = UnityEngine.PlayerPrefs.GetInt("TutorialSeen", 0) != 0;
        if (tutorialSeen)
        {
            var mgrs = FindObjectsOfType<TutorialManager>(true);
            foreach (var mgr in mgrs)
            {
                if (mgr != null)
                {
                    Destroy(mgr.gameObject);
                }
            }
            // Reset of the static Instance will happen in TutorialManager.OnDestroy
            // when the managers are destroyed above. Do not attempt to set it
            // here because the property setter is private.
        }
    }
}