using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReviveOfferUIController : MonoBehaviour
{
    [Header("Offer UI")]
    [SerializeField] private GameObject offerRoot; // Panel completo de la oferta
    [SerializeField] private Button watchAdButton;
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

    /// <summary>
    /// Abre la oferta. Si expira o se rechaza => onTimeoutOrDecline.
    /// Si rewarded completado => countdown => onRewardedCompleted.
    /// </summary>
    public void Open(Action onTimeoutOrDecline, Action onRewardedCompleted)
    {
        this.onTimeoutOrDecline = onTimeoutOrDecline;
        this.onRewardedCompleted = onRewardedCompleted;

        isOpen = true;
        gameObject.SetActive(true);

        if (offerRoot != null) offerRoot.SetActive(true);

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

        if (offerRoot != null)
            offerRoot.SetActive(false);

        gameObject.SetActive(false);

        onTimeoutOrDecline = null;
        onRewardedCompleted = null;
    }

    private void HandleOfferExpired()
    {
        if (!isOpen) return;
        // Oferta expira => Final
        onTimeoutOrDecline?.Invoke();
    }

    private void OnClickWatchAd()
    {
        if (!isOpen) return;

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

        // Acepta ver anuncio: paramos el timer y ocultamos offer
        if (offerTimer != null) offerTimer.Stop();
        if (offerRoot != null) offerRoot.SetActive(false);

        // Mostramos rewarded
        MediationAds.Instance.ShowRewardedAd(OnRewardedSuccess);
    }

    private void OnRewardedSuccess()
    {
        // Countdown 3-2-1-GO (unscaled), luego revive
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
}
