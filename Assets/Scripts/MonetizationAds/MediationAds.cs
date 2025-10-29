using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.LevelPlay; // Necesario para la nueva Mediation
using System;

public class MediationAds : MonoBehaviour
{
    [SerializeField] private Button showAdButton;
    [SerializeField] private string adUnitId = "Rewarded_Android"; // Pon aquí el Ad Unit ID real

    private LevelPlayRewardedAd rewardedAd;

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
        rewardedAd = new LevelPlayRewardedAd(adUnitId);

        rewardedAd.OnAdLoaded += OnAdLoaded;
        rewardedAd.OnAdLoadFailed += OnAdLoadFailed;
        rewardedAd.OnAdRewarded += OnAdRewarded;
        rewardedAd.OnAdClosed += OnAdClosed;

        showAdButton.interactable = false;
        showAdButton.onClick.RemoveAllListeners();
        showAdButton.onClick.AddListener(TryShowAd);

        rewardedAd.LoadAd();
    }

    private void OnAdLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Ad loaded and ready.");
        showAdButton.interactable = true; // Activa botón solo cuando el anuncio está listo
    }

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"Failed to load ad: {error}");
        showAdButton.interactable = false;
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
        // Aquí añade la lógica para otorgar la recompensa in-game.
    }

    private void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Ad closed, loading next ad.");
        rewardedAd.LoadAd();
    }
}
