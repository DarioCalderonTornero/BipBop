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

    [Header("UI")]
    [SerializeField] private Button rollBackgroundButton;
    [SerializeField] private Button rollAvatarButton;

    [SerializeField] private TextMeshProUGUI feedbackText; // Solo para errores (sin monedas, sin pool, etc.)
    [SerializeField] private DailyLuckSpinnerUI spinnerUI;

    [Header("Spin visuals")]
    [SerializeField] private int spinListSize = 18;

    private const string DEFAULT_BG_ID = "DefaultBackground";
    private const string DEFAULT_AVATAR_ID = "NormalAvatar";

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

    private void RollBackground()
    {
        if (spinnerUI != null && spinnerUI.IsSpinning) return;

        if (backgroundPool == null || backgroundPool.possibleBackgrounds == null || backgroundPool.possibleBackgrounds.Count == 0)
        {
            SetFeedback("No hay fondos configurados en la suerte diaria.");
            return;
        }

        if (!CurrencyManager.Instance.TrySpendCoins(rollCost))
        {
            SetFeedback("No tienes suficientes monedas.");
            return;
        }

        BackgroundDataSO reward = PickBackground(backgroundPool.possibleBackgrounds);
        if (reward == null || reward.sprite == null)
        {
            SetFeedback("Error: premio inválido.");
            return;
        }

        bool alreadyOwned = IsBackgroundOwned(reward);
        string resultMsg = alreadyOwned
            ? $"+{duplicateRefund} monedas (ya lo tenías)"
            : "¡Nuevo fondo conseguido!";

        List<Sprite> spinSprites = BuildSpinSpriteList(backgroundPool.possibleBackgrounds, spinListSize, backgroundPool.allowDuplicatesInSpin);

        LockButtons(true);
        SetFeedback("");

        spinnerUI.PlaySpin(
            spinSprites,
            reward.sprite,
            resultMsg,
            onFinished: () => ApplyBackgroundReward(reward, alreadyOwned),
            onClosed: () => LockButtons(false)
        );
    }

    private void RollAvatar()
    {
        if (spinnerUI != null && spinnerUI.IsSpinning) return;

        if (avatarPool == null || avatarPool.possibleAvatars == null || avatarPool.possibleAvatars.Count == 0)
        {
            SetFeedback("No hay avatares configurados en la suerte diaria.");
            return;
        }

        if (!CurrencyManager.Instance.TrySpendCoins(rollCost))
        {
            SetFeedback("No tienes suficientes monedas.");
            return;
        }

        AvatarDataSO reward = PickAvatar(avatarPool.possibleAvatars);
        if (reward == null || reward.sprite == null)
        {
            SetFeedback("Error: premio inválido.");
            return;
        }

        // Si quieres bloquear aquí avatares de score (por si cuela uno en la lista):
        if (reward.unlockByScore)
        {
            SetFeedback("Error: ese avatar no puede salir en suerte diaria (unlockByScore).");
            return;
        }

        bool alreadyOwned = IsAvatarOwned(reward);
        string resultMsg = alreadyOwned
            ? $"+{duplicateRefund} monedas (ya lo tenías)"
            : "¡Nuevo avatar conseguido!";

        List<Sprite> spinSprites = BuildSpinSpriteListAvatars(avatarPool.possibleAvatars, spinListSize, avatarPool.allowDuplicatesInSpin);

        LockButtons(true);
        SetFeedback("");

        spinnerUI.PlaySpin(
            spinSprites,
            reward.sprite,
            resultMsg,
            onFinished: () => ApplyAvatarReward(reward, alreadyOwned),
            onClosed: () => LockButtons(false)
        );
    }

    // -------------------------
    // Apply rewards
    // -------------------------
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

    // -------------------------
    // Owned checks
    // -------------------------
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

    // -------------------------
    // Picks
    // -------------------------
    private BackgroundDataSO PickBackground(List<BackgroundDataSO> list)
    {
        int tries = 0;
        while (tries < 50)
        {
            var pick = list[Random.Range(0, list.Count)];
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
            var pick = list[Random.Range(0, list.Count)];
            if (pick != null) return pick;
            tries++;
        }
        return null;
    }

    // -------------------------
    // Spin sprite lists
    // -------------------------
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
            // metemos todos (o hasta desiredCount) y luego repetimos si falta
            for (int i = 0; i < Mathf.Min(desiredCount, valid.Count); i++)
                sprites.Add(valid[i]);

            while (sprites.Count < desiredCount)
                sprites.Add(valid[Random.Range(0, valid.Count)]);
        }
        else
        {
            for (int i = 0; i < desiredCount; i++)
                sprites.Add(valid[Random.Range(0, valid.Count)]);
        }

        return sprites;
    }

    // -------------------------
    // UI helpers
    // -------------------------
    private void LockButtons(bool locked)
    {
        if (rollBackgroundButton != null) rollBackgroundButton.interactable = !locked;
        if (rollAvatarButton != null) rollAvatarButton.interactable = !locked;
    }

    private void SetFeedback(string msg)
    {
        if (feedbackText != null)
            feedbackText.text = msg;
    }
}
