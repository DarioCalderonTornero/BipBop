using UnityEngine;
using Unity.Services.LevelPlay;
using System;
using System.Collections;

public class MediationAds : MonoBehaviour
{
    public static MediationAds Instance { get; private set; }

    [SerializeField] private string adUnitIdAndroid = "Rewarded_Android";
    [SerializeField] private string adUnitIdIOS = "Rewarded_iOS";

    private LevelPlayRewardedAd rewardedAd;
    private string adUnitId;

    private Action<bool> onAdFinishedCallback;
    private bool rewardEarned = false;
    private bool adsInitialized = false;
    private bool eventsHooked = false;

    public event Action<bool> OnAdAvailabilityChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        AdsInicializer.OnLevelPlayInitialized += InitializeAds;

        // Muy importante: si ya se inicializó antes, inicializamos aquí también
        if (AdsInicializer.IsInitialized)
            InitializeAds();
    }

    private void OnDisable()
    {
        AdsInicializer.OnLevelPlayInitialized -= InitializeAds;
    }

    private void OnDestroy()
    {
        if (rewardedAd != null && eventsHooked)
        {
            rewardedAd.OnAdLoaded -= OnAdLoaded;
            rewardedAd.OnAdLoadFailed -= OnAdLoadFailed;
            rewardedAd.OnAdRewarded -= OnAdRewarded;
            rewardedAd.OnAdClosed -= OnAdClosed;
            eventsHooked = false;
        }

        if (Instance == this)
            Instance = null;
    }

    private void InitializeAds()
    {
        if (adsInitialized)
            return;

        adsInitialized = true;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        LevelPlay.LaunchTestSuite();
#endif

#if UNITY_ANDROID
        adUnitId = adUnitIdAndroid;
#elif UNITY_IOS
        adUnitId = adUnitIdIOS;
#else
        adUnitId = adUnitIdAndroid;
#endif

        if (string.IsNullOrEmpty(adUnitId))
        {
            Debug.LogError("MediationAds: adUnitId está vacío.");
            OnAdAvailabilityChanged?.Invoke(false);
            return;
        }

        rewardedAd = new LevelPlayRewardedAd(adUnitId);

        rewardedAd.OnAdLoaded += OnAdLoaded;
        rewardedAd.OnAdLoadFailed += OnAdLoadFailed;
        rewardedAd.OnAdRewarded += OnAdRewarded;
        rewardedAd.OnAdClosed += OnAdClosed;
        eventsHooked = true;

        rewardedAd.LoadAd();

        Debug.Log($"MediationAds: Rewarded inicializado con adUnitId = {adUnitId}");
    }

    private void OnAdLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("MediationAds: Anuncio recompensado cargado.");
        OnAdAvailabilityChanged?.Invoke(true);
    }

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"MediationAds: Error cargando rewarded ad: {error}");
        OnAdAvailabilityChanged?.Invoke(false);

        CancelInvoke(nameof(RetryLoadAd));
        Invoke(nameof(RetryLoadAd), 5f);
    }

    private void RetryLoadAd()
    {
        if (rewardedAd == null)
        {
            Debug.LogWarning("MediationAds: RetryLoadAd llamado pero rewardedAd es null.");
            return;
        }

        Debug.Log("MediationAds: Reintentando cargar anuncio...");
        rewardedAd.LoadAd();
    }

    private void OnAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        rewardEarned = true;
        Debug.Log("MediationAds: Usuario recompensado por la red de anuncios.");
    }

    private void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"MediationAds: Anuncio cerrado. rewardEarned = {rewardEarned}");

        rewardedAd?.LoadAd();
        OnAdAvailabilityChanged?.Invoke(IsAdReady());

        StartCoroutine(WaitAndNotifyReward());
    }

    private IEnumerator WaitAndNotifyReward()
    {
        yield return new WaitForSecondsRealtime(0.5f);

        if (rewardEarned)
        {
            AdsProgressManager.RegisterAdViewed();
        }

        Action<bool> callback = onAdFinishedCallback;
        onAdFinishedCallback = null;

        callback?.Invoke(rewardEarned);

        rewardEarned = false;
    }

    public void ShowRewardedAd(Action<bool> onAdFinished)
    {
        if (rewardedAd != null && rewardedAd.IsAdReady())
        {
            onAdFinishedCallback = onAdFinished;
            rewardEarned = false;

            Debug.Log("MediationAds: Mostrando anuncio recompensado.");
            rewardedAd.ShowAd();

            OnAdAvailabilityChanged?.Invoke(false);
        }
        else
        {
            Debug.LogWarning("MediationAds: Se intentó mostrar un anuncio pero no estaba listo.");
            OnAdAvailabilityChanged?.Invoke(false);
            onAdFinished?.Invoke(false);
        }
    }

    public bool IsAdReady()
    {
        return rewardedAd != null && rewardedAd.IsAdReady();
    }
}