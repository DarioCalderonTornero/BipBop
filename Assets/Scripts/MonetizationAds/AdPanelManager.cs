using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using UnityEngine.Localization; // ✅ Localization

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
    [SerializeField] private RectTransform bonusButtonRoot;     // contenedor (NO escalar)
    [SerializeField] private RectTransform bonusButtonVisual;   // ✅ hijo visual (SÍ escalar)
    [SerializeField] private TextMeshProUGUI bonusLeftText;     // texto debajo

    [Header("Localization (texto botón bonus)")]
    [SerializeField] private LocalizedString lsBonusRemaining;  // key: ads_bonus_remaining  => "Quedan {0} bonus hoy" (Smart)
    [SerializeField] private LocalizedString lsGet20Coins;      // key: ads_bonus_get_20     => "Conseguir 20 monedas"

    [SerializeField] private float breatheAmount = 0.05f;       // +5%
    [SerializeField] private float breathePeriod = 2.4f;        // lento
    private Coroutine breatheRoutine;
    private Vector3 visualBaseScale = Vector3.one;

    private Coroutine bonusTextRoutine;                         // ✅ para evitar solapes de async

    [Header("Otros scripts")]
    [SerializeField] private CurrencyManager gameManager;

    [Header("Progress UI (4 hitos: 30 / 50 / 80 / 20)")]
    [SerializeField] private Image[] milestoneIcons;            // tamaño 4
    [SerializeField] private TextMeshProUGUI[] milestoneTexts;  // tamaño 4
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

    // ---------- Daily Progress ----------
    private const string PREF_LAST_DAY = "ADS_LAST_DAY";
    private const string PREF_WATCHED_TODAY = "ADS_WATCHED_TODAY";

    // ✅ Los 4 “bonus” diarios (los buenos)
    private static readonly int[] DailyRewards = { 30, 50, 80, 20 };
    private int DailyBonusLimit => DailyRewards.Length; // 4

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

        // Captura escala base del "visual" cuando el layout ya está asentado
        StartCoroutine(CaptureVisualBaseScaleNextFrame());

        RefreshDailyReset();
        RefreshUI();
        RefreshBonusButtonUI();

        // La respiración SIEMPRE activa
        EnsureBreathing();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) return;

        RefreshDailyReset();
        RefreshUI();
        RefreshBonusButtonUI();

        EnsureBreathing();
    }

    public void ShowPanel()
    {
        if (adPanel == null || panelRT == null) return;
        if (isOpen) return;

        RefreshDailyReset();
        RefreshUI();
        RefreshBonusButtonUI();

        EnsureBreathing();

        isOpen = true;
        adPanel.SetActive(true);

        if (watchAdButton != null)
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
                return;
            }

            RefreshDailyReset();

            int watched = PlayerPrefs.GetInt(PREF_WATCHED_TODAY, 0);

            int reward = GetRewardForWatchIndex(watched);
            PlayerPrefs.SetInt(PREF_WATCHED_TODAY, watched + 1);
            PlayerPrefs.Save();

            if (reward > 0 && gameManager != null)
                gameManager.AddCoins(reward);

            RefreshUI();
            RefreshBonusButtonUI();
            EnsureBreathing();
        });
    }

    // ------------------ Bonus helpers ------------------

    private int GetRemainingBonusToday()
    {
        int watched = PlayerPrefs.GetInt(PREF_WATCHED_TODAY, 0);
        return Mathf.Max(0, DailyBonusLimit - watched);
    }

    private int GetRewardForWatchIndex(int watchedSoFarToday)
    {
        if (watchedSoFarToday < 0) watchedSoFarToday = 0;
        if (watchedSoFarToday >= DailyRewards.Length) return 0;
        return DailyRewards[watchedSoFarToday];
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
            // ✅ "Quedan {0} bonus hoy"
            lsBonusRemaining.Arguments = new object[] { remaining };
            var op = lsBonusRemaining.GetLocalizedStringAsync();
            yield return op;
            if (bonusLeftText != null) bonusLeftText.text = op.Result;
        }
        else
        {
            // ✅ "Conseguir 20 monedas"
            var op = lsGet20Coins.GetLocalizedStringAsync();
            yield return op;
            if (bonusLeftText != null) bonusLeftText.text = op.Result;
        }

        bonusTextRoutine = null;
    }

    // ------------------ Daily reset ------------------

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

    // ------------------ Main UI refresh ------------------

    private void RefreshUI()
    {
        int watched = PlayerPrefs.GetInt(PREF_WATCHED_TODAY, 0);

        for (int i = 0; i < 4; i++)
        {
            bool completed = watched >= (i + 1);

            Color iconColor = completed ? milestoneColor : milestoneGray;
            Color textColor = completed ? claimedTextColor : milestoneGray;

            if (milestoneIcons != null && i < milestoneIcons.Length && milestoneIcons[i] != null)
                milestoneIcons[i].color = iconColor;

            if (milestoneTexts != null && i < milestoneTexts.Length && milestoneTexts[i] != null)
                milestoneTexts[i].color = textColor;
        }

        int next = GetRewardForWatchIndex(watched);

        if (nextRewardText != null)
            nextRewardText.text = $"Siguiente anuncio: {next}";

        if (nextRewardCoinIcon != null)
            nextRewardCoinIcon.enabled = true;
    }

    // ------------------ Breathing (SIEMPRE) ------------------

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

        // ✅ Base scale fija (evita “runaway”)
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

    // ------------------ Panel animations ------------------

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

        StopBreathing();

        if (bonusTextRoutine != null)
        {
            StopCoroutine(bonusTextRoutine);
            bonusTextRoutine = null;
        }
    }

    private void HandleAdReadyChanged(bool ready)
    {
        if (watchAdButton != null)
            watchAdButton.interactable = ready;
    }

    private float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    private float EaseInCubic(float t) => t * t * t;
}