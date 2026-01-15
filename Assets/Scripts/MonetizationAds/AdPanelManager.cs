using System;
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

    [Header("Progress UI (4 hitos: 30 / 50 / 80 / 20+)")]
    [SerializeField] private Image[] milestoneIcons;          // tamaño 4 (iconos circulares)
    [SerializeField] private TextMeshProUGUI[] milestoneTexts; // tamaño 4 ("30","50","80","20+")
    [SerializeField] private Color milestoneGray = new Color(0.65f, 0.65f, 0.65f, 1f);
    [SerializeField] private Color milestoneColor = Color.white; // si tus sprites ya son dorados, blanco = “a color”
    [SerializeField] private Color claimedTextColor = new Color(0.42f, 0.24f, 0.12f);

    [Header("Next reward UI")]
    [SerializeField] private TextMeshProUGUI nextRewardText;  // "Siguiente recompensa: 30"
    [SerializeField] private Image nextRewardCoinIcon;        // icono moneda a la derecha

    [Header("Pop Animation (suave)")]
    [SerializeField] private float popInDuration = 0.16f;
    [SerializeField] private float popOutDuration = 0.12f;
    [SerializeField] private float popOvershoot = 1.06f;
    [SerializeField] private float popOutScale = 0.92f;

    private MediationAds Mediation => MediationAds.Instance;

    private RectTransform panelRT;
    private Vector3 panelBaseScale = Vector3.one;
    private Coroutine animRoutine;
    private bool isOpen = false;

    // ---------- Daily Progress ----------
    private const string PREF_LAST_DAY = "ADS_LAST_DAY";
    private const string PREF_WATCHED_TODAY = "ADS_WATCHED_TODAY";

    // 1º, 2º, 3º y luego 20 siempre
    private static readonly int[] FirstRewards = { 30, 50, 80 };
    private const int RepeatReward = 20;

    private void Start()
    {
        panelRT = adPanel != null ? adPanel.GetComponent<RectTransform>() : null;
        if (panelRT != null) panelBaseScale = panelRT.localScale;

        if (adPanel != null) adPanel.SetActive(false);
        if (panelRT != null) panelRT.localScale = panelBaseScale;

        if (coinImage != null) coinImage.sprite = coinSprite;
        if (nextRewardCoinIcon != null) nextRewardCoinIcon.sprite = coinSprite;

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

        else Debug.LogWarning("AdPanelManager: MediationAds.Instance es null al iniciar el menú.");

        RefreshDailyReset();
        RefreshUI();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) return;
        RefreshDailyReset();
        RefreshUI();
    }

    public void ShowPanel()
    {
        if (adPanel == null || panelRT == null) return;
        if (isOpen) return;

        RefreshDailyReset();
        RefreshUI();

        isOpen = true;
        adPanel.SetActive(true);

        watchAdButton.interactable = (Mediation != null) && Mediation.IsAdReady();

        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(PopInRoutine());
    }

    private void ClosePanel()
    {
        if (adPanel == null || panelRT == null) return;
        if (!isOpen) return;

        isOpen = false;

        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(PopOutRoutine());
    }

    private void OnWatchAdBtnClicked()
    {
        // Opcional: si quieres NO cerrar el panel hasta que termine el anuncio,
        // mueve ClosePanel() dentro del callback de recompensa.
        ClosePanel();

        if (Mediation == null)
        {
            Debug.LogWarning("AdPanelManager: MediationAds es null, no se puede mostrar el anuncio.");
            return;
        }

        Mediation.ShowRewardedAd(() =>
        {
            RefreshDailyReset();

            int watched = PlayerPrefs.GetInt(PREF_WATCHED_TODAY, 0);
            int reward = GetRewardForWatchIndex(watched); // watched=0 => 30 (primer anuncio)
            PlayerPrefs.SetInt(PREF_WATCHED_TODAY, watched + 1);
            PlayerPrefs.Save();

            if (gameManager != null) gameManager.AddCoins(reward);

            RefreshUI();
        });
    }

    // ------------------ Rewards / UI ------------------

    private int GetRewardForWatchIndex(int watchedSoFarToday)
    {
        if (watchedSoFarToday < 0) watchedSoFarToday = 0;
        if (watchedSoFarToday < FirstRewards.Length) return FirstRewards[watchedSoFarToday];
        return RepeatReward;
    }

    private void RefreshDailyReset()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        string lastDay = PlayerPrefs.GetString(PREF_LAST_DAY, "");

        if (lastDay != today)
        {
            PlayerPrefs.SetString(PREF_LAST_DAY, today);
            PlayerPrefs.SetInt(PREF_WATCHED_TODAY, 0);
            PlayerPrefs.Save();
        }
    }

    private void RefreshUI()
    {
        int watched = PlayerPrefs.GetInt(PREF_WATCHED_TODAY, 0);

        // Milestones: 0..2 se colorean si ya los has visto, el 3 (20+) siempre gris
        for (int i = 0; i < 4; i++)
        {
            bool isInfinite = (i == 3);                 // el "20+"
            bool completed = (!isInfinite) && (watched >= (i + 1));

            // ICONO (moneda)
            Color iconColor = isInfinite ? milestoneGray : (completed ? milestoneColor : milestoneGray);

            // TEXTO (número): cuando esté completado usa claimedTextColor (distinto al icono)
            Color textColor = isInfinite ? milestoneGray : (completed ? claimedTextColor : milestoneGray);

            if (milestoneIcons != null && i < milestoneIcons.Length && milestoneIcons[i] != null)
                milestoneIcons[i].color = iconColor;

            if (milestoneTexts != null && i < milestoneTexts.Length && milestoneTexts[i] != null)
                milestoneTexts[i].color = textColor;
        }

        // Next reward text
        int next = GetRewardForWatchIndex(watched);
        if (nextRewardText != null)
            nextRewardText.text = $"Siguiente anuncio: {next}";

        if (nextRewardCoinIcon != null)
            nextRewardCoinIcon.enabled = true;
    }

    // ------------------ Animations ------------------

    private IEnumerator PopInRoutine()
    {
        panelRT.localScale = panelBaseScale * 0.90f;

        float t = 0f;

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

        adPanel.SetActive(false);
        panelRT.localScale = panelBaseScale;
        animRoutine = null;
    }

    private void OnEnable()
    {
        if (MediationAds.Instance != null)
            MediationAds.Instance.OnAdAvailabilityChanged += HandleAdReadyChanged;
    }

    private void OnDisable()
    {
        if (MediationAds.Instance != null)
            MediationAds.Instance.OnAdAvailabilityChanged -= HandleAdReadyChanged;
    }

    private void HandleAdReadyChanged(bool ready)
    {
        if (watchAdButton != null)
            watchAdButton.interactable = ready;
    }

    private float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    private float EaseInCubic(float t) => t * t * t;
}
