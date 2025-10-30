using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.LevelPlay; // Necesario para la nueva Mediation
using System;

public class MediationAds : MonoBehaviour
{
    [SerializeField] private Button showAdButton;
    [SerializeField] private string adUnitIdAndroid = "Rewarded_Androidd"; // Ad Unit ID Android
    [SerializeField] private string adUnitIdIOS = "Rewarded_iOS"; // Ad Unit ID iOS

    private LevelPlayRewardedAd rewardedAd;
    private string adUnitId;

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
        showAdButton.interactable = true; // Solo habilita botón cuando el anuncio está listo
    }

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"Failed to load ad: {error}");
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
        if (rewardedAd.IsAdReady())
        {
            rewardedAd.ShowAd();
            showAdButton.interactable = false;
        }
        else
        {
            Debug.Log("Ad not ready yet.");
        }
    }

    private void OnAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        Debug.Log($"User rewarded: {reward.Name} x {reward.Amount}");
        // Aquí pones tu lógica para dar recompensa in-game
    }

    private void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Ad closed, loading next ad.");
        rewardedAd.LoadAd();
    }
}
