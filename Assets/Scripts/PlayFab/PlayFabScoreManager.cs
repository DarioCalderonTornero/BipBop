using System;
using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using PlayFab.Json;

[Serializable]
public class ScoreQueueItem { public string statName; public int score; public int sessionLength; public long time; }

public class PlayFabScoreManager : MonoBehaviour
{
    public static PlayFabScoreManager Instance { get; private set; }
    const string QUEUE_KEY = "pf_score_queue_v1";

    private List<ScoreQueueItem> queue = new List<ScoreQueueItem>();
    private DateTime sessionStart;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadQueue();

            sessionStart = DateTime.UtcNow;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void LoadQueue()
    {
        string json = PlayerPrefs.GetString(QUEUE_KEY, "");
        if (!string.IsNullOrEmpty(json))
            queue = JsonUtility.FromJson<Wrapper>(json).items;
    }

    void SaveQueue()
    {
        PlayerPrefs.SetString(QUEUE_KEY, JsonUtility.ToJson(new Wrapper { items = queue }));
        PlayerPrefs.Save();
    }

    [Serializable]
    class Wrapper { public List<ScoreQueueItem> items = new List<ScoreQueueItem>(); }

    public void EnqueueScore(string statName, int score, int sessionLength = 0)
    {
        queue.Add(new ScoreQueueItem
        {
            statName = statName,
            score = score,
            sessionLength = sessionLength,
            time = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });
        SaveQueue();
    }

    // Enviar score directo sin CloudScript
    public void SubmitScore(string statisticName, int score, int sessionLength = 0, bool forceSend = false)
    {
        if (!PlayFabLoginManager.Instance.IsLoggedIn)
        {
            Debug.LogWarning("Intentando enviar score antes de loguear.");
            EnqueueScore(statisticName, score, sessionLength);
            return;
        }

        if (sessionLength <= 0)
            sessionLength = (int)(DateTime.UtcNow - sessionStart).TotalSeconds;

        string key = "best_" + statisticName;
        int bestLocal = PlayerPrefs.GetInt(key, 0);
        if (!forceSend && score <= bestLocal)
        {
            Debug.Log($"Score menor o igual al mejor local ({bestLocal}). Ignorado.");
            return;
        }

        PlayerPrefs.SetInt(key, score);
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

        Debug.Log($"[PlayFab] Subiendo score directo: {statisticName}={score}");

        PlayFabClientAPI.UpdatePlayerStatistics(request,
            result => { Debug.Log("[PlayFab] Score subido correctamente"); },
            error =>
            {
                Debug.LogWarning("[PlayFab] Error al subir score, encolando: " + error.GenerateErrorReport());
                EnqueueScore(statisticName, score, sessionLength);
            });
    }

    private void Start()
    {
        StartCoroutine(ProcessQueueLoop());
    }

    private IEnumerator ProcessQueueLoop()
    {
        while (true)
        {
            if (queue.Count == 0) { yield return new WaitForSeconds(5f); continue; }

            var item = queue[0];
            bool finished = false;
            bool success = false;

            var request = new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate
                    {
                        StatisticName = item.statName,
                        Value = item.score
                    }
                }
            };

            PlayFabClientAPI.UpdatePlayerStatistics(request,
                res => { success = true; finished = true; },
                err => { success = false; finished = true; });

            float timer = 0f;
            while (!finished && timer < 10f) { timer += Time.deltaTime; yield return null; }

            if (success)
            {
                queue.RemoveAt(0);
                SaveQueue();
                Debug.Log("[PlayFab] Queued score enviado OK");
            }
            else
            {
                yield return new WaitForSeconds(5f);
            }
        }
    }

    #region Obtener Leaderboard
    public void GetLeaderboard(string statisticName, int top, Action<List<PlayerLeaderboardEntry>> onSuccess)
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = statisticName,
            StartPosition = 0,
            MaxResultsCount = top
        };

        PlayFabClientAPI.GetLeaderboard(request,
            result => { onSuccess?.Invoke(result.Leaderboard); },
            error => { Debug.LogWarning("Error obteniendo leaderboard: " + error.GenerateErrorReport()); });
    }
    #endregion
}
