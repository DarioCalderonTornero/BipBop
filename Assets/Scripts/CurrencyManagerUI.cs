using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CurrencyManagerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinText;

    private void Start()
    {
        UpdateCoinText();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == "Menu")
        {
            UpdateCoinText();
        }
    }

    private void UpdateCoinText()
    {
        if (CurrencyManager.Instance != null && coinText != null)
        {
            coinText.text = CurrencyManager.Instance.GetCoins().ToString();
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Menu")
            UpdateCoinText();
    }


}
