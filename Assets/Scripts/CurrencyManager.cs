using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }


    private int coins;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            coins = PlayerPrefs.GetInt("CoinCount", 0);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        Debug.Log(coins);
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        SaveCoins();
    }

    public void SpendCoins(int amount)
    {
        coins = Mathf.Max(0, coins - amount);
        SaveCoins();
    }

    public int GetCoins() => coins;

    private void SaveCoins()
    {
        PlayerPrefs.SetInt("CoinCount", coins);
        PlayerPrefs.Save();
    }

    // Cuando entres a otra escena, reubica el TMP por nombre si quieres
    /*
    public void AssignUIByName(string coinTextObjectName)
    {
        GameObject obj = GameObject.Find(coinTextObjectName);
        if (obj != null)
        {
            coinText = obj.GetComponent<TextMeshProUGUI>();
        }
    }
    */
}
