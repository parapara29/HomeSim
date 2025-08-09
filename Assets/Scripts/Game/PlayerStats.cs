using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }
    const string MoneyKey = "PlayerMoney";
    [SerializeField] private int money = 500;
    [SerializeField, Range(0f,1f)] private float hunger = 1f;
    [SerializeField, Range(0f,1f)] private float fatigue = 0f;

    // Suspicion level (0 = not suspicious, 1 = maximum suspicion). If this reaches
    // 1 the player is exposed and the game ends. Persisted via PlayerPrefs.
    [SerializeField, Range(0f,1f)] private float suspicion = 0f;
    const string SuspicionKey = "PlayerSuspicion";

    public int Money => money;
    public float Hunger => hunger;
    public float Fatigue => fatigue;

    public float Suspicion => suspicion;

    public delegate void StatsChanged();
    public event StatsChanged OnStatsChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        // load saved money amount if available
        money = PlayerPrefs.GetInt(MoneyKey, money);

        // load saved suspicion level if available
        suspicion = PlayerPrefs.GetFloat(SuspicionKey, suspicion);
    }
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static void CreateIfNeeded()
    {
        if (Instance == null)
        {
            var go = new GameObject("PlayerStats");
            go.AddComponent<PlayerStats>();
        }
    }

    public void SetMoney(int value)
    {
        money = value;
        PlayerPrefs.SetInt(MoneyKey, money);
        PlayerPrefs.Save();
        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// Sets the suspicion level directly (0–1) and persists it. Invokes
    /// OnStatsChanged so the HUD can update.
    /// </summary>
    /// <param name="value">New suspicion value (clamped to 0–1)</param>
    public void SetSuspicion(float value)
    {
        float previous = suspicion;
        suspicion = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SuspicionKey, suspicion);
        PlayerPrefs.Save();
        if (previous != suspicion)
            OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// Changes the suspicion level by a delta (positive or negative) and persists
    /// the result. Invokes OnStatsChanged when the value actually changes.
    /// </summary>
    /// <param name="delta">Amount to add to suspicion (can be negative)</param>
    public void ChangeSuspicion(float delta)
    {
        if (Mathf.Approximately(delta, 0f)) return;
        SetSuspicion(suspicion + delta);
    }

    public void ChangeMoney(int delta)
    {
        money += delta;
        PlayerPrefs.SetInt(MoneyKey, money);
        PlayerPrefs.Save();
        OnStatsChanged?.Invoke();
    }

    public void SetHunger(float value)
    {
        var previous = hunger;
        hunger = Mathf.Clamp01(value);
        Debug.Log($"Hunger changed from {previous} to {hunger}");
        OnStatsChanged?.Invoke();
    }

    public void SetFatigue(float value)
    {
        var previous = fatigue;
        fatigue = Mathf.Clamp01(value);
        Debug.Log($"Fatigue changed from {previous} to {fatigue}");
        OnStatsChanged?.Invoke();
    }
}