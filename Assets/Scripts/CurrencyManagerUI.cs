using TMPro;
using UnityEngine;

public class CurrencyManagerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinText;

    private void Start()
    {
        UpdateCoinText();
    }

    private void UpdateCoinText()
    {
        if (CurrencyManager.Instance != null && coinText != null)
        {
            coinText.text = CurrencyManager.Instance.GetCoins().ToString();
        }
        else
        {
            Debug.LogWarning("CurrencyManager or coinText is null!");
        }
    }
}
