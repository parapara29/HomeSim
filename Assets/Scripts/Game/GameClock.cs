using UnityEngine;

/// <summary>
/// Maintains a simple in‑game clock and day counter. Time in HomeSim is
/// decoupled from real time and instead advances whenever the player
/// performs actions that consume hours, such as working or resting. The
/// clock begins at 06:00 on day one and wraps around at 24 hours. When
/// a full 24‑hour cycle elapses, the day counter increments and the
/// end‑of‑day logic is triggered via DayManager. This component
/// persists across scenes and exposes helper methods for formatting
/// time and retrieving the current hour and day strings.
/// </summary>
public class GameClock : MonoBehaviour
{
    public static GameClock Instance { get; private set; }

    // Current time in hours (0–24). Fractional parts represent minutes.
    private float currentHour = 6f;
    // Current day count starting from 1
    private int currentDay = 1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        // Load persisted time from PlayerPrefs if present. Use sensible
        // defaults of 6:00 on day 1 when first starting the game. We use
        // separate keys for the hour (float) and day (int). The keys
        // "GameClock.CurrentHour" and "GameClock.CurrentDay" are namespaced
        // to avoid collisions with other preferences.
        currentHour = PlayerPrefs.GetFloat("GameClock.CurrentHour", 6f);
        currentDay  = PlayerPrefs.GetInt("GameClock.CurrentDay", 1);
    }

    /// <summary>
    /// Creates a GameClock if one does not yet exist. Called automatically
    /// before the first scene loads. Use this instead of explicitly
    /// instantiating a clock from other scripts.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        CreateIfNeeded();
    }

    public static GameClock CreateIfNeeded()
    {
        if (Instance == null)
        {
            var go = new GameObject("GameClock");
            go.AddComponent<GameClock>();
        }
        return Instance;
    }

    /// <summary>
    /// Advances the in‑game clock by the specified number of hours. If the
    /// clock wraps past 24:00, days are incremented and the DayManager
    /// end‑of‑day logic is invoked via reflection. Fractions are supported.
    /// </summary>
    public void AdvanceTime(float hours)
    {
        if (hours <= 0f) return;
        currentHour += hours;
        while (currentHour >= 24f)
        {
            currentHour -= 24f;
            currentDay++;
            // Use reflection to invoke the private ProcessEndOfDay() on DayManager
            if (DayManager.Instance != null)
            {
                var dmType = typeof(DayManager);
                var method = dmType.GetMethod("ProcessEndOfDay", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (method != null)
                {
                    method.Invoke(DayManager.Instance, null);
                }
            }
        }
    }

    /// <summary>
    /// Returns the current time formatted as HH:mm. Rounds minutes to the
    /// nearest whole minute based on the fractional part of the hour.
    /// </summary>
    public string GetTimeString()
    {
        int hour   = Mathf.FloorToInt(currentHour);
        int minute = Mathf.RoundToInt((currentHour - hour) * 60f);
        if (minute >= 60)
        {
            minute = 0;
            hour++;
            if (hour >= 24) hour -= 24;
        }
        return string.Format("{0:00}:{1:00}", hour, minute);
    }

    /// <summary>
    /// Returns the current day as a string, e.g. "Day 1".
    /// </summary>
    public string GetDayString()
    {
        return $"Day {currentDay}";
    }

    /// <summary>
    /// Returns the current hour as an integer from 0 to 23.
    /// </summary>
    public int GetCurrentHour()
    {
        return Mathf.FloorToInt(currentHour) % 24;
    }

    void OnDestroy()
    {
        // Persist the current in-game time and day when the clock is
        // destroyed (e.g. when exiting the application). Saving here
        // ensures that time resumes from the last point when the game
        // is reloaded. Unity calls OnDestroy on objects marked as
        // DontDestroyOnLoad when the application quits.
        PlayerPrefs.SetFloat("GameClock.CurrentHour", currentHour);
        PlayerPrefs.SetInt("GameClock.CurrentDay", currentDay);
        PlayerPrefs.Save();
    }
}