using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    public static LeaderboardUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject panel;             // Panel principal del leaderboard
    public Transform contentParent;      // Content del ScrollView
    public GameObject playerRowPrefab;   // Prefab con Texts para Rank, Name, Score
    public Button closeButton;           // Botón para cerrar

    private Button openButton;           // Botón para abrir el ranking (por nombre)
    public TextMeshProUGUI myPositionText;

    private void Awake()
    {

        // Singleton
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Persistir entre escenas
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Seguridad: desactivar panel al inicio
        if (panel != null) panel.SetActive(false);

        // Asignar listener de cierre (ya asignado manualmente en inspector)
        if (closeButton != null)
            closeButton.onClick.AddListener(() => panel.SetActive(false));

        // Buscar el botón de abrir ranking por nombre en la escena
        GameObject openButtonObj = GameObject.Find("ButtonLeaderScore"); // <- reemplaza con el nombre real del botón
        if (openButtonObj != null)
        {
            openButton = openButtonObj.GetComponent<Button>();
            if (openButton != null)
            {
                openButton.onClick.AddListener(OnOpenButtonClicked);
            }
            else
            {
                Debug.LogWarning("LeaderboardUI: No se encontró componente Button en BotonRanking.");
            }
        }
        else
        {
            Debug.LogWarning("LeaderboardUI: No se encontró objeto con nombre 'BotonRanking'.");
        }
    }

    private void OnOpenButtonClicked()
    {
        ShowLeaderboard("HighScore", 10); // Cambia el nombre de la estadística si hace falta
    }

    public void OnColorLeaderboardButtonClicked()
    {
        LeaderboardUI.Instance.ShowLeaderboard("ColorScore", 10); // Top 10 del modo colores
    }

    public void OnGeometricLeaderboardButtonClicked()
    {
        LeaderboardUI.Instance.ShowLeaderboard("GeometricScore", 10); // Top 10 del modo colores
    }

    public void OnGridLeaderboardButtonClicked()
    {
        LeaderboardUI.Instance.ShowLeaderboard("GridScore", 10);
    }

    public void OnDodgeLeaderboardButtonClicked()
    {
        LeaderboardUI.Instance.ShowLeaderboard("DodgeScore", 10);
    }

    /// <summary>
    /// Abre el panel y carga el top N del leaderboard
    /// </summary>
    public void ShowLeaderboard(string statisticName, int top = 10)
    {
        if (panel == null || contentParent == null || playerRowPrefab == null)
        {
            Debug.LogWarning("LeaderboardUI: Falta asignar referencias en el inspector.");
            return;
        }

        panel.SetActive(true);

        // Limpiar filas anteriores
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        // Pedir leaderboard a PlayFab
        PlayFabScoreManager.Instance.GetLeaderboard(statisticName, top, leaderboard =>
        {
            for (int i = 0; i < leaderboard.Count; i++)
            {
                var entry = leaderboard[i];
                GameObject row = Instantiate(playerRowPrefab, contentParent);

                // Buscar referencias dentro del prefab
                var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
                var levelIcon = row.transform.Find("LevelIcon");
                var levelText = levelIcon?.GetComponentInChildren<TextMeshProUGUI>();

                // Asignar datos básicos
                texts[0].text = (entry.Position + 1).ToString();        // Rank
                texts[1].text = entry.DisplayName ?? "Player";          // Nombre
                texts[2].text = entry.StatValue.ToString();            // Score

                // ----- NUEVO: Obtener nivel desde otra estadística -----
                GetPlayerLevel(entry.PlayFabId, level =>
                {
                    if (levelText != null)
                        levelText.text = level.ToString(); // nivel dentro del iconito
                });
            }
        });



        PlayFabScoreManager.Instance.GetPlayerRank(statisticName, myEntry =>
        {
            if (myEntry != null)
            {
                int rank = myEntry.Position + 1; // PlayFab empieza en 0
                int score = myEntry.StatValue;
                myPositionText.text = $"Tu posición actual: {rank}º con {score} puntos";
            }
            else
            {
                myPositionText.text = "Aún no tienes puntuación en este modo.";
            }
        });
    }

    private void GetPlayerLevel(string playFabId, Action<int> onLevelFound)
    {
        var request = new PlayFab.ClientModels.GetUserDataRequest
        {
            PlayFabId = playFabId,
            Keys = new List<string> { "PlayerLevel" }
        };

        PlayFabClientAPI.GetUserData(request,
            result =>
            {
                int level = 1;
                if (result.Data != null && result.Data.ContainsKey("PlayerLevel"))
                {
                    int.TryParse(result.Data["PlayerLevel"].Value, out level);
                }
                onLevelFound?.Invoke(level);
            },
            error =>
            {
                Debug.LogWarning("Error obteniendo PlayerLevel de UserData: " + error.GenerateErrorReport());
                onLevelFound?.Invoke(1);
            }
        );
    }

    /// <summary>
    /// Método opcional para cerrar desde código
    /// </summary>
    public void CloseLeaderboard()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}
