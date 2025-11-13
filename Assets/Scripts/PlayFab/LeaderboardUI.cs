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
    public Transform contentParent;
    public GameObject playerRowPrefab;
    public TextMeshProUGUI myPositionText;

    [Header("Modo Botones")]
    public Button classicButton;
    public Button colorButton;
    public Button geometricButton;
    public Button gridButton;
    public Button dodgeButton;

    private Button currentSelectedButton;

    private bool isLoading = false;             // Evita llamadas simultáneas
    private string currentRequestedStat = "";   // Guarda el modo solicitado actual

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
        if (myPositionText != null)
            myPositionText.text = "¡Toca un botón para ver su tabla de clasificación online!";

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);
    }

    private void OnModeButtonClicked(string statisticName, Button clickedButton)
    {
        if (isLoading) return; // Ignorar clicks mientras carga

        // Cambiar color del botón seleccionado
        if (currentSelectedButton != null)
            SetButtonSelected(currentSelectedButton, false);

        SetButtonSelected(clickedButton, true);
        currentSelectedButton = clickedButton;

        ShowLeaderboard(statisticName, 10);
    }

    private void SetButtonSelected(Button button, bool selected)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = selected ? new Color(0.8f, 0.8f, 1f) : Color.white;
        colors.selectedColor = colors.normalColor;
        button.colors = colors;
    }

    public void ShowLeaderboard(string statisticName, int top = 10)
    {
        if (contentParent == null || playerRowPrefab == null)
        {
            Debug.LogWarning("LeaderboardUI: Falta asignar referencias.");
            return;
        }

        isLoading = true;
        currentRequestedStat = statisticName;

        // Limpiar contenido anterior
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        myPositionText.text = "Cargando puntuaciones...";

        if (PlayFabScoreManager.Instance == null)
        {
            StartCoroutine(DelayedShow(statisticName, top));
            return;
        }

        PlayFabScoreManager.Instance.GetLeaderboard(statisticName, top, leaderboard =>
        {
            // Verifica que este resultado corresponde al último botón pulsado
            if (statisticName != currentRequestedStat)
                return;

            foreach (Transform child in contentParent)
                Destroy(child.gameObject);

            if (leaderboard == null || leaderboard.Count == 0)
            {
                myPositionText.text = "No hay puntuaciones todavía.";
            }
            else
            {
                int count = Mathf.Min(leaderboard.Count, top);
                for (int i = 0; i < count; i++)
                {
                    var entry = leaderboard[i];
                    GameObject row = Instantiate(playerRowPrefab, contentParent);

                    var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
                    var levelIcon = row.transform.Find("LevelIcon");
                    var levelText = levelIcon?.GetComponentInChildren<TextMeshProUGUI>();

                    if (texts != null && texts.Length >= 3)
                    {
                        texts[0].text = (entry.Position + 1).ToString();
                        texts[1].text = entry.DisplayName ?? "Player";
                        texts[2].text = entry.StatValue.ToString();
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
            }

            Canvas.ForceUpdateCanvases();
            if (contentParent is RectTransform rt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

            isLoading = false; // Libera bloqueo
        });

        // Mostrar posición del jugador
        if (PlayFabScoreManager.Instance != null)
        {
            PlayFabScoreManager.Instance.GetPlayerRank(statisticName, myEntry =>
            {
                if (statisticName != currentRequestedStat) return; // Evita resultados antiguos

                if (myEntry != null)
                    myPositionText.text = $"Tu posición actual: {myEntry.Position + 1}º con {myEntry.StatValue} puntos";
                else
                    myPositionText.text = "Aún no tienes puntuación en este modo.";
            });
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
                    int.TryParse(result.Data["PlayerLevel"].Value, out level);
                onLevelFound?.Invoke(level);
            },
            error =>
            {
                onLevelFound?.Invoke(1);
            }
        );
    }
}
