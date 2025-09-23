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
    private DateTime sessionStart; // añadimos esto

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadQueue();

            sessionStart = DateTime.UtcNow; // guardamos inicio sesión
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void LoadQueue()
    {
        string json = PlayerPrefs.GetString(QUEUE_KEY, "");
        if (!string.IsNullOrEmpty(json)) queue = JsonUtility.FromJson<Wrapper>(json).items;
    }
    void SaveQueue()
    {
        PlayerPrefs.SetString(QUEUE_KEY, JsonUtility.ToJson(new Wrapper { items = queue }));
        PlayerPrefs.Save();
    }
    [Serializable] class Wrapper { public List<ScoreQueueItem> items = new List<ScoreQueueItem>(); }

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

    // Llamar al final de partida
    public void SubmitScore(string statisticName, int score, int sessionLength = 0, bool forceSend = false)
    {
        if (!PlayFabLoginManager.Instance.IsLoggedIn)
        {
            Debug.LogWarning("Intentando enviar score antes de loguear.");
            EnqueueScore(statisticName, score, sessionLength);
            return;
        }

        // calculamos duración de sesión si no se pasó manualmente
        if (sessionLength <= 0)
        {
            sessionLength = (int)(DateTime.UtcNow - sessionStart).TotalSeconds;
        }

        string key = "best_" + statisticName;
        int bestLocal = PlayerPrefs.GetInt(key, 0);
        if (!forceSend && score <= bestLocal)
        {
            Debug.Log($"Score menor o igual al mejor local ({bestLocal}). Ignorado.");
            return;
        }

        PlayerPrefs.SetInt(key, score);
        PlayerPrefs.Save();

        var req = new ExecuteCloudScriptRequest
        {
            FunctionName = "submitScore",
            FunctionParameter = new
            {
                statName = statisticName,
                score = score,
                sessionLength = sessionLength,
                clientBest = bestLocal
            },
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(req, result =>
        {
            if (result.FunctionResult != null)
            {
                try
                {
                    string raw = PlayFabSimpleJson.SerializeObject(result.FunctionResult);
                    Debug.Log("CloudScript result: " + raw);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Error parseando FunctionResult: " + ex.Message);
                    Debug.Log("FunctionResult (ToString): " + result.FunctionResult.ToString());
                }
            }
            else
            {
                Debug.LogWarning("CloudScript devolvió null en FunctionResult");
            }
        },
        error =>
        {
            Debug.LogWarning("CloudScript failed, encolando: " + error.GenerateErrorReport());
            EnqueueScore(statisticName, score, sessionLength);
        });
    }

    // Cola procesadora simple
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

            var req = new ExecuteCloudScriptRequest
            {
                FunctionName = "submitScore",
                FunctionParameter = new
                {
                    statName = item.statName,
                    score = item.score,
                    sessionLength = item.sessionLength
                },
                GeneratePlayStreamEvent = true
            };

            PlayFabClientAPI.ExecuteCloudScript(req,
                res => { success = true; finished = true; },
                err => { success = false; finished = true; });

            float timer = 0f;
            while (!finished && timer < 10f) { timer += Time.deltaTime; yield return null; }

            if (success)
            {
                queue.RemoveAt(0);
                SaveQueue();
                Debug.Log("Queued score enviado OK");
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
