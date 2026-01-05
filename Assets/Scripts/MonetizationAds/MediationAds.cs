using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.LevelPlay; // Necesario para la nueva Mediation
using System;

public class MediationAds : MonoBehaviour
{
    public static MediationAds Instance { get; private set; }

    public Button showAdButton;
    [SerializeField] private string adUnitIdAndroid = "Rewarded_Androidd"; // Ad Unit ID Android
    [SerializeField] private string adUnitIdIOS = "Rewarded_iOS"; // Ad Unit ID iOS

    private LevelPlayRewardedAd rewardedAd;
    private string adUnitId;

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

        Debug.Log("MediationAds singleton instance created.");
    }

    private void OnDestroy()
    {
        Debug.Log("MediationAds destroyed: " + gameObject.name);
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
        // Selecciona Ad Unit según la plataforma
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        // Lanza el modo de prueba oficial de LevelPlay
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

        showAdButton.interactable = false;
        showAdButton.onClick.RemoveAllListeners();
        showAdButton.onClick.AddListener(TryShowAd);

        // Cargar el anuncio
        rewardedAd.LoadAd();
    }

    private void OnAdLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Ad loaded and ready.");

        if (showAdButton != null)
            showAdButton.interactable = true; // Solo habilita botón cuando el anuncio está listo
    }

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"Failed to load ad: {error}");

        if (showAdButton != null)
            showAdButton.interactable = false;

        // Reintento automático opcional
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
        else
        {
            Debug.Log("Ad not ready yet.");
        }
    }

    private System.Action onRewardedCallback;

    private void OnAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        Debug.Log($"User rewarded: {reward.Name} x {reward.Amount}");

        // Aquí interprete el reward.Name y aplique reward.Amount según corresponda
        if (reward.Name == "Coin")
        {
            CurrencyManager.Instance.AddCoins(5);
        }
        else
        {
            // Recuerda manejar o ignorar otros tipos de recompensa
            CurrencyManager.Instance.AddCoins(5);
        }

        onRewardedCallback?.Invoke();
        onRewardedCallback = null;
    }

    private void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Ad closed, loading next ad.");
        rewardedAd.LoadAd();
    }

    public void ShowRewardedAd(System.Action onRewarded)
    {
        if (rewardedAd != null && rewardedAd.IsAdReady())
        {
            // Guarda el callback, lo llamas al recibir OnAdRewarded
            this.onRewardedCallback = onRewarded;
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
