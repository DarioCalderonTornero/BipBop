using UnityEngine;
using UnityEngine.UI;
using Unity.Services.LevelPlay;
using System;

public class MediationAds : MonoBehaviour
{
    public static MediationAds Instance { get; private set; }

    [Header("UI")]
    public Button showAdButton;

    [SerializeField] private string adUnitIdAndroid = "Rewarded_Androidd";
    [SerializeField] private string adUnitIdIOS = "Rewarded_iOS";

    private LevelPlayRewardedAd rewardedAd;
    private string adUnitId;
    private Action onRewardedCallback;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("MediationAds duplicado, destruyendo este GameObject.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        AdsInicializer.OnLevelPlayInitialized += InitializeAds;
    }

    void OnDisable()
    {
        AdsInicializer.OnLevelPlayInitialized -= InitializeAds;
    }

    private void InitializeAds()
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        LevelPlay.LaunchTestSuite();
#endif

#if UNITY_ANDROID
        adUnitId = adUnitIdAndroid;
#elif UNITY_IOS
        adUnitId = adUnitIdIOS;
#endif

        rewardedAd = new LevelPlayRewardedAd(adUnitId);

        rewardedAd.OnAdLoaded += OnAdLoaded;
        rewardedAd.OnAdLoadFailed += OnAdLoadFailed;
        rewardedAd.OnAdRewarded += OnAdRewarded;
        rewardedAd.OnAdClosed += OnAdClosed;

        // El botón puede no existir aún, se configurará desde fuera
        if (showAdButton != null)
            SetupButton(showAdButton);

        rewardedAd.LoadAd();
    }

    // Lo llamará AdPanelManager siempre que vuelva a la escena de menú
    public void SetShowAdButton(Button button)
    {
        showAdButton = button;

        if (showAdButton == null)
            return;

        SetupButton(showAdButton);

        // Estado inicial en función de si el anuncio está listo
        showAdButton.interactable = IsAdReady();
    }

    private void SetupButton(Button button)
    {
        button.interactable = false;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(TryShowAd);
    }

    private void OnAdLoaded(LevelPlayAdInfo adInfo)
    {
        if (showAdButton != null)
            showAdButton.interactable = true;
    }

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
        if (showAdButton != null)
            showAdButton.interactable = false;

        Invoke(nameof(RetryLoadAd), 5f);
    }

    private void RetryLoadAd()
    {
        if (rewardedAd != null)
            rewardedAd.LoadAd();
    }

    private void TryShowAd()
    {
        if (rewardedAd != null && rewardedAd.IsAdReady())
        {
            rewardedAd.ShowAd();

            if (showAdButton != null)
                showAdButton.interactable = false;
        }
    }

    private void OnAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        CurrencyManager.Instance.AddCoins(5);
        onRewardedCallback?.Invoke();
        onRewardedCallback = null;
    }

    private void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        rewardedAd.LoadAd();
    }

    public void ShowRewardedAd(Action onRewarded)
    {
        if (rewardedAd != null && rewardedAd.IsAdReady())
        {
            onRewardedCallback = onRewarded;
            rewardedAd.ShowAd();

            if (showAdButton != null)
                showAdButton.interactable = false;
        }
    }

    public bool IsAdReady()
    {
        return rewardedAd != null && rewardedAd.IsAdReady();
    }
}
