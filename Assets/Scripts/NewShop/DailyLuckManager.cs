using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class DailyLuckManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private DailyLuckPoolSO backgroundPool;
    [SerializeField] private DailyLuckAvatarPoolSO avatarPool;
    [SerializeField] private int rollCost = 100;
    [SerializeField] private int duplicateRefund = 75;

    [Header("Daily Attempts")]
    [SerializeField] private int maxAttemptsPerDay = 10; // ahora 10

    [Header("UI")]
    [SerializeField] private Button rollBackgroundButton;
    [SerializeField] private Button rollAvatarButton;

    [SerializeField] private TextMeshProUGUI feedbackText; // Solo errores
    [SerializeField] private DailyLuckSpinnerUI spinnerUI;

    [Header("Attempts Text")]
    [SerializeField] private TextMeshProUGUI bgAttemptsText;     // "Intentos restantes: X/X"
    [SerializeField] private TextMeshProUGUI avatarAttemptsText; // "Intentos restantes: X/X"

    [Header("Localization")]
    [Tooltip("Smart String con {0} y {1}. Ej: 'Intentos restantes: {0}/{1}'")]
    [SerializeField] private LocalizedString attemptsTextTemplate;      // UI/daily_luck_attempts_template

    [Tooltip("Smart String con {0}. Ej: '+{0} monedas (ya lo tenías)'")]
    [SerializeField] private LocalizedString bgDuplicateRefundMsg;      // UI/daily_luck_bg_duplicate_refund

    [SerializeField] private LocalizedString bgNewMsg;                  // UI/daily_luck_bg_new

    [Tooltip("Smart String con {0}. Ej: '+{0} monedas (ya lo tenías)'")]
    [SerializeField] private LocalizedString avDuplicateRefundMsg;      // UI/daily_luck_av_duplicate_refund

    [SerializeField] private LocalizedString avNewMsg;                  // UI/daily_luck_av_new

    [Header("Spin visuals")]
    [SerializeField] private int spinListSize = 18;

    [Header("Ads")]
    [SerializeField] private MediationAds mediationAds; // referencia a tu script de anuncios

    private const string DEFAULT_BG_ID = "DefaultBackground";
    private const string DEFAULT_AVATAR_ID = "NormalAvatar";

    // PlayerPrefs keys
    private const string BG_DAY_KEY = "DailyLuck_BG_DayKey";
    private const string AV_DAY_KEY = "DailyLuck_AV_DayKey";
    private const string BG_REMAINING_KEY = "DailyLuck_BG_Remaining";
    private const string AV_REMAINING_KEY = "DailyLuck_AV_Remaining";

    // Contadores de tiradas de HOY (para saber cuál es la 5 y la 10)
    private const string BG_TODAY_ROLLS_KEY = "DailyLuck_BG_TodayRolls";
    private const string AV_TODAY_ROLLS_KEY = "DailyLuck_AV_TodayRolls";

    private bool buttonsLockedBySpin;

    [Header("Ad roll UI")]
    [SerializeField] private GameObject bgAdInfoRoot;     // Imagen + texto para tirada BG con anuncio
    [SerializeField] private GameObject avatarAdInfoRoot; // Imagen + texto para tirada Avatar con anuncio

    private void Awake()
    {
        if (rollBackgroundButton != null)
        {
            rollBackgroundButton.onClick.RemoveAllListeners();
            rollBackgroundButton.onClick.AddListener(RollBackground);
        }

        if (rollAvatarButton != null)
        {
            rollAvatarButton.onClick.RemoveAllListeners();
            rollAvatarButton.onClick.AddListener(RollAvatar);
        }
    }

    private void OnEnable()
    {
        buttonsLockedBySpin = false;
        EnsureDailyCounters();
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        RefreshAttemptsUI();
        RefreshButtonsInteractable();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale _)
    {
        RefreshAttemptsUI();
    }

    private void Update()
    {
        // Si cambia de día a las 00:00
        if (EnsureDailyCounters())
        {
            RefreshAttemptsUI();
            RefreshButtonsInteractable();
        }
    }

    // =========================================================
    // Rolls
    // =========================================================
    private void RollBackground()
    {
        if (spinnerUI != null && spinnerUI.IsSpinning) return;

        if (spinnerUI == null)
        {
            SetFeedback("spinnerUI no asignado.");
            return;
        }

        if (!spinnerUI.isActiveAndEnabled)
        {
            spinnerUI.gameObject.SetActive(true);
            spinnerUI.enabled = true;
        }

        EnsureDailyCounters();

        int remaining = GetRemainingBG();
        if (remaining <= 0)
        {
            SetFeedback("No te quedan intentos hoy.");
            RefreshAttemptsUI();
            RefreshButtonsInteractable();
            return;
        }

        if (backgroundPool == null || backgroundPool.possibleBackgrounds == null || backgroundPool.possibleBackgrounds.Count == 0)
        {
            SetFeedback("No hay fondos configurados en la suerte diaria.");
            return;
        }

        // Índice de tirada de hoy: 1..maxAttemptsPerDay
        int todayRollIndex = maxAttemptsPerDay - remaining + 1;
        bool isAdRoll = (todayRollIndex == 5 || todayRollIndex == 10);

        if (isAdRoll)
        {
            // Tirada que requiere anuncio (no gasta monedas)
            if (mediationAds != null && mediationAds.IsAdReady())
            {
                LockButtons(true);
                SetFeedback("");

                mediationAds.ShowRewardedAd(() =>
                {
                    // Callback cuando se ha visto el anuncio -> tirada gratuita
                    _ = DoBackgroundRollInternal(remaining, isFreeRoll: true);
                });
            }
            else
            {
                SetFeedback("Anuncio no disponible ahora mismo.");
            }
        }
        else
        {
            // Tirada normal (pagando monedas)
            _ = DoBackgroundRollInternal(remaining, isFreeRoll: false);
        }
    }

    private void RollAvatar()
    {
        if (spinnerUI != null && spinnerUI.IsSpinning) return;

        if (spinnerUI == null)
        {
            SetFeedback("spinnerUI no asignado.");
            return;
        }

        if (!spinnerUI.isActiveAndEnabled)
        {
            spinnerUI.gameObject.SetActive(true);
            spinnerUI.enabled = true;
        }

        EnsureDailyCounters();

        int remaining = GetRemainingAV();
        if (remaining <= 0)
        {
            SetFeedback("No te quedan intentos hoy.");
            RefreshAttemptsUI();
            RefreshButtonsInteractable();
            return;
        }

        if (avatarPool == null || avatarPool.possibleAvatars == null || avatarPool.possibleAvatars.Count == 0)
        {
            SetFeedback("No hay avatares configurados en la suerte diaria.");
            return;
        }

        AvatarDataSO rewardCheck = PickAvatar(avatarPool.possibleAvatars);
        if (rewardCheck == null || rewardCheck.sprite == null)
        {
            SetFeedback("Error: premio inválido.");
            return;
        }

        if (rewardCheck.unlockByScore)
        {
            SetFeedback("Error: ese avatar no puede salir en suerte diaria (unlockByScore).");
            return;
        }

        // Índice de tirada de hoy: 1..maxAttemptsPerDay
        int todayRollIndex = maxAttemptsPerDay - remaining + 1;
        bool isAdRoll = (todayRollIndex == 5 || todayRollIndex == 10);

        if (isAdRoll)
        {
            // Tirada que requiere anuncio (no gasta monedas)
            if (mediationAds != null && mediationAds.IsAdReady())
            {
                LockButtons(true);
                SetFeedback("");

                mediationAds.ShowRewardedAd(() =>
                {
                    _ = DoAvatarRollInternal(remaining, isFreeRoll: true);
                });
            }
            else
            {
                SetFeedback("Anuncio no disponible ahora mismo.");
            }
        }
        else
        {
            // Tirada normal (pagando monedas)
            _ = DoAvatarRollInternal(remaining, isFreeRoll: false);
        }
    }

    // =========================================================
    // Lógica interna de tirada BG
    // =========================================================
    private async Task DoBackgroundRollInternal(int remaining, bool isFreeRoll)
    {
        BackgroundDataSO reward = PickBackground(backgroundPool.possibleBackgrounds);
        if (reward == null || reward.sprite == null)
        {
            SetFeedback("Error: premio inválido.");
            LockButtons(false);
            return;
        }

        // Si no es tirada gratuita (5 o 10), cobramos monedas
        if (!isFreeRoll)
        {
            if (!CurrencyManager.Instance.TrySpendCoins(rollCost))
            {
                SetFeedback("No tienes suficientes monedas.");
                LockButtons(false);
                return;
            }
        }

        // Consumimos intento SOLO si la tirada arranca
        SetRemainingBG(remaining - 1);

        // Incrementamos contador de tiradas de hoy (por si lo quisieras usar en otro sitio)
        SetTodayBgRolls(GetTodayBgRolls() + 1);

        RefreshAttemptsUI();

        bool alreadyOwned = IsBackgroundOwned(reward);

        string resultMsg = alreadyOwned
            ? await GetLocalized(bgDuplicateRefundMsg, duplicateRefund)
            : await GetLocalized(bgNewMsg);

        List<Sprite> spinSprites = BuildSpinSpriteList(
            backgroundPool.possibleBackgrounds, spinListSize, backgroundPool.allowDuplicatesInSpin
        );

        LockButtons(true);
        SetFeedback("");

        spinnerUI.PlaySpin(
            spinSprites,
            reward.sprite,
            resultMsg,
            onFinished: () => ApplyBackgroundReward(reward, alreadyOwned),
            onClosed: () =>
            {
                LockButtons(false);
                RefreshButtonsInteractable();
            }
        );
    }

    // =========================================================
    // Lógica interna de tirada Avatar
    // =========================================================
    private async Task DoAvatarRollInternal(int remaining, bool isFreeRoll)
    {
        AvatarDataSO reward = PickAvatar(avatarPool.possibleAvatars);
        if (reward == null || reward.sprite == null)
        {
            SetFeedback("Error: premio inválido.");
            LockButtons(false);
            return;
        }

        if (reward.unlockByScore)
        {
            SetFeedback("Error: ese avatar no puede salir en suerte diaria (unlockByScore).");
            LockButtons(false);
            return;
        }

        if (!isFreeRoll)
        {
            if (!CurrencyManager.Instance.TrySpendCoins(rollCost))
            {
                SetFeedback("No tienes suficientes monedas.");
                LockButtons(false);
                return;
            }
        }

        SetRemainingAV(remaining - 1);
        SetTodayAvRolls(GetTodayAvRolls() + 1);

        RefreshAttemptsUI();

        bool alreadyOwned = IsAvatarOwned(reward);

        string resultMsg = alreadyOwned
            ? await GetLocalized(avDuplicateRefundMsg, duplicateRefund)
            : await GetLocalized(avNewMsg);

        List<Sprite> spinSprites = BuildSpinSpriteListAvatars(
            avatarPool.possibleAvatars, spinListSize, avatarPool.allowDuplicatesInSpin
        );

        LockButtons(true);
        SetFeedback("");

        spinnerUI.PlaySpin(
            spinSprites,
            reward.sprite,
            resultMsg,
            onFinished: () => ApplyAvatarReward(reward, alreadyOwned),
            onClosed: () =>
            {
                LockButtons(false);
                RefreshButtonsInteractable();
            }
        );
    }

    // =========================================================
    // Daily counters (reset 00:00)
    // =========================================================
    private bool EnsureDailyCounters()
    {
        string today = DateTime.Now.ToString("yyyyMMdd");
        bool changed = false;

        // BG
        if (PlayerPrefs.GetString(BG_DAY_KEY, "") != today)
        {
            PlayerPrefs.SetString(BG_DAY_KEY, today);
            PlayerPrefs.SetInt(BG_REMAINING_KEY, maxAttemptsPerDay);
            PlayerPrefs.SetInt(BG_TODAY_ROLLS_KEY, 0);
            changed = true;
        }

        // AV
        if (PlayerPrefs.GetString(AV_DAY_KEY, "") != today)
        {
            PlayerPrefs.SetString(AV_DAY_KEY, today);
            PlayerPrefs.SetInt(AV_REMAINING_KEY, maxAttemptsPerDay);
            PlayerPrefs.SetInt(AV_TODAY_ROLLS_KEY, 0);
            changed = true;
        }

        if (changed) PlayerPrefs.Save();
        return changed;
    }

    private int GetRemainingBG() => PlayerPrefs.GetInt(BG_REMAINING_KEY, maxAttemptsPerDay);
    private int GetRemainingAV() => PlayerPrefs.GetInt(AV_REMAINING_KEY, maxAttemptsPerDay);

    private void SetRemainingBG(int value)
    {
        PlayerPrefs.SetInt(BG_REMAINING_KEY, Mathf.Clamp(value, 0, maxAttemptsPerDay));
        PlayerPrefs.Save();
    }

    private void SetRemainingAV(int value)
    {
        PlayerPrefs.SetInt(AV_REMAINING_KEY, Mathf.Clamp(value, 0, maxAttemptsPerDay));
        PlayerPrefs.Save();
    }

    private int GetTodayBgRolls() => PlayerPrefs.GetInt(BG_TODAY_ROLLS_KEY, 0);
    private int GetTodayAvRolls() => PlayerPrefs.GetInt(AV_TODAY_ROLLS_KEY, 0);

    private void SetTodayBgRolls(int value)
    {
        PlayerPrefs.SetInt(BG_TODAY_ROLLS_KEY, Mathf.Clamp(value, 0, maxAttemptsPerDay));
        PlayerPrefs.Save();
    }

    private void SetTodayAvRolls(int value)
    {
        PlayerPrefs.SetInt(AV_TODAY_ROLLS_KEY, Mathf.Clamp(value, 0, maxAttemptsPerDay));
        PlayerPrefs.Save();
    }

    private async void RefreshAttemptsUI()
    {
        int remainingBG = GetRemainingBG();
        int remainingAV = GetRemainingAV();

        // Índice de la SIGUIENTE tirada de hoy (1..maxAttemptsPerDay)
        int nextBgRollIndex = maxAttemptsPerDay - remainingBG + 1;
        int nextAvRollIndex = maxAttemptsPerDay - remainingAV + 1;

        bool nextBgIsAdRoll = (nextBgRollIndex == 5 || nextBgRollIndex == 10);
        bool nextAvIsAdRoll = (nextAvRollIndex == 5 || nextAvRollIndex == 10);

        // -------- Fondo --------
        if (nextBgIsAdRoll)
        {
            // Ocultamos texto de intentos y mostramos la imagen+texto especial
            if (bgAttemptsText != null)
                bgAttemptsText.gameObject.SetActive(false);

            if (bgAdInfoRoot != null)
                bgAdInfoRoot.SetActive(true);
        }
        else
        {
            if (bgAdInfoRoot != null)
                bgAdInfoRoot.SetActive(false);

            if (bgAttemptsText != null)
                bgAttemptsText.gameObject.SetActive(true);
        }

        // -------- Avatar --------
        if (nextAvIsAdRoll)
        {
            if (avatarAttemptsText != null)
                avatarAttemptsText.gameObject.SetActive(false);

            if (avatarAdInfoRoot != null)
                avatarAdInfoRoot.SetActive(true);
        }
        else
        {
            if (avatarAdInfoRoot != null)
                avatarAdInfoRoot.SetActive(false);

            if (avatarAttemptsText != null)
                avatarAttemptsText.gameObject.SetActive(true);
        }

        // Si no asignas LocalizedString, fallback al texto hardcodeado
        if (attemptsTextTemplate.IsEmpty)
        {
            if (bgAttemptsText != null && bgAttemptsText.gameObject.activeSelf)
                bgAttemptsText.text = $"Intentos restantes: {remainingBG}/{maxAttemptsPerDay}";

            if (avatarAttemptsText != null && avatarAttemptsText.gameObject.activeSelf)
                avatarAttemptsText.text = $"Intentos restantes: {remainingAV}/{maxAttemptsPerDay}";

            return;
        }

        string bgLine = await GetLocalized(attemptsTextTemplate, remainingBG, maxAttemptsPerDay);
        string avLine = await GetLocalized(attemptsTextTemplate, remainingAV, maxAttemptsPerDay);

        if (bgAttemptsText != null && bgAttemptsText.gameObject.activeSelf)
            bgAttemptsText.text = bgLine;

        if (avatarAttemptsText != null && avatarAttemptsText.gameObject.activeSelf)
            avatarAttemptsText.text = avLine;
    }

    private void RefreshButtonsInteractable()
    {
        if (buttonsLockedBySpin) return;

        if (rollBackgroundButton != null)
            rollBackgroundButton.interactable = GetRemainingBG() > 0;

        if (rollAvatarButton != null)
            rollAvatarButton.interactable = GetRemainingAV() > 0;
    }

    private void LockButtons(bool locked)
    {
        buttonsLockedBySpin = locked;

        if (rollBackgroundButton != null) rollBackgroundButton.interactable = !locked;
        if (rollAvatarButton != null) rollAvatarButton.interactable = !locked;

        if (!locked)
            RefreshButtonsInteractable();
    }

    // =========================================================
    // Rewards
    // =========================================================
    private void ApplyBackgroundReward(BackgroundDataSO reward, bool alreadyOwned)
    {
        if (alreadyOwned)
        {
            CurrencyManager.Instance.AddCoins(duplicateRefund);
            return;
        }

        PlayerPrefs.SetInt("Purchased_" + reward.id, 1);
        PlayerPrefs.Save();
    }

    private void ApplyAvatarReward(AvatarDataSO reward, bool alreadyOwned)
    {
        if (alreadyOwned)
        {
            CurrencyManager.Instance.AddCoins(duplicateRefund);
            return;
        }

        PlayerPrefs.SetInt("AvatarPurchased_" + reward.id, 1);
        PlayerPrefs.Save();
    }

    private bool IsBackgroundOwned(BackgroundDataSO reward)
    {
        if (reward == null) return false;
        if (reward.id == DEFAULT_BG_ID) return true;
        return PlayerPrefs.GetInt("Purchased_" + reward.id, 0) == 1;
    }

    private bool IsAvatarOwned(AvatarDataSO reward)
    {
        if (reward == null) return false;
        if (reward.id == DEFAULT_AVATAR_ID) return true;
        return PlayerPrefs.GetInt("AvatarPurchased_" + reward.id, 0) == 1;
    }

    // =========================================================
    // Picks
    // =========================================================
    private BackgroundDataSO PickBackground(List<BackgroundDataSO> list)
    {
        int tries = 0;
        while (tries < 50)
        {
            var pick = list[UnityEngine.Random.Range(0, list.Count)];
            if (pick != null) return pick;
            tries++;
        }
        return null;
    }

    private AvatarDataSO PickAvatar(List<AvatarDataSO> list)
    {
        int tries = 0;
        while (tries < 50)
        {
            var pick = list[UnityEngine.Random.Range(0, list.Count)];
            if (pick != null) return pick;
            tries++;
        }
        return null;
    }

    // =========================================================
    // Spin sprite lists
    // =========================================================
    private List<Sprite> BuildSpinSpriteList(List<BackgroundDataSO> source, int desiredCount, bool allowDuplicates)
    {
        List<Sprite> valid = new List<Sprite>();
        for (int i = 0; i < source.Count; i++)
            if (source[i] != null && source[i].sprite != null)
                valid.Add(source[i].sprite);

        return BuildSpinFromValid(valid, desiredCount, allowDuplicates);
    }

    private List<Sprite> BuildSpinSpriteListAvatars(List<AvatarDataSO> source, int desiredCount, bool allowDuplicates)
    {
        List<Sprite> valid = new List<Sprite>();
        for (int i = 0; i < source.Count; i++)
            if (source[i] != null && source[i].sprite != null)
                valid.Add(source[i].sprite);

        return BuildSpinFromValid(valid, desiredCount, allowDuplicates);
    }

    private List<Sprite> BuildSpinFromValid(List<Sprite> valid, int desiredCount, bool allowDuplicates)
    {
        List<Sprite> sprites = new List<Sprite>(desiredCount);
        if (valid.Count == 0) return sprites;

        if (!allowDuplicates)
        {
            for (int i = 0; i < Mathf.Min(desiredCount, valid.Count); i++)
                sprites.Add(valid[i]);

            while (sprites.Count < desiredCount)
                sprites.Add(valid[UnityEngine.Random.Range(0, valid.Count)]);
        }
        else
        {
            for (int i = 0; i < desiredCount; i++)
                sprites.Add(valid[UnityEngine.Random.Range(0, valid.Count)]);
        }

        return sprites;
    }

    private void SetFeedback(string msg)
    {
        if (feedbackText != null)
            feedbackText.text = msg;
    }

    // =========================================================
    // Localization helpers
    // =========================================================
    private async Task<string> GetLocalized(LocalizedString ls, params object[] args)
    {
        if (ls.IsEmpty) return "";

        ls.Arguments = args;

        AsyncOperationHandle<string> handle = ls.GetLocalizedStringAsync();
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
            return handle.Result ?? "";

        return "";
    }
}
