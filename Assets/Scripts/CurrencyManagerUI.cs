using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CurrencyManagerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinText;

    private void Update()
    {
        coinText.text = CurrencyManager.Instance.GetCoins().ToString();
        Debug.Log(coinText);
    }
}
