using System;
using System.Collections;
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
    public Transform contentParent;      // Content donde se instancian las filas
    public GameObject playerRowPrefab;   // Prefab con Rank, Name, Score, Level
    public TextMeshProUGUI myPositionText;

    [Header("Modo Botones")]
    public Button classicButton;
    public Button colorButton;
    public Button geometricButton;
    public Button gridButton;
    public Button dodgeButton;

    private Button currentSelectedButton;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        // Listeners de los botones
        classicButton.onClick.AddListener(() => OnModeButtonClicked("HighScore", classicButton));
        colorButton.onClick.AddListener(() => OnModeButtonClicked("ColorScore", colorButton));
        geometricButton.onClick.AddListener(() => OnModeButtonClicked("GeometricScore", geometricButton));
        gridButton.onClick.AddListener(() => OnModeButtonClicked("GridScore", gridButton));
        dodgeButton.onClick.AddListener(() => OnModeButtonClicked("DodgeScore", dodgeButton));
    }

    private void Start()
    {
        // Mostrar mensaje inicial hasta que el jugador pulse un modo
        if (myPositionText != null)
            myPositionText.text = "¡Toca un botón para ver su tabla de clasificación online!";

        // Limpiar contenido inicial por si acaso
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);
    }

    private void OnModeButtonClicked(string statisticName, Button clickedButton)
    {
        // Cambiar color del botón seleccionado
        if (currentSelectedButton != null)
            SetButtonSelected(currentSelectedButton, false);

        SetButtonSelected(clickedButton, true);
        currentSelectedButton = clickedButton;

        // Cargar leaderboard de ese modo
        ShowLeaderboard(statisticName, 10);
    }

    private void SetButtonSelected(Button button, bool selected)
    {
        ColorBlock colors = button.colors;
        if (selected)
        {
            colors.normalColor = new Color(0.8f, 0.8f, 1f);  // azul clarito
            colors.selectedColor = new Color(0.8f, 0.8f, 1f);
        }
        else
        {
            colors.normalColor = Color.white;
            colors.selectedColor = Color.white;
        }
        button.colors = colors;
    }

    public void ShowLeaderboard(string statisticName, int top = 10)
    {
        if (contentParent == null || playerRowPrefab == null)
        {
            Debug.LogWarning("LeaderboardUI: Falta asignar referencias en el inspector.");
            return;
        }

        // Limpiar filas anteriores
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        // Seguridad: si PlayFabScoreManager no está, evitar excepción y reintentar en un frame
        if (PlayFabScoreManager.Instance == null)
        {
            Debug.LogWarning("PlayFabScoreManager no está listo. Reintentando en el siguiente frame.");
            StartCoroutine(DelayedShow(statisticName, top));
            return;
        }

        // Obtener top de PlayFab
        PlayFabScoreManager.Instance.GetLeaderboard(statisticName, top, leaderboard =>
        {
            // Si no hay resultados, muestra mensaje sencillo
            if (leaderboard == null || leaderboard.Count == 0)
            {
                myPositionText.text = "No hay puntuaciones todavía.";
            }

            for (int i = 0; i < leaderboard.Count; i++)
            {
                var entry = leaderboard[i];
                GameObject row = Instantiate(playerRowPrefab, contentParent);

                var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
                var levelIcon = row.transform.Find("LevelIcon");
                var levelText = levelIcon?.GetComponentInChildren<TextMeshProUGUI>();

                if (texts != null && texts.Length >= 3)
                {
                    texts[0].text = (entry.Position + 1).ToString();       // Rank
                    texts[1].text = entry.DisplayName ?? "Player";         // Nombre
                    texts[2].text = entry.StatValue.ToString();            // Score
                }

                if (!string.IsNullOrEmpty(entry.PlayFabId))
                {
                    GetPlayerLevel(entry.PlayFabId, level =>
                    {
                        if (levelText != null)
                            levelText.text = level.ToString();
                    });
                }
            }

            Canvas.ForceUpdateCanvases();
            var rt = contentParent as RectTransform;
            if (rt != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        });

        // Mostrar posición del jugador
        if (PlayFabScoreManager.Instance != null)
        {
            PlayFabScoreManager.Instance.GetPlayerRank(statisticName, myEntry =>
            {
                if (myEntry != null)
                {
                    int rank = myEntry.Position + 1;
                    int score = myEntry.StatValue;
                    myPositionText.text = $"Tu posición actual: {rank}º con {score} puntos";
                }
                else
                {
                    myPositionText.text = "Aún no tienes puntuación en este modo.";
                }
            });
        }
        else
        {
            myPositionText.text = "Cargando posición...";
        }
    }

    private IEnumerator DelayedShow(string statisticName, int top)
    {
        yield return new WaitForEndOfFrame();
        ShowLeaderboard(statisticName, top);
    }

    private void GetPlayerLevel(string playFabId, Action<int> onLevelFound)
    {
        var request = new GetUserDataRequest
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
                Debug.LogWarning("Error obteniendo PlayerLevel: " + error.GenerateErrorReport());
                onLevelFound?.Invoke(1);
            }
        );
    }
}
