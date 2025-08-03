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
    }
}