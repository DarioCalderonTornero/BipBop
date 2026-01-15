using UnityEngine;
using Unity.Services.LevelPlay;
using System;

public class MediationAds : MonoBehaviour
{
    public static MediationAds Instance { get; private set; }

    [SerializeField] private string adUnitIdAndroid = "Rewarded_Androidd";
    [SerializeField] private string adUnitIdIOS = "Rewarded_iOS";

    private LevelPlayRewardedAd rewardedAd;
    private string adUnitId;
    private Action onRewardedCallback;

    public event Action<bool> OnAdAvailabilityChanged; // opcional para UI

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
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

        rewardedAd.LoadAd();
    }

    private void OnAdLoaded(LevelPlayAdInfo adInfo)
    {
        OnAdAvailabilityChanged?.Invoke(true);
    }

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
        OnAdAvailabilityChanged?.Invoke(false);
        Invoke(nameof(RetryLoadAd), 5f);
    }

    private void RetryLoadAd()
    {
        rewardedAd?.LoadAd();
    }

    private void OnAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        // IMPORTANT: aquí NO damos monedas.
        onRewardedCallback?.Invoke();
        onRewardedCallback = null;
    }

    private void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        rewardedAd?.LoadAd();
        OnAdAvailabilityChanged?.Invoke(IsAdReady());
    }

    public void ShowRewardedAd(Action onRewarded)
    {
        if (rewardedAd != null && rewardedAd.IsAdReady())
        {
            onRewardedCallback = onRewarded;
            rewardedAd.ShowAd();
            OnAdAvailabilityChanged?.Invoke(false);
        }
        else
        {
            // Si quieres feedback, aquí podrías loguear
            OnAdAvailabilityChanged?.Invoke(false);
        }
    }

    public bool IsAdReady()
    {
        return rewardedAd != null && rewardedAd.IsAdReady();
    }
}
