using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class ColorManager : MonoBehaviour
{
    public static ColorManager Instance { get; private set; }

    public event EventHandler OnGameOver;
    public event EventHandler OnVideo;

    [Header("Revive Countdown UI")]
    [SerializeField] private ReviveCountdownUI reviveCountdownUI;

    [Header("UI Elements")]
    public TextMeshProUGUI colorWordText;
    public Image colorWordBackground;
    public TextMeshProUGUI scoreText;
    public Image timeBarImage;
    public float startTime = 60f;
    public TextMeshProUGUI TimeText;

    [Header("Candidate Buttons")]
    public List<Button> candidateButtons;

    [Header("Color Data")]
    public LocalizedString[] colorNames;
    public Color[] colorValues;

    private float currentTime;
    private int correctIndex;
    private int lastCorrectIndex = -1;

    private bool hasEnded = false;
    private bool hasStarted = false;

    // 1 revive máximo por partida (opcional, pero recomendado)
    private bool hasUsedReviveOffer = false;

    public enum DeathType
    {
        None,
        Video,
        GameOver
    }

    public DeathType deathType { get; private set; } = DeathType.None;

    private void Awake()
    {
        Instance = this;
        deathType = DeathType.None;
    }

    private void Start()
    {
        if (ColorGameState.Instance != null)
            ColorGameState.Instance.OnPlayingColorGame += HandleOnPlayingColorGame;
    }

    private void Update()
    {
        if (hasEnded || !hasStarted)
            return;

        // Si estamos en Video/GameOver, el juego está congelado
        if (deathType != DeathType.None)
            return;

        currentTime -= Time.deltaTime;

        if (timeBarImage != null)
            timeBarImage.fillAmount = Mathf.Clamp01(currentTime / startTime);

        if (currentTime <= 0f)
        {
            DecideDeathType();
        }
    }

    private void HandleOnPlayingColorGame(object sender, EventArgs e)
    {
        if (!hasStarted)
            StartColorGame();
    }

    private void StartColorGame()
    {
        hasEnded = false;
        hasStarted = true;
        deathType = DeathType.None;

        hasUsedReviveOffer = false;

        currentTime = startTime;

        if (timeBarImage != null)
            timeBarImage.fillAmount = 1f;

        UpdateScoreText();
        SetupRound();
        UpdateCandidateButtonsSize();
    }

    private void DecideDeathType()
    {
        if (deathType != DeathType.None)
            return;

        // Si ya tuvo revive en esta partida, no vuelve a salir
        if (hasUsedReviveOffer)
        {
            SetDeathType(DeathType.GameOver);
            return;
        }

        float videoProbability = UnityEngine.Random.Range(0, 10);

        if (videoProbability >= 5)
        {
            hasUsedReviveOffer = true;
            SetDeathType(DeathType.Video);
        }
        else
        {
            SetDeathType(DeathType.GameOver);
        }
    }

    public void SetDeathType(DeathType newType)
    {
        if (deathType == newType)
            return;

        deathType = newType;

        switch (deathType)
        {
            case DeathType.Video:
                OnVideo?.Invoke(this, EventArgs.Empty);
                break;

            case DeathType.GameOver:
                EndGame();
                break;
        }
    }

    // Llamado desde VideoGameOver cuando el rewarded termina
    public void StartReviveCountdown()
    {
        // Solo tiene sentido si estamos en el estado Video
        if (deathType != DeathType.Video)
            return;

        // Si no hay UI asignada, revive directo (fallback)
        if (reviveCountdownUI == null)
        {
            Revive();
            return;
        }

        // Sigue congelado durante la cuenta atrás.
        reviveCountdownUI.Play(() =>
        {
            Revive();
        });
    }

    public void Revive()
    {
        if (deathType != DeathType.Video)
            return;

        // Reanudar
        deathType = DeathType.None;

        // margen de tiempo para seguir jugando (ajústalo a tu gusto)
        currentTime = Mathf.Max(currentTime, startTime * 0.5f);

        // opcional: podrías regenerar ronda para evitar edge cases
        // SetupRound();
    }

    // --- TU SetupRound (con shuffle corregido) ---
    private void SetupRound()
    {
        if (hasEnded || !hasStarted)
            return;

        int availableCount = 6;
        if (ColorGamePuntos.Instance.GetScore() >= 10) availableCount = 7;
        if (ColorGamePuntos.Instance.GetScore() >= 20) availableCount = 8;
        if (ColorGamePuntos.Instance.GetScore() >= 30) availableCount = 9;

        do
        {
            correctIndex = UnityEngine.Random.Range(0, availableCount);
        } while (correctIndex == lastCorrectIndex);

        lastCorrectIndex = correctIndex;

        string correctColorName = colorNames[correctIndex].GetLocalizedString();

        int textColorIndex;
        do
        {
            textColorIndex = UnityEngine.Random.Range(0, availableCount);
        } while (textColorIndex == correctIndex);

        colorWordText.text = correctColorName;
        colorWordText.color = colorValues[textColorIndex];

        int backgroundColorIndex;
        do
        {
            backgroundColorIndex = UnityEngine.Random.Range(0, colorValues.Length);
        } while (backgroundColorIndex == textColorIndex);

        colorWordBackground.color = colorValues[backgroundColorIndex];

        List<int> candidateIndices = new List<int> { correctIndex, textColorIndex };
        List<int> remaining = new List<int>();

        for (int i = 0; i < availableCount; i++)
            if (i != correctIndex && i != textColorIndex)
                remaining.Add(i);

        while (candidateIndices.Count < candidateButtons.Count && remaining.Count > 0)
        {
            int r = UnityEngine.Random.Range(0, remaining.Count);
            candidateIndices.Add(remaining[r]);
            remaining.RemoveAt(r);
        }

        // SHUFFLE (clave)
        for (int i = 0; i < candidateIndices.Count; i++)
        {
            int r = UnityEngine.Random.Range(i, candidateIndices.Count);
            (candidateIndices[i], candidateIndices[r]) = (candidateIndices[r], candidateIndices[i]);
        }

        for (int i = 0; i < candidateButtons.Count; i++)
        {
            int index = candidateIndices[i];
            candidateButtons[i].image.color = colorValues[index];
            candidateButtons[i].onClick.RemoveAllListeners();
            candidateButtons[i].onClick.AddListener(() => OnCandidateSelected(index));
        }
    }

    public void OnCandidateSelected(int selectedIndex)
    {
        if (hasEnded || !hasStarted || deathType != DeathType.None)
            return;

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
            DecideDeathType();
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
            btn.GetComponent<RectTransform>().sizeDelta = new Vector2(size1, size2);
    }

    private void UpdateScoreText()
    {
        ColorGamePuntos.Instance.ShowScore();
    }

    private void EndGame()
    {
        if (hasEnded)
            return;

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
            rewardUI.ShowReward(coinsEarned);
        else
            CurrencyManager.Instance.AddCoins(coinsEarned);
    }
}
