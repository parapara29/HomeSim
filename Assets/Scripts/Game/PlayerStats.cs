using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }
    const string MoneyKey = "PlayerMoney";
    [SerializeField] private int money = 1000;
    [SerializeField, Range(0f,1f)] private float hunger = 1f;
    [SerializeField, Range(0f,1f)] private float fatigue = 0f;

    public int Money => money;
    public float Hunger => hunger;
    public float Fatigue => fatigue;

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
        StatsHUD.CreateIfNeeded();
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

    public void ChangeMoney(int delta)
    {
        money += delta;
        PlayerPrefs.SetInt(MoneyKey, money);
        PlayerPrefs.Save();
        OnStatsChanged?.Invoke();
    }

    public void SetHunger(float value)
    {
        hunger = Mathf.Clamp01(value);
        OnStatsChanged?.Invoke();
    }

    public void SetFatigue(float value)
    {
        fatigue = Mathf.Clamp01(value);
        OnStatsChanged?.Invoke();
    }
}