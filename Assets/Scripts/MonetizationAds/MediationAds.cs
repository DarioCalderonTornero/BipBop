using UnityEngine;
using Unity.Services.LevelPlay;
using System;
using System.Collections;

public class MediationAds : MonoBehaviour
{
    public static MediationAds Instance { get; private set; }

    [SerializeField] private string adUnitIdAndroid = "Rewarded_Androidd";
    [SerializeField] private string adUnitIdIOS = "Rewarded_iOS";

    private LevelPlayRewardedAd rewardedAd;
    private string adUnitId;

    private Action<bool> onAdFinishedCallback;
    private bool rewardEarned = false;

    public event Action<bool> OnAdAvailabilityChanged;

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
        rewardEarned = true; // Anotamos que ha ganado el premio
    }

    private void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        // Pedimos otro anuncio para tenerlo listo
        rewardedAd?.LoadAd();
        OnAdAvailabilityChanged?.Invoke(IsAdReady());

        // ¡EL ARREGLO! En lugar de ejecutarlo ya, iniciamos una corrutina de seguridad
        StartCoroutine(WaitAndNotifyReward());
    }

    private IEnumerator WaitAndNotifyReward()
    {
        yield return new WaitForSecondsRealtime(0.5f);

        if (rewardEarned)
        {
            AdsProgressManager.RegisterAdViewed();
        }

        onAdFinishedCallback?.Invoke(rewardEarned);
        onAdFinishedCallback = null;
    }

    public void ShowRewardedAd(Action<bool> onAdFinished)
    {
        if (rewardedAd != null && rewardedAd.IsAdReady())
        {
            onAdFinishedCallback = onAdFinished;
            rewardEarned = false; // Reseteamos siempre antes de mostrar
            rewardedAd.ShowAd();
            OnAdAvailabilityChanged?.Invoke(false);
        }
        else
        {
            OnAdAvailabilityChanged?.Invoke(false);
            onAdFinished?.Invoke(false);
        }
    }

    public bool IsAdReady()
    {
        return rewardedAd != null && rewardedAd.IsAdReady();
    }
}