using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class ColorManager : MonoBehaviour
{
    public static ColorManager Instance { get; private set; }

    public event EventHandler OnGameOver;

    [Header("UI Elements")]
    public TextMeshProUGUI colorWordText;
    public Image colorWordBackground;
    public TextMeshProUGUI scoreText;
    public Image timeBarImage;
    public float startTime = 60f;

    [Header("Candidate Buttons")]
    public List<Button> candidateButtons;

    [Header("Color Data")]
    public LocalizedString[] colorNames;
    public Color[] colorValues = {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        new Color(0.5f, 0f, 0.5f),
        new Color(1f, 0.5f, 0f),
        new Color(0.6f, 0.3f, 0.1f),
        Color.black,
        Color.white
    };

    private float currentTime;
    private int correctIndex;
    private int lastCorrectIndex = -1;

    // Bandera para evitar múltiples finales
    private bool hasEnded = false;
    // Bandera para saber si el minijuego ha empezado realmente (tras el Countdown)
    private bool hasStarted = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (ColorGameState.Instance != null)
        {
            ColorGameState.Instance.OnPlayingColorGame += HandleOnPlayingColorGame;
        }
    }

    private void Update()
    {
        if (hasEnded) return;
        if (!hasStarted) return; 

        currentTime -= Time.deltaTime;

        if (timeBarImage != null)
            timeBarImage.fillAmount = Mathf.Clamp01(currentTime / startTime);

        if (currentTime <= 0f)
        {
            EndGame();
        }
    }

    private void HandleOnPlayingColorGame(object sender, EventArgs e)
    {
        Debug.Log("PlayingColorGame");
        if (!hasStarted)
        {
            StartColorGame();
        }
    }

    private void StartColorGame()
    {
        hasEnded = false;
        hasStarted = true;

        currentTime = startTime;

        if (timeBarImage != null)
            timeBarImage.fillAmount = 1f;

        UpdateScoreText();
        SetupRound();
        UpdateCandidateButtonsSize();
    }

    private void SetupRound()
    {
        if (hasEnded) return;
        if (!hasStarted) return; 

        int availableCount = 6;
        if (ColorGamePuntos.Instance.GetScore() >= 10) availableCount = 7;
        if (ColorGamePuntos.Instance.GetScore() >= 20) availableCount = 8;
        if (ColorGamePuntos.Instance.GetScore() >= 30) availableCount = 9;

        // no repetir mismo color correcto
        do
        {
            correctIndex = UnityEngine.Random.Range(0, availableCount);
        } while (correctIndex == lastCorrectIndex);

        lastCorrectIndex = correctIndex;

        // nombre del color CORRECTO, ahora localizado
        string correctColorName = colorNames[correctIndex].GetLocalizedString();

        // Color del texto (distinto al correcto)
        int textColorIndex;
        do
        {
            textColorIndex = UnityEngine.Random.Range(0, availableCount);
        } while (textColorIndex == correctIndex);
        Color displayColor = colorValues[textColorIndex];

        colorWordText.text = correctColorName;
        colorWordText.color = displayColor;

        // Fondo (distinto al texto)
        int backgroundColorIndex;
        do
        {
            backgroundColorIndex = UnityEngine.Random.Range(0, colorValues.Length);
        } while (backgroundColorIndex == textColorIndex);
        colorWordBackground.color = colorValues[backgroundColorIndex];

        // Construcción de la lista de candidatos
        List<int> candidateIndices = new List<int> { correctIndex, textColorIndex };

        List<int> remainingIndices = new List<int>();
        for (int i = 0; i < availableCount; i++)
        {
            if (i != correctIndex && i != textColorIndex)
                remainingIndices.Add(i);
        }

        // Barajar remainingIndices
        for (int i = 0; i < remainingIndices.Count; i++)
        {
            int r = UnityEngine.Random.Range(i, remainingIndices.Count);
            (remainingIndices[i], remainingIndices[r]) = (remainingIndices[r], remainingIndices[i]);
        }

        if (remainingIndices.Count >= 2)
        {
            candidateIndices.Add(remainingIndices[0]);
            candidateIndices.Add(remainingIndices[1]);
        }
        else if (remainingIndices.Count == 1)
        {
            candidateIndices.Add(remainingIndices[0]);
        }

        // Barajar candidateIndices
        for (int i = 0; i < candidateIndices.Count; i++)
        {
            int r = UnityEngine.Random.Range(i, candidateIndices.Count);
            (candidateIndices[i], candidateIndices[r]) = (candidateIndices[r], candidateIndices[i]);
        }

        // Asignar colores a los botones
        for (int i = 0; i < candidateButtons.Count; i++)
        {
            int assignedIndex = candidateIndices[i];
            candidateButtons[i].image.color = colorValues[assignedIndex];
            candidateButtons[i].onClick.RemoveAllListeners();
            int indexCaptured = assignedIndex;
            candidateButtons[i].onClick.AddListener(() => OnCandidateSelected(indexCaptured));
        }
    }

    public void OnCandidateSelected(int selectedIndex)
    {
        if (hasEnded) return;   // Bloquear interacción tras game over
        if (!hasStarted) return; // Bloquear interacción durante Countdown / antes de empezar

        if (selectedIndex == correctIndex)
        {
            ColorGamePuntos.Instance.AddScore();

            UpdateScoreText();

            startTime = Mathf.Max(1f, startTime - 0.1f);
            currentTime = startTime;

            SetupRound();
            UpdateCandidateButtonsSize();
        }
        else
        {
            EndGame();
        }
    }

    private void UpdateCandidateButtonsSize()
    {
        float size1 = 419.92f;
        float size2 = 262.141f;

        if (ColorGamePuntos.Instance.GetScore() >= 50)
        {
            size1 = 220f;
            size2 = 130f;
        }
        else if (ColorGamePuntos.Instance.GetScore() >= 25)
        {
            size1 = 320f;
            size2 = 200f;
        }

        foreach (Button btn in candidateButtons)
        {
            RectTransform rt = btn.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size1, size2);
        }
    }

    private void UpdateScoreText()
    {
        ColorGamePuntos.Instance.ShowScore();
    }

    private void EndGame()
    {
        if (hasEnded) return; // Previene múltiples ejecuciones
        hasEnded = true;

        ColorGamePuntos.Instance.SafeRecordIfNeeded();

        OnGameOver?.Invoke(this, EventArgs.Empty);

        if (PlayFabLoginManager.Instance != null && PlayFabLoginManager.Instance.IsLoggedIn)
        {
            PlayFabScoreManager.Instance.SubmitScore("ColorScore", ColorGamePuntos.Instance.GetScore());
        }

        int coinsEarned = ColorGamePuntos.Instance.score;

        CoinsRewardUI rewardUI = FindObjectOfType<CoinsRewardUI>(true);
        if (rewardUI != null)
        {
            rewardUI.ShowReward(coinsEarned);
        }
        else
        {
            CurrencyManager.Instance.AddCoins(coinsEarned);
        }

        Debug.Log($"Fin de partida — Recompensa: {coinsEarned} monedas");
    }
}
