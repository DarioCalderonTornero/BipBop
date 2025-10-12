using System;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    public event Action<int> OnCoinsChanged;

    private static int coins;
    private const string CoinsKey = "CoinCount"; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            coins = PlayerPrefs.GetInt(CoinsKey, 0);
            OnCoinsChanged?.Invoke(coins);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        Debug.Log(GetCoins());
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        coins += amount;
        SaveCoins();
        OnCoinsChanged?.Invoke(coins);
        Debug.Log("Coin added");
    }

    public void SpendCoins(int amount)
    {
        if (amount <= 0) return;
        coins = Mathf.Max(0, coins - amount);
        SaveCoins();
        OnCoinsChanged?.Invoke(coins);
    }

    public int GetCoins() => coins;

    private void SaveCoins()
    {
        PlayerPrefs.SetInt(CoinsKey, coins);
        PlayerPrefs.Save();
    }

    private void OnApplicationQuit()
    {
        SaveCoins();
    }
}
