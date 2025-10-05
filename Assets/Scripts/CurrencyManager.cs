using System;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    public event Action<int> OnCoinsChanged;

    private static int coins;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            //coins = 0; 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        coins += amount;
        OnCoinsChanged?.Invoke(coins);
    }

    public void SpendCoins(int amount)
    {
        if (amount <= 0) return;
        coins = Mathf.Max(0, coins - amount);
        OnCoinsChanged?.Invoke(coins);
    }

    public int GetCoins() => coins;
}
