using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReviveOfferUIController : MonoBehaviour
{
    [Header("Offer UI")]
    [SerializeField] private GameObject offerRoot;
    [SerializeField] private CanvasGroup offerCanvasGroup; // NUEVO (recomendado)
    [SerializeField] private Button watchAdButton;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI watchAdText;
    [SerializeField] private Animator offerAnimator;
    [SerializeField] private string offerAnimatorBool = "PlayVideoGameOver";

    [Header("Timer")]
    [SerializeField] private AdOfferTimer offerTimer;

    [Header("Countdown UI")]
    [SerializeField] private ReviveCountdownUI reviveCountdownUI;

    private Action onTimeoutOrDecline;
    private Action onRewardedCompleted;
    private bool isOpen;

    private void Awake()
    {
        // Importante: si el juego baja timeScale, que el animator no se quede congelado
        if (offerAnimator != null)
            offerAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;

        CloseImmediate();

        if (watchAdButton != null)
        {
            watchAdButton.onClick.RemoveAllListeners();
            watchAdButton.onClick.AddListener(OnClickWatchAd);
        }

        if (offerTimer != null)
        {
            offerTimer.OnExpired -= HandleOfferExpired;
            offerTimer.OnExpired += HandleOfferExpired;
        }
    }

    public void Open(Action onTimeoutOrDecline, Action onRewardedCompleted)
    {
        this.onTimeoutOrDecline = onTimeoutOrDecline;
        this.onRewardedCompleted = onRewardedCompleted;

        isOpen = true;

        // Activamos el GO por si estaba apagado
        gameObject.SetActive(true);
        if (offerRoot != null) offerRoot.SetActive(true);

        // Mostrar INSTANT (da igual timeScale)
        SetOfferVisible(true);

        if (offerAnimator != null && !string.IsNullOrEmpty(offerAnimatorBool))
            offerAnimator.SetBool(offerAnimatorBool, true);

        if (offerTimer != null)
            offerTimer.Begin();
    }

    public void CloseImmediate()
    {
        isOpen = false;

        if (offerTimer != null)
            offerTimer.Stop();

        if (offerAnimator != null && !string.IsNullOrEmpty(offerAnimatorBool))
            offerAnimator.SetBool(offerAnimatorBool, false);

        // Oculta instantáneo y corta interacción
        SetOfferVisible(false);

        if (offerRoot != null)
            offerRoot.SetActive(false);

        gameObject.SetActive(false);

        onTimeoutOrDecline = null;
        onRewardedCompleted = null;
    }

    private void HandleOfferExpired()
    {
        if (!isOpen) return;

        // Oculta ya
        HideOfferVisuals();

        onTimeoutOrDecline?.Invoke();
    }

    private void OnClickWatchAd()
    {
        if (!isOpen) return;

        // Oculta YA al click (antes de cualquier cosa)
        HideOfferVisuals();

        // Si no hay ads manager, caemos a final
        if (MediationAds.Instance == null)
        {
            Debug.LogWarning("[ReviveOfferUIController] MediationAds.Instance is null.");
            onTimeoutOrDecline?.Invoke();
            return;
        }

        if (!MediationAds.Instance.IsAdReady())
        {
            Debug.Log("[ReviveOfferUIController] Rewarded not ready.");
            onTimeoutOrDecline?.Invoke();
            return;
        }

        MediationAds.Instance.ShowRewardedAd(OnRewardedSuccess);
    }

    private void OnRewardedSuccess()
    {
        // Seguridad extra: oculta otra vez por si algo lo reactivó
        HideOfferVisuals();

        if (reviveCountdownUI == null)
        {
            onRewardedCompleted?.Invoke();
            return;
        }

        reviveCountdownUI.Play(() =>
        {
            onRewardedCompleted?.Invoke();
        });
    }

    private void HideOfferVisuals()
    {
        if (offerTimer != null)
            offerTimer.Stop();

        if (offerAnimator != null && !string.IsNullOrEmpty(offerAnimatorBool))
            offerAnimator.SetBool(offerAnimatorBool, false);

        SetOfferVisible(false);
    }

    /// <summary>
    /// Visible/invisible instantáneo (no depende de timeScale),
    /// y además corta raycasts/interacción.
    /// </summary>
    private void SetOfferVisible(bool visible)
    {
        // 1) CanvasGroup manda (recomendado)
        if (offerCanvasGroup != null)
        {
            offerCanvasGroup.alpha = visible ? 1f : 0f;
            offerCanvasGroup.interactable = visible;
            offerCanvasGroup.blocksRaycasts = visible;
        }

        // 2) Por si no usas CanvasGroup, apagamos objetos explícitos también
        if (watchAdButton != null)
        {
            watchAdButton.interactable = visible;
            watchAdButton.gameObject.SetActive(visible);
        }

        if (backgroundImage != null)
        {
            backgroundImage.gameObject.SetActive(visible);
        }

        if (watchAdText != null)
            watchAdText.gameObject.SetActive(visible);

        // Si tienes root y quieres que desaparezca sí o sí:
        if (offerRoot != null)
            offerRoot.SetActive(visible);
    }
}
