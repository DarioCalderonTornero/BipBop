using System;
using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public class PlayFabScoreManager : MonoBehaviour
{
    public static PlayFabScoreManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    #region Enviar Scores Client-side
    /// <summary>
    /// Envia un score al leaderboard de PlayFab si supera el mejor local.
    /// </summary>
    public void SubmitScore(string statisticName, int score)
    {
        if (!PlayFabLoginManager.Instance.IsLoggedIn)
        {
            Debug.LogWarning("Intentando enviar score antes de loguear.");
            return;
        }

        int bestLocal = PlayerPrefs.GetInt("best_" + statisticName, 0);
        if (score <= bestLocal)
        {
            Debug.Log("Score menor o igual al mejor local. Ignorado.");
            return;
        }

        PlayerPrefs.SetInt("best_" + statisticName, score);
        PlayerPrefs.Save();

        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate
                {
                    StatisticName = statisticName,
                    Value = score
                }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request, result =>
        {
            Debug.Log($"Score enviado: {score} ({statisticName})");
        }, error =>
        {
            Debug.LogWarning("Error al enviar score: " + error.GenerateErrorReport());
            // opcional: encolar para reintento
        });
    }
    #endregion

    #region Obtener Leaderboard
    /// <summary>
    /// Obtiene el top N de un leaderboard
    /// </summary>
    public void GetLeaderboard(string statisticName, int top, Action<List<PlayerLeaderboardEntry>> onSuccess)
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = statisticName,
            StartPosition = 0,
            MaxResultsCount = top
        };

        PlayFabClientAPI.GetLeaderboard(request, result =>
        {
            onSuccess?.Invoke(result.Leaderboard);
        }, error =>
        {
            Debug.LogWarning("Error obteniendo leaderboard: " + error.GenerateErrorReport());
        });
    }
    #endregion
}
