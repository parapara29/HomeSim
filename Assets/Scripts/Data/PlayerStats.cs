using UnityEngine;

public static class PlayerStats
{
    private const string MoneyKey = "PlayerMoney";

    public static int Money
    {
        get { return PlayerPrefs.GetInt(MoneyKey, 0); }
        set { PlayerPrefs.SetInt(MoneyKey, value); PlayerPrefs.Save(); }
    }
}
