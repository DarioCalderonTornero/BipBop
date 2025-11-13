using System;
using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

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

    [Header("Localization")]
    public LocalizedString touchButtonPrompt;      // "¡Toca un botón..." / "Tap a button..."
    public LocalizedString loadingScoresText;      // "Cargando..." / "Loading..."
    public LocalizedString noScoresText;           // "No hay puntuaciones..." / "No scores yet..."
    public LocalizedString noScoreThisModeText;    // "Aún no tienes puntuación..." / "You don't have a score yet..."
    public LocalizedString leaderboardHasScore;    // Smart String con rank y score

    private MyPosState myPosState = MyPosState.Prompt;
    private int lastRank = -1;
    private int lastScore = 0;

    private Button currentSelectedButton;

    private bool isLoading = false;
    private string currentRequestedStat = "";

    public enum MyPosState
    {
        Prompt,             // "Toca un botón..."
        Loading,            // "Cargando..."
        NoScores,           // "No hay puntuaciones..."
        HasScore,           // "Tu posición actual: ..."
        NoScoreThisMode     // "Aún no tienes puntuación..."
    }

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
        {
            myPosState = MyPosState.Prompt;
            myPositionText.text = touchButtonPrompt.GetLocalizedString();
        }

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

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        myPosState = MyPosState.Loading;
        myPositionText.text = loadingScoresText.GetLocalizedString();

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
                myPosState = MyPosState.NoScores;
                myPositionText.text = noScoresText.GetLocalizedString();
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
                {
                    myPosState = MyPosState.HasScore;
                    lastRank = myEntry.Position + 1;
                    lastScore = myEntry.StatValue;

                    // Smart String: una sola entrada localizada que recibe {0} (rank) y {1} (score)
                    myPositionText.text = leaderboardHasScore.GetLocalizedString(lastRank, lastScore);
                }
                else
                {
                    myPosState = MyPosState.NoScoreThisMode;
                    myPositionText.text = noScoreThisModeText.GetLocalizedString();
                }
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

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale newLocale)
    {
        // Cuando cambia el idioma, rehacemos el texto según el estado actual
        switch (myPosState)
        {
            case MyPosState.Prompt:
                myPositionText.text = touchButtonPrompt.GetLocalizedString();
                break;

            case MyPosState.Loading:
                myPositionText.text = loadingScoresText.GetLocalizedString();
                break;

            case MyPosState.NoScores:
                myPositionText.text = noScoresText.GetLocalizedString();
                break;

            case MyPosState.NoScoreThisMode:
                myPositionText.text = noScoreThisModeText.GetLocalizedString();
                break;

            case MyPosState.HasScore:
                // Volvemos a pedir la Smart String con los mismos parámetros
                myPositionText.text = leaderboardHasScore.GetLocalizedString(lastRank, lastScore);
                break;
        }
    }
}
