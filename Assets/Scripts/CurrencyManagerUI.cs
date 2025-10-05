using TMPro;
using UnityEngine;

public class CurrencyManagerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinText;

    private void OnEnable()
    {
        if (coinText == null)
        {
            return;
        }

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinsChanged += HandleCoinsChanged;
            // valor inicial
            HandleCoinsChanged(CurrencyManager.Instance.GetCoins());
        }
        else
        {
            // Si no hay Instance aún, el texto puede quedar en 0 o vacío
            if (coinText != null) coinText.text = "0";
        }
    }

    private void OnDisable()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCoinsChanged -= HandleCoinsChanged;
    }

    private void HandleCoinsChanged(int newAmount)
    {
        if (coinText != null)
            coinText.text = newAmount.ToString();
    }
}
