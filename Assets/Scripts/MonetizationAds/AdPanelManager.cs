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
    [SerializeField] private Button openAdPanelButton;

    [Header("Otros scripts")]
    [SerializeField] private CurrencyManager gameManager;

    private MediationAds Mediation => MediationAds.Instance;

    void Start()
    {
        adPanel.SetActive(false);
        coinImage.sprite = coinSprite;

        watchAdButton.onClick.RemoveAllListeners();
        watchAdButton.onClick.AddListener(OnWatchAdBtnClicked);
        watchAdButton.interactable = false;

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(ClosePanel);

        if (openAdPanelButton != null)
        {
            openAdPanelButton.onClick.RemoveAllListeners();
            openAdPanelButton.onClick.AddListener(ShowPanel);
        }

        // Asigna SIEMPRE el botón al singleton cuando se entra al menú
        if (Mediation != null)
        {
            Mediation.SetShowAdButton(watchAdButton);
        }
        else
        {
            Debug.LogWarning("AdPanelManager: MediationAds.Instance es null al iniciar el menú.");
        }
    }

    public void ShowPanel()
    {
        adPanel.SetActive(true);

        if (Mediation != null)
            watchAdButton.interactable = Mediation.IsAdReady();
        else
            watchAdButton.interactable = false;
    }

    private void ClosePanel()
    {
        adPanel.SetActive(false);
    }

    private void OnWatchAdBtnClicked()
    {
        ClosePanel();

        if (Mediation == null)
        {
            Debug.LogWarning("AdPanelManager: MediationAds es null, no se puede mostrar el anuncio.");
            return;
        }

        Mediation.ShowRewardedAd(() =>
        {
            gameManager.AddCoins(5);
        });
    }
}
