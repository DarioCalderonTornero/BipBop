using System.Collections;
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

    [Header("Pop Animation (suave)")]
    [SerializeField] private float popInDuration = 0.16f;
    [SerializeField] private float popOutDuration = 0.12f;
    [SerializeField] private float popOvershoot = 1.06f;  // 1.04 - 1.10 recomendado
    [SerializeField] private float popOutScale = 0.92f;   // 0.85 - 0.95 recomendado

    private MediationAds Mediation => MediationAds.Instance;

    private RectTransform panelRT;
    private Vector3 panelBaseScale = Vector3.one;
    private Coroutine animRoutine;
    private bool isOpen = false;

    void Start()
    {
        panelRT = adPanel != null ? adPanel.GetComponent<RectTransform>() : null;
        if (panelRT != null) panelBaseScale = panelRT.localScale;

        adPanel.SetActive(false);
        if (panelRT != null) panelRT.localScale = panelBaseScale;

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
        if (adPanel == null || panelRT == null) return;
        if (isOpen) return;

        isOpen = true;
        adPanel.SetActive(true);

        if (Mediation != null)
            watchAdButton.interactable = Mediation.IsAdReady();
        else
            watchAdButton.interactable = false;

        // Pop in suave
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(PopInRoutine());
    }

    private void ClosePanel()
    {
        if (adPanel == null || panelRT == null) return;
        if (!isOpen) return;

        isOpen = false;

        // Pop out suave (y al final desactiva)
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(PopOutRoutine());
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

    // ------------------ Animations ------------------

    private IEnumerator PopInRoutine()
    {
        // start: un poco pequeño y transparente (opcional)
        panelRT.localScale = panelBaseScale * 0.90f;

        float t = 0f;

        // 1) scale up to overshoot
        Vector3 a = panelBaseScale * 0.90f;
        Vector3 b = panelBaseScale * popOvershoot;

        while (t < popInDuration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / popInDuration);
            float eased = EaseOutCubic(u);
            panelRT.localScale = Vector3.LerpUnclamped(a, b, eased);
            yield return null;
        }

        // 2) settle back to 1
        float settleDur = popInDuration * 0.55f;
        t = 0f;
        a = panelRT.localScale;
        b = panelBaseScale;

        while (t < settleDur)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / settleDur);
            float eased = EaseOutCubic(u);
            panelRT.localScale = Vector3.LerpUnclamped(a, b, eased);
            yield return null;
        }

        panelRT.localScale = panelBaseScale;
        animRoutine = null;
    }

    private IEnumerator PopOutRoutine()
    {
        // 1) pequeño “shrink”
        float t = 0f;
        Vector3 a = panelRT.localScale;
        Vector3 b = panelBaseScale * popOutScale;

        while (t < popOutDuration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / popOutDuration);
            float eased = EaseInCubic(u);
            panelRT.localScale = Vector3.LerpUnclamped(a, b, eased);
            yield return null;
        }

        // 2) apagar
        adPanel.SetActive(false);
        panelRT.localScale = panelBaseScale;
        animRoutine = null;
    }

    // ------------------ Easing ------------------
    private float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    private float EaseInCubic(float t) => t * t * t;
}
