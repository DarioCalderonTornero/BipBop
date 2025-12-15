using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DailyLuckManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private DailyLuckPoolSO backgroundPool;
    [SerializeField] private DailyLuckAvatarPoolSO avatarPool;
    [SerializeField] private int rollCost = 100;
    [SerializeField] private int duplicateRefund = 75;

    [Header("Daily Attempts")]
    [SerializeField] private int maxAttemptsPerDay = 3;

    [Header("UI")]
    [SerializeField] private Button rollBackgroundButton;
    [SerializeField] private Button rollAvatarButton;

    [SerializeField] private TextMeshProUGUI feedbackText; // Solo errores (sin monedas, sin pool, etc.)
    [SerializeField] private DailyLuckSpinnerUI spinnerUI;

    [Header("Attempts Text")]
    [SerializeField] private TextMeshProUGUI bgAttemptsText;     // "Intentos restantes: X/X"
    [SerializeField] private TextMeshProUGUI avatarAttemptsText; // "Intentos restantes: X/X"

    [Header("Spin visuals")]
    [SerializeField] private int spinListSize = 18;

    private const string DEFAULT_BG_ID = "DefaultBackground";
    private const string DEFAULT_AVATAR_ID = "NormalAvatar";

    // PlayerPrefs keys
    private const string BG_DAY_KEY = "DailyLuck_BG_DayKey";
    private const string AV_DAY_KEY = "DailyLuck_AV_DayKey";
    private const string BG_REMAINING_KEY = "DailyLuck_BG_Remaining";
    private const string AV_REMAINING_KEY = "DailyLuck_AV_Remaining";

    private bool buttonsLockedBySpin;

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
        RefreshAttemptsUI();
        RefreshButtonsInteractable();
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
            // Intentamos reactivar el objeto del spinner
            spinnerUI.gameObject.SetActive(true);
            spinnerUI.enabled = true;
        }

        EnsureDailyCounters();

        Debug.Log("Debería hacer algo");

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

        BackgroundDataSO reward = PickBackground(backgroundPool.possibleBackgrounds);
        if (reward == null || reward.sprite == null)
        {
            SetFeedback("Error: premio inválido.");
            return;
        }

        // Cobro ÚNICO (TrySpendCoins ya debe restar)
        if (!CurrencyManager.Instance.TrySpendCoins(rollCost))
        {
            SetFeedback("No tienes suficientes monedas.");
            return;
        }

        // Consumimos intento SOLO si el roll arranca de verdad
        SetRemainingBG(remaining - 1);
        RefreshAttemptsUI();

        bool alreadyOwned = IsBackgroundOwned(reward);
        string resultMsg = alreadyOwned
            ? $"+{duplicateRefund} monedas (ya lo tenías)"
            : "¡Nuevo fondo conseguido!";

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
            // Intentamos reactivar el objeto del spinner
            spinnerUI.gameObject.SetActive(true);
            spinnerUI.enabled = true;
        }

        EnsureDailyCounters();

        Debug.Log("Debería hacer algo de avatares");

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

        AvatarDataSO reward = PickAvatar(avatarPool.possibleAvatars);
        if (reward == null || reward.sprite == null)
        {
            SetFeedback("Error: premio inválido.");
            return;
        }

        if (reward.unlockByScore)
        {
            SetFeedback("Error: ese avatar no puede salir en suerte diaria (unlockByScore).");
            return;
        }

        if (!CurrencyManager.Instance.TrySpendCoins(rollCost))
        {
            SetFeedback("No tienes suficientes monedas.");
            return;
        }

        SetRemainingAV(remaining - 1);
        RefreshAttemptsUI();

        bool alreadyOwned = IsAvatarOwned(reward);
        string resultMsg = alreadyOwned
            ? $"+{duplicateRefund} monedas (ya lo tenías)"
            : "¡Nuevo avatar conseguido!";

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
            changed = true;
        }

        // AV
        if (PlayerPrefs.GetString(AV_DAY_KEY, "") != today)
        {
            PlayerPrefs.SetString(AV_DAY_KEY, today);
            PlayerPrefs.SetInt(AV_REMAINING_KEY, maxAttemptsPerDay);
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

    private void RefreshAttemptsUI()
    {
        if (bgAttemptsText != null)
            bgAttemptsText.text = $"Intentos restantes: {GetRemainingBG()}/{maxAttemptsPerDay}";

        if (avatarAttemptsText != null)
            avatarAttemptsText.text = $"Intentos restantes: {GetRemainingAV()}/{maxAttemptsPerDay}";
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
}
