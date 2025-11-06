using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdPanelManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject adPanel;
    [SerializeField] private Button watchAdButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Image coinImage;
    [SerializeField] private Sprite coinSprite;
    // [SerializeField] private TextMeshProUGUI adPromptText;

    [SerializeField] private Button openAdPanelButton;

    [Header("Otros scripts")]
    [SerializeField] private MediationAds mediationAds;

    // Para el ejemplo, el GameManager responsable de sumar monedas
    [SerializeField] private CurrencyManager gameManager;

    void Start()
    {
        adPanel.SetActive(false);
        // adPromptText.text = "¿Quieres ver un anuncio por 5 ";
        coinImage.sprite = coinSprite;

        watchAdButton.onClick.RemoveAllListeners();
        watchAdButton.onClick.AddListener(OnWatchAdBtnClicked);

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(ClosePanel);

        // Deshabilita el botón hasta que el anuncio esté listo
        watchAdButton.interactable = false;

        // Evento desde MediationAds cuando el anuncio está listo
        mediationAds.showAdButton = watchAdButton;

        if (openAdPanelButton != null)
        {
            openAdPanelButton.onClick.RemoveAllListeners();
            openAdPanelButton.onClick.AddListener(ShowPanel);
        }
    }

    public void ShowPanel()
    {
        adPanel.SetActive(true);

        // El botón sólo está habilitado si el anuncio está listo
        watchAdButton.interactable = mediationAds.IsAdReady();
    }

    private void ClosePanel()
    {
        adPanel.SetActive(false);
    }

    private void OnWatchAdBtnClicked()
    {
        ClosePanel();
        mediationAds.ShowRewardedAd(() => {
            // Recompensa al jugador con 5 monedas tras ver el anuncio  
            gameManager.AddCoins(5);
        });
    }
}
