using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReviveOfferUIController : MonoBehaviour
{
    [Header("Offer UI")]
    [SerializeField] private GameObject offerRoot;
    [SerializeField] private CanvasGroup offerCanvasGroup;
    [SerializeField] private Button watchAdButton;
    [SerializeField] private Button declineButton; // NUEVO
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI watchAdText;
    [SerializeField] private Animator offerAnimator;
    [SerializeField] private string offerAnimatorBool = "PlayVideoGameOver";

    [Header("Timer")]
    [SerializeField] private AdOfferTimer offerTimer;

    [Header("Countdown UI (después del rewarded)")]
    [SerializeField] private ReviveCountdownUI reviveCountdownUI;

    private Action onTimeoutOrDecline;
    private Action onRewardedCompleted;
    private bool isOpen;

    private void Awake()
    {
        if (offerAnimator != null)
            offerAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;

        CloseImmediate();

        if (watchAdButton != null)
        {
            watchAdButton.onClick.RemoveAllListeners();
            watchAdButton.onClick.AddListener(OnClickWatchAd);
        }

        if (declineButton != null)
        {
            declineButton.onClick.RemoveAllListeners();
            declineButton.onClick.AddListener(OnClickDecline);
        }

        if (offerTimer != null)
        {
            offerTimer.OnExpired -= HandleOfferExpired;
            offerTimer.OnExpired += HandleOfferExpired;
        }
    }

    private void OnDestroy()
    {
        if (offerTimer != null)
            offerTimer.OnExpired -= HandleOfferExpired;
    }

    public void Open(Action onTimeoutOrDecline, Action onRewardedCompleted)
    {
        this.onTimeoutOrDecline = onTimeoutOrDecline;
        this.onRewardedCompleted = onRewardedCompleted;

        isOpen = true;

        gameObject.SetActive(true);
        if (offerRoot != null) offerRoot.SetActive(true);

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

        HideOfferVisuals();
        onTimeoutOrDecline?.Invoke();
    }

    private void OnClickDecline()
    {
        if (!isOpen) return;

        HideOfferVisuals();
        onTimeoutOrDecline?.Invoke();
    }

    private void OnClickWatchAd()
    {
        if (!isOpen) return;

        HideOfferVisuals();

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

    private void SetOfferVisible(bool visible)
    {
        if (offerCanvasGroup != null)
        {
            offerCanvasGroup.alpha = visible ? 1f : 0f;
            offerCanvasGroup.interactable = visible;
            offerCanvasGroup.blocksRaycasts = visible;
        }

        if (watchAdButton != null)
        {
            watchAdButton.interactable = visible;
            watchAdButton.gameObject.SetActive(visible);
        }

        if (declineButton != null)
        {
            declineButton.interactable = visible;
            declineButton.gameObject.SetActive(visible);
        }

        if (backgroundImage != null)
            backgroundImage.gameObject.SetActive(visible);

        if (watchAdText != null)
            watchAdText.gameObject.SetActive(visible);

        if (offerRoot != null)
            offerRoot.SetActive(visible);
    }
}