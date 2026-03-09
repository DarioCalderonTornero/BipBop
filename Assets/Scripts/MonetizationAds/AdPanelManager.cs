using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

public class AdPanelManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject adPanel;
    [SerializeField] private Button watchAdButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Image coinImage;
    [SerializeField] private Sprite coinSprite;
    [SerializeField] private Button openAdPanelButton;

    [Header("Botón bonus (texto + respiración)")]
    [SerializeField] private RectTransform bonusButtonRoot;
    [SerializeField] private RectTransform bonusButtonVisual;
    [SerializeField] private TextMeshProUGUI bonusLeftText;

    [Header("Localization (texto botón bonus)")]
    [SerializeField] private LocalizedString lsBonusRemaining;
    [SerializeField] private LocalizedString lsGet20Coins;

    [SerializeField] private float breatheAmount = 0.05f;
    [SerializeField] private float breathePeriod = 2.4f;
    private Coroutine breatheRoutine;
    private Vector3 visualBaseScale = Vector3.one;

    private Coroutine bonusTextRoutine;

    [Header("Otros scripts")]
    [SerializeField] private CurrencyManager gameManager;

    [Header("Progress UI (3 bonus: 30 / 50 / 80)")]
    [SerializeField] private Image[] milestoneIcons;
    [SerializeField] private TextMeshProUGUI[] milestoneTexts;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private Color milestoneGray = new Color(0.65f, 0.65f, 0.65f, 1f);
    [SerializeField] private Color milestoneColor = Color.white;
    [SerializeField] private Color claimedTextColor = new Color(0.42f, 0.24f, 0.12f);

    [Header("Next reward UI")]
    [SerializeField] private TextMeshProUGUI nextRewardText;
    [SerializeField] private Image nextRewardCoinIcon;

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

    private const string PREF_LAST_DAY = "ADS_LAST_DAY";
    private const string PREF_WATCHED_TODAY = "ADS_WATCHED_TODAY";

    private static readonly int[] DailyBonusRewards = { 30, 50, 80 };
    private const int PostBonusReward = 20;
    private int DailyBonusLimit => DailyBonusRewards.Length;

    [ContextMenu("Reset Ads Today")]
    public void ResetAdsToday()
    {
        PlayerPrefs.DeleteKey(PREF_LAST_DAY);
        PlayerPrefs.DeleteKey(PREF_WATCHED_TODAY);
        PlayerPrefs.Save();

        Debug.Log("Ads diarios reseteados.");

        RefreshDailyReset();
        RefreshUI();
        RefreshBonusButtonUI();
    }

    private void Start()
    {
        panelRT = adPanel != null ? adPanel.GetComponent<RectTransform>() : null;
        if (panelRT != null) panelBaseScale = panelRT.localScale;

        if (adPanel != null) adPanel.SetActive(false);
        if (panelRT != null) panelRT.localScale = panelBaseScale;

        if (coinImage != null) coinImage.sprite = coinSprite;
        if (nextRewardCoinIcon != null) nextRewardCoinIcon.sprite = coinSprite;

        if (watchAdButton != null)
        {
            watchAdButton.onClick.RemoveAllListeners();
            watchAdButton.onClick.AddListener(OnWatchAdBtnClicked);
            watchAdButton.interactable = false;
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePanel);
        }

        if (openAdPanelButton != null)
        {
            openAdPanelButton.onClick.RemoveAllListeners();
            openAdPanelButton.onClick.AddListener(ShowPanel);
        }

        StartCoroutine(CaptureVisualBaseScaleNextFrame());

        RefreshDailyReset();
        RefreshUI();
        RefreshBonusButtonUI();
        EnsureBreathing();

        // Intenta encontrar CurrencyManager si no está asignado
        CacheCurrencyManager();

        // Sincroniza el estado inicial del botón por si el anuncio ya estaba cargado
        SyncAdButtonState();
    }

    private void OnEnable()
    {
        if (MediationAds.Instance != null)
        {
            MediationAds.Instance.OnAdAvailabilityChanged -= HandleAdReadyChanged;
            MediationAds.Instance.OnAdAvailabilityChanged += HandleAdReadyChanged;
        }

        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;

        SyncAdButtonState();
    }

    private void OnDisable()
    {
        if (MediationAds.Instance != null)
            MediationAds.Instance.OnAdAvailabilityChanged -= HandleAdReadyChanged;

        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;

        StopBreathing();

        if (bonusTextRoutine != null)
        {
            StopCoroutine(bonusTextRoutine);
            bonusTextRoutine = null;
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) return;

        RefreshDailyReset();
        RefreshUI();
        RefreshBonusButtonUI();
        EnsureBreathing();
        CacheCurrencyManager();
        SyncAdButtonState();
    }

    public void ShowPanel()
    {
        if (adPanel == null || panelRT == null) return;
        if (isOpen) return;

        RefreshDailyReset();
        RefreshUI();
        RefreshBonusButtonUI();
        EnsureBreathing();
        CacheCurrencyManager();
        SyncAdButtonState();

        isOpen = true;
        adPanel.SetActive(true);

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
        ClosePanel();

        if (Mediation == null)
        {
            Debug.LogWarning("AdPanelManager: MediationAds es null, no se puede mostrar el anuncio.");
            return;
        }

        Mediation.ShowRewardedAd((bool rewardEarned) =>
        {
            if (!rewardEarned)
            {
                Debug.Log("AdPanelManager: Anuncio cancelado o fallido. No hay recompensa.");
                RefreshUI();
                RefreshBonusButtonUI();
                SyncAdButtonState();
                return;
            }

            RefreshDailyReset();

            int watched = PlayerPrefs.GetInt(PREF_WATCHED_TODAY, 0);
            int reward = GetRewardForWatchIndex(watched);

            PlayerPrefs.SetInt(PREF_WATCHED_TODAY, watched + 1);
            PlayerPrefs.Save();

            CurrencyManager currency = GetCurrencyManager();

            if (reward > 0 && currency != null)
            {
                currency.AddCoins(reward);
                Debug.Log($"AdPanelManager: Recompensa otorgada correctamente: {reward} monedas.");
            }
            else
            {
                Debug.LogWarning($"AdPanelManager: No se pudo otorgar la recompensa. reward={reward}, CurrencyManager={(currency == null ? "NULL" : "OK")}");
            }

            RefreshUI();
            RefreshBonusButtonUI();
            EnsureBreathing();
            SyncAdButtonState();
        });
    }

    private CurrencyManager GetCurrencyManager()
    {
        if (gameManager == null)
            CacheCurrencyManager();

        return gameManager;
    }

    private void CacheCurrencyManager()
    {
#if UNITY_2023_1_OR_NEWER
        if (gameManager == null)
            gameManager = FindFirstObjectByType<CurrencyManager>(FindObjectsInactive.Exclude);
#else
        if (gameManager == null)
            gameManager = FindObjectOfType<CurrencyManager>();
#endif
    }

    private void SyncAdButtonState()
    {
        if (watchAdButton == null) return;

        bool ready = Mediation != null && Mediation.IsAdReady();
        watchAdButton.interactable = ready;
    }

    private int GetRemainingBonusToday()
    {
        int watched = PlayerPrefs.GetInt(PREF_WATCHED_TODAY, 0);
        return Mathf.Max(0, DailyBonusLimit - watched);
    }

    private int GetRewardForWatchIndex(int watchedSoFarToday)
    {
        if (watchedSoFarToday < 0)
            watchedSoFarToday = 0;

        if (watchedSoFarToday < DailyBonusRewards.Length)
            return DailyBonusRewards[watchedSoFarToday];

        return PostBonusReward;
    }

    private void RefreshBonusButtonUI()
    {
        if (bonusLeftText == null) return;

        int remaining = GetRemainingBonusToday();

        if (bonusTextRoutine != null) StopCoroutine(bonusTextRoutine);
        bonusTextRoutine = StartCoroutine(UpdateBonusLabelRoutine(remaining));
    }

    private IEnumerator UpdateBonusLabelRoutine(int remaining)
    {
        if (remaining > 0)
        {
            lsBonusRemaining.Arguments = new object[] { remaining };
            var op = lsBonusRemaining.GetLocalizedStringAsync();
            yield return op;

            if (bonusLeftText != null)
                bonusLeftText.text = op.Result;
        }
        else
        {
            var op = lsGet20Coins.GetLocalizedStringAsync();
            yield return op;

            if (bonusLeftText != null)
                bonusLeftText.text = op.Result;
        }

        bonusTextRoutine = null;
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

        for (int i = 0; i < DailyBonusLimit; i++)
        {
            bool completed = watched >= (i + 1);

            Color iconColor = completed ? milestoneColor : milestoneGray;
            Color textColor = completed ? claimedTextColor : milestoneGray;

            if (milestoneIcons != null && i < milestoneIcons.Length && milestoneIcons[i] != null)
                milestoneIcons[i].color = iconColor;

            if (milestoneTexts != null && i < milestoneTexts.Length && milestoneTexts[i] != null)
                milestoneTexts[i].color = textColor;
        }

        int postBonusIndex = DailyBonusLimit;

        if (milestoneIcons != null && postBonusIndex < milestoneIcons.Length && milestoneIcons[postBonusIndex] != null)
            milestoneIcons[postBonusIndex].color = milestoneGray;

        if (milestoneTexts != null && postBonusIndex < milestoneTexts.Length && milestoneTexts[postBonusIndex] != null)
            milestoneTexts[postBonusIndex].color = milestoneGray;

        if (milestoneIcons != null)
        {
            for (int i = postBonusIndex + 1; i < milestoneIcons.Length; i++)
            {
                if (milestoneIcons[i] != null)
                    milestoneIcons[i].color = milestoneGray;
            }
        }

        if (milestoneTexts != null)
        {
            for (int i = postBonusIndex + 1; i < milestoneTexts.Length; i++)
            {
                if (milestoneTexts[i] != null)
                    milestoneTexts[i].color = milestoneGray;
            }
        }

        if (progressFillImage != null)
            progressFillImage.fillAmount = GetProgressFillAmount(watched);

        int next = GetRewardForWatchIndex(watched);

        if (nextRewardText != null)
            nextRewardText.text = $"Siguiente anuncio: {next}";

        if (nextRewardCoinIcon != null)
            nextRewardCoinIcon.enabled = true;
    }

    private float GetProgressFillAmount(int watched)
    {
        if (watched <= 0) return 0f;
        if (watched == 1) return 0.16f;
        if (watched == 2) return 0.35f;
        if (watched == 3) return 0.66f;

        return 1f;
    }

    private IEnumerator CaptureVisualBaseScaleNextFrame()
    {
        yield return null;

        if (bonusButtonVisual != null)
            visualBaseScale = bonusButtonVisual.localScale;
        else if (bonusButtonRoot != null)
            visualBaseScale = bonusButtonRoot.localScale;
    }

    private void EnsureBreathing()
    {
        if (bonusButtonVisual == null && bonusButtonRoot == null) return;

        if (breatheRoutine == null)
            breatheRoutine = StartCoroutine(BreatheRoutine());
    }

    private IEnumerator BreatheRoutine()
    {
        RectTransform target = bonusButtonVisual != null ? bonusButtonVisual : bonusButtonRoot;
        Vector3 baseScale = target.localScale;

        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime;

            float sin01 = 0.5f + 0.5f * Mathf.Sin(t * (2f * Mathf.PI / breathePeriod));
            float s = 1f + breatheAmount * sin01;

            target.localScale = baseScale * s;
            yield return null;
        }
    }

    private void StopBreathing()
    {
        if (breatheRoutine != null)
        {
            StopCoroutine(breatheRoutine);
            breatheRoutine = null;
        }

        if (bonusButtonVisual != null)
            bonusButtonVisual.localScale = visualBaseScale;
        else if (bonusButtonRoot != null)
            bonusButtonRoot.localScale = visualBaseScale;
    }

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

    private void HandleLocaleChanged(UnityEngine.Localization.Locale _)
    {
        RefreshBonusButtonUI();
        RefreshUI();
    }

    private void HandleAdReadyChanged(bool ready)
    {
        if (watchAdButton != null)
            watchAdButton.interactable = ready;
    }

    private float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    private float EaseInCubic(float t) => t * t * t;
}