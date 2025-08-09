using UnityEngine;
using System;

/// <summary>
/// Handles the progression of in‑game days and applies daily checks that
/// influence the player's suspicion level. At the start of each session and
/// whenever a new day has begun, this manager will run end‑of‑day logic to
/// determine whether the player has performed required human actions such as
/// purchasing essential furniture or showering. Violations increase the
/// suspicion meter. This component persists across scenes.
/// </summary>
public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    private const string LastDayKey = "LastDayCheck";
    private const string DaysSinceShowerKey = "DaysSinceShower";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        // Immediately perform a day check on startup in case days have passed
        CheckForNewDay();
    }

    /// <summary>
    /// Creates a DayManager if one does not yet exist. Called automatically
    /// before the first scene loads.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        CreateIfNeeded();
    }

    public static DayManager CreateIfNeeded()
    {
        if (Instance == null)
        {
            var go = new GameObject("DayManager");
            go.AddComponent<DayManager>();
        }
        return Instance;
    }

    /// <summary>
    /// Resets the shower timer. Should be called when the player uses the
    /// shower to perform basic hygiene. Resets days since last shower back to 0.
    /// </summary>
    public void ResetShowerTimer()
    {
        PlayerPrefs.SetInt(DaysSinceShowerKey, 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Checks if a new day has begun since the last recorded date. If one or
    /// more days have passed, runs the end‑of‑day logic for each day elapsed.
    /// This method can be called at any time to synchronise the daily loop.
    /// </summary>
    public void CheckForNewDay()
    {
        DateTime today = DateTime.Now.Date;
        string saved = PlayerPrefs.GetString(LastDayKey, string.Empty);
        DateTime lastDate;
        if (string.IsNullOrEmpty(saved) || !DateTime.TryParse(saved, out lastDate))
        {
            // If no previous record exists, set it to today
            PlayerPrefs.SetString(LastDayKey, today.ToString("yyyy-MM-dd"));
            PlayerPrefs.Save();
            return;
        }
        // If the current date is later than the last recorded date
        if (today > lastDate)
        {
            int daysPassed = (int)(today - lastDate).TotalDays;
            for (int i = 0; i < daysPassed; i++)
            {
                ProcessEndOfDay();
            }
            PlayerPrefs.SetString(LastDayKey, today.ToString("yyyy-MM-dd"));
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Executes logic at the end of each day. Increases suspicion if essential
    /// furniture is missing and processes shower hygiene. Also logs any
    /// suspicious findings to Agent Miller's log.
    /// </summary>
    private void ProcessEndOfDay()
    {
        var stats = PlayerStats.Instance;
        if (stats == null) return;

        // Apply daily suspicion decay – reduce suspicion slightly each day
        // This encourages players to behave normally over time and recover from
        // past mistakes. The decay is small and capped at zero.
        if (stats.Suspicion > 0f)
        {
            // Reduce by 2% per day
            stats.ChangeSuspicion(-0.02f);
        }

        // Check for essential furniture using the utility to handle amplification
        if (!HasFurniture("bed"))
        {
            SuspicionUtils.ApplySuspicion(stats, 0.1f, "No bed detected in apartment. Subject appears neglectful.");
        }
        if (!HasFurniture("shower"))
        {
            SuspicionUtils.ApplySuspicion(stats, 0.1f, "No shower detected in apartment. Subject lacks basic hygiene facilities.");
        }

        // Shower timer: increment days since last shower
        int daysSinceShower = PlayerPrefs.GetInt(DaysSinceShowerKey, 0);
        daysSinceShower++;
        // After two days without shower, suspicion rises each day
        if (daysSinceShower > 2)
        {
            // Increase suspicion gradually; each additional day adds 5% suspicion
            float delta = 0.05f * (daysSinceShower - 2);
            SuspicionUtils.ApplySuspicion(stats, delta, $"Subject has not showered for {daysSinceShower} days. Public nuisance.");
        }
        PlayerPrefs.SetInt(DaysSinceShowerKey, daysSinceShower);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Checks across all saved rooms whether a piece of furniture whose
    /// prefab name contains the given search substring exists. This is used
    /// to detect whether a bed or shower has been purchased. Case insensitive.
    /// </summary>
    private bool HasFurniture(string search)
    {
        if (string.IsNullOrEmpty(search)) return false;
        search = search.ToLowerInvariant();
        // The player may have multiple rooms saved. We'll check the two known
        // room types: the default "Room" and the "Kitchen". New room types can
        // be added here as the game expands.
        string[] rooms = { "Room", "Kitchen" };
        foreach (var roomName in rooms)
        {
            var items = RoomSave.Load(roomName);
            foreach (var item in items)
            {
                if (item != null && !string.IsNullOrEmpty(item.prefab) && item.prefab.ToLowerInvariant().Contains(search))
                {
                    return true;
                }
            }
        }
        return false;
    }
}