using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class ColorManager : MonoBehaviour, IGameOverClient
{
    public static ColorManager Instance { get; private set; }

    public event EventHandler OnGameOver;

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

    [Header("Sprites de colores")]
    [SerializeField] private Sprite[] colorSprites;

    [SerializeField] private AudioClip errorAudioClip;

    private float currentTime;
    private int correctIndex;
    private int lastCorrectIndex = -1;

    private bool hasEnded = false;
    private bool hasStarted = false;

    // --- NUEVO: pausa específica cuando el flow está mostrando oferta ---
    private bool isPausedByOffer = false;

    // --- NUEVO: limitar a 1 offer por run (lo usa el manager global) ---
    private bool hasUsedReviveOffer = false;

    public bool HasUsedReviveOffer
    {
        get => hasUsedReviveOffer;
        set => hasUsedReviveOffer = value;
    }

    public enum RoundMode
    {
        BackgroundColor,
        WordMeaning,
        TextColor
    }

    [Header("Modo desbloqueo ruleta")]
    [SerializeField] private int modeWheelUnlockScore = 20;

    [Header("Modo de ronda")]
    [SerializeField] private Image[] modeIcons;
    [SerializeField] private float modeActiveScale = 1.15f;
    [SerializeField] private float modeInactiveScale = 1f;
    [SerializeField] private Color modeActiveColor = Color.white;
    [SerializeField] private Color modeInactiveColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private float modeAnimDuration = 0.12f;

    [Header("Sprites de fondo (referencia)")]
    [SerializeField] private Sprite[] backgroundSprites;

    private RoundMode currentMode;

    [Header("Combo")]
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private RectTransform comboTransform;
    [SerializeField] private float maxTimeBetweenHits = 6f;
    [SerializeField] private float comboPopScale = 1.15f;
    [SerializeField] private float comboPopDuration = 0.1f;

    private int comboCount = 0;
    private float timeSinceLastHit = 0f;
    private bool isComboActive = false;
    private Coroutine comboAnimRoutine;

    [SerializeField] private float comboShakeAmount = 0.05f;
    [SerializeField] private float comboShakeSpeed = 15f;

    private Coroutine comboShakeRoutine;

    [Header("Countdown Candidates Shuffle")]
    [SerializeField] private float candidatesShuffleInterval = 0.06f;
    [SerializeField] private float candidatesShuffleDuration = 3f;
    private Coroutine candidatesShuffleRoutine;

    [Header("Tutorial Panel")]
    [SerializeField] private TutorialPanelUI tutorialPrefab;
    [SerializeField] private Transform tutorialParent;
    private TutorialPanelUI tutorialInstance;
    private const string ShowTutorialKey = "ShowTutorialOnStart";

    private void Awake()
    {
        Instance = this;
        currentMode = RoundMode.WordMeaning;

        // Estado limpio
        isPausedByOffer = false;
        hasUsedReviveOffer = false;
    }

    private void Start()
    {
        if (ColorGameState.Instance != null)
            ColorGameState.Instance.OnPlayingColorGame += HandleOnPlayingColorGame;

        bool showTutorialOnStart = PlayerPrefs.GetInt(ShowTutorialKey, 1) == 1;

        if (showTutorialOnStart && tutorialPrefab != null)
        {
            ShowTutorial();
        }
        else
        {
            HideAnyExistingTutorialPanel();
            BeginGameAfterTutorial();
        }
    }

    private void ShowTutorial()
    {
        if (tutorialInstance != null) return;

        var existing = FindObjectOfType<TutorialPanelUI>(true);
        if (existing != null)
        {
            tutorialInstance = existing;
            tutorialInstance.gameObject.SetActive(true);
        }
        else
        {
            Transform parent = tutorialParent;
            if (parent == null)
            {
                Canvas c = FindObjectOfType<Canvas>();
                parent = (c != null) ? c.transform : transform;
            }
            tutorialInstance = Instantiate(tutorialPrefab, parent);
        }

        tutorialInstance.OnClosed -= HandleTutorialClosed;
        tutorialInstance.OnClosed += HandleTutorialClosed;

        PauseGameplay();
    }

    private void HandleTutorialClosed()
    {
        if (tutorialInstance != null)
            tutorialInstance.OnClosed -= HandleTutorialClosed;

        tutorialInstance = null;
        BeginGameAfterTutorial();
    }

    private void HideAnyExistingTutorialPanel()
    {
        var existing = FindObjectOfType<TutorialPanelUI>(true);
        if (existing != null)
            existing.gameObject.SetActive(false);
    }

    private void BeginGameAfterTutorial()
    {
        StartCountdownFlow();
    }

    private void PauseGameplay()
    {
        hasStarted = false;
    }

    // ==================
    // Countdown flow
    // ==================
    private void StartCountdownFlow()
    {
        hasEnded = false;
        hasStarted = false;

        // Reset run state
        isPausedByOffer = false;
        hasUsedReviveOffer = false;

        if (timeBarImage != null)
            timeBarImage.fillAmount = 1f;

        if (comboText != null)
            comboText.gameObject.SetActive(false);

        if (candidatesShuffleRoutine != null) StopCoroutine(candidatesShuffleRoutine);
        candidatesShuffleRoutine = StartCoroutine(ShuffleCandidatesDuringCountdown());

        if (ColorGameState.Instance != null)
            ColorGameState.Instance.StartCountdown();
    }

    private IEnumerator ShuffleCandidatesDuringCountdown()
    {
        if (candidateButtons == null || candidateButtons.Count == 0 || colorSprites == null || colorSprites.Length == 0)
            yield break;

        float t = 0f;
        Sprite[] current = new Sprite[candidateButtons.Count];

        while (t < candidatesShuffleDuration && !hasStarted && !hasEnded)
        {
            for (int i = current.Length - 1; i > 0; i--)
                current[i] = current[i - 1];

            current[0] = colorSprites[UnityEngine.Random.Range(0, colorSprites.Length)];

            for (int i = 0; i < candidateButtons.Count; i++)
            {
                var btn = candidateButtons[i];
                if (btn == null) continue;

                var img = btn.image;
                if (img == null) continue;

                img.sprite = current[i];
                img.color = Color.white;
            }

            yield return new WaitForSeconds(candidatesShuffleInterval);
            t += candidatesShuffleInterval;
        }

        candidatesShuffleRoutine = null;
    }

    private void Update()
    {
        if (hasEnded || !hasStarted) return;
        if (isPausedByOffer) return; // ✅ equivalente al "deathType != None" que querías

        currentTime -= Time.deltaTime;
        timeSinceLastHit += Time.deltaTime;

        if (timeBarImage != null)
            timeBarImage.fillAmount = Mathf.Clamp01(currentTime / startTime);

        if (isComboActive && timeSinceLastHit > maxTimeBetweenHits)
            EndCombo();

        if (currentTime <= 0f)
            TriggerFail();
    }

    private void HandleOnPlayingColorGame(object sender, EventArgs e)
    {
        if (!hasStarted)
            StartColorGame();
    }

    private void StartColorGame()
    {
        if (candidatesShuffleRoutine != null)
        {
            StopCoroutine(candidatesShuffleRoutine);
            candidatesShuffleRoutine = null;
        }

        hasEnded = false;
        hasStarted = true;

        // Reset run state
        isPausedByOffer = false;
        hasUsedReviveOffer = false;

        currentTime = startTime;
        timeSinceLastHit = 0f;
        comboCount = 0;
        isComboActive = false;
        if (comboText != null) comboText.gameObject.SetActive(false);

        if (timeBarImage != null)
            timeBarImage.fillAmount = 1f;

        UpdateScoreText();
        SetupRound();
    }

    /// <summary>
    /// Punto único de fallo: lo llamamos desde timeout y desde click incorrecto.
    /// </summary>
    private void TriggerFail()
    {
        if (hasEnded) return;
        if (!hasStarted) return;
        if (isPausedByOffer) return; // ya en flow

        // Sonido de error como antes (tú lo ponías al cambiar DeathType)
        if (SoundManager.Instance != null && errorAudioClip != null)
            SoundManager.Instance.PlaySound(errorAudioClip, 1f);

        // Delegamos al sistema central
        if (GameOverFlowManager.Instance != null)
        {
            GameOverFlowManager.Instance.NotifyFail(this);
        }
        else
        {
            // Fallback: si por lo que sea no está el manager global, gameover directo
            FinalGameOver();
        }
    }

    // =========================
    // IGameOverClient (Flow API)
    // =========================
    public void PauseOnFail()
    {
        // Esto es lo que pedías: parar Update sin tocar timeScale.
        isPausedByOffer = true;
    }

    public void FinalGameOver()
    {
        // Al llegar aquí, ya es fin definitivo
        isPausedByOffer = false;
        EndGame();
    }

    public void Revive()
    {
        // EXACTAMENTE tu revive anterior
        isPausedByOffer = false;

        // margen de tiempo para seguir jugando
        currentTime = Mathf.Max(currentTime, startTime * 0.5f);

        // Si quisieras regenerar ronda opcional:
        // SetupRound();
    }

    // --- TU SetupRound (tal cual lo tenías) ---
    private void SetupRound()
    {
        if (hasEnded || !hasStarted)
            return;

        int availableCount = 6;
        if (ColorGamePuntos.Instance.GetScore() >= 10) availableCount = 7;
        if (ColorGamePuntos.Instance.GetScore() >= 20) availableCount = 8;
        if (ColorGamePuntos.Instance.GetScore() >= 30) availableCount = 9;

        if (colorValues != null) availableCount = Mathf.Min(availableCount, colorValues.Length);
        if (colorNames != null) availableCount = Mathf.Min(availableCount, colorNames.Length);

        int backgroundColorIndex, textColorIndex, wordColorIndex;

        wordColorIndex = UnityEngine.Random.Range(0, availableCount);
        string correctColorName = colorNames[wordColorIndex].GetLocalizedString();

        do
        {
            textColorIndex = UnityEngine.Random.Range(0, availableCount);
        } while (textColorIndex == wordColorIndex);

        do
        {
            backgroundColorIndex = UnityEngine.Random.Range(0, availableCount);
        } while (backgroundColorIndex == textColorIndex || backgroundColorIndex == wordColorIndex);

        colorWordText.text = correctColorName;
        colorWordText.color = colorValues[textColorIndex];

        if (backgroundSprites != null &&
            backgroundColorIndex < backgroundSprites.Length &&
            backgroundSprites[backgroundColorIndex] != null)
        {
            colorWordBackground.sprite = backgroundSprites[backgroundColorIndex];
            colorWordBackground.color = Color.white;
        }
        else
        {
            colorWordBackground.sprite = null;
            colorWordBackground.color = colorValues[backgroundColorIndex];
        }

        int score = ColorGamePuntos.Instance.GetScore();

        if (score < 15)
        {
            currentMode = RoundMode.WordMeaning;
        }
        else if (score < 25)
        {
            if (currentMode == RoundMode.WordMeaning)
                currentMode = RoundMode.BackgroundColor;
            else
                currentMode = RoundMode.WordMeaning;
        }
        else
        {
            switch (currentMode)
            {
                case RoundMode.BackgroundColor:
                    currentMode = RoundMode.WordMeaning;
                    break;
                case RoundMode.WordMeaning:
                    currentMode = RoundMode.TextColor;
                    break;
                case RoundMode.TextColor:
                default:
                    currentMode = RoundMode.BackgroundColor;
                    break;
            }
        }

        UpdateModeIconsUI();

        switch (currentMode)
        {
            case RoundMode.BackgroundColor:
                correctIndex = backgroundColorIndex;
                break;
            case RoundMode.WordMeaning:
                correctIndex = wordColorIndex;
                break;
            case RoundMode.TextColor:
                correctIndex = textColorIndex;
                break;
        }

        lastCorrectIndex = correctIndex;

        List<int> candidateIndices = new List<int> { correctIndex };
        List<int> remaining = new List<int>();

        for (int i = 0; i < availableCount; i++)
            if (i != correctIndex)
                remaining.Add(i);

        while (candidateIndices.Count < candidateButtons.Count && remaining.Count > 0)
        {
            int r = UnityEngine.Random.Range(0, remaining.Count);
            candidateIndices.Add(remaining[r]);
            remaining.RemoveAt(r);
        }

        for (int i = 0; i < candidateIndices.Count; i++)
        {
            int r = UnityEngine.Random.Range(i, candidateIndices.Count);
            (candidateIndices[i], candidateIndices[r]) = (candidateIndices[r], candidateIndices[i]);
        }

        for (int i = 0; i < candidateButtons.Count; i++)
        {
            Button btn = candidateButtons[i];

            if (i >= candidateIndices.Count)
            {
                btn.gameObject.SetActive(false);
                continue;
            }
            else
            {
                btn.gameObject.SetActive(true);
            }

            int index = candidateIndices[i];

            var img = btn.image;
            if (colorSprites != null && index < colorSprites.Length && colorSprites[index] != null)
            {
                img.sprite = colorSprites[index];
                img.color = Color.white;
            }
            else
            {
                img.sprite = null;
                img.color = colorValues[index];
            }

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                var fx = btn.GetComponent<ColorOptionButtonFX>();
                if (fx != null) fx.PlayPop();

                OnCandidateSelected(index);
            });
        }
    }

    private void UpdateModeIconsUI()
    {
        if (modeIcons == null || modeIcons.Length < 3)
            return;

        for (int i = 0; i < modeIcons.Length; i++)
        {
            Image img = modeIcons[i];
            if (img == null) continue;

            bool isActive = (int)currentMode == i;

            float targetScale = isActive ? modeActiveScale : modeInactiveScale;
            Color targetColor = isActive ? modeActiveColor : modeInactiveColor;

            StartCoroutine(AnimateModeIcon(img.rectTransform, img, targetScale, targetColor));
        }
    }

    private IEnumerator AnimateModeIcon(RectTransform rt, Image img, float targetScale, Color targetColor)
    {
        Vector3 startScale = rt.localScale;
        Color startColor = img.color;
        float t = 0f;

        while (t < modeAnimDuration)
        {
            float k = t / modeAnimDuration;
            float eased = k * k * (3f - 2f * k);

            rt.localScale = Vector3.Lerp(startScale, Vector3.one * targetScale, eased);
            img.color = Color.Lerp(startColor, targetColor, eased);

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        rt.localScale = Vector3.one * targetScale;
        img.color = targetColor;
    }

    public void OnCandidateSelected(int selectedIndex)
    {
        if (hasEnded || !hasStarted) return;
        if (isPausedByOffer) return;

        if (selectedIndex == correctIndex)
        {
            foreach (var btn in candidateButtons)
            {
                var fx = btn.GetComponent<ColorOptionButtonFX>();
                if (fx != null) fx.PlayShake();
            }

            if (timeSinceLastHit <= maxTimeBetweenHits) comboCount++;
            else comboCount = 1;

            timeSinceLastHit = 0f;

            if (comboCount >= 3)
            {
                if (!isComboActive)
                {
                    isComboActive = true;
                    UpdateComboText();
                    PlayComboPop();

                    if (comboShakeRoutine != null)
                        StopCoroutine(comboShakeRoutine);
                    comboShakeRoutine = StartCoroutine(ComboShakeLoop());
                }
                else
                {
                    UpdateComboText();
                    PlayComboPop();
                }
            }
            else
            {
                EndCombo(instant: true);
            }

            if (isComboActive)
            {
                ColorGamePuntos.Instance.AddScore();
                ColorGamePuntos.Instance.AddScoreRaw(1);
            }
            else
            {
                ColorGamePuntos.Instance.AddScore();
            }

            startTime = Mathf.Max(5f, startTime - 0.1f);
            float bonus = 2f;
            currentTime = Mathf.Min(currentTime + bonus, startTime);

            SetupRound();
        }
        else
        {
            EndCombo();
            TriggerFail();
        }
    }

    private void UpdateComboText()
    {
        if (comboText == null) return;

        comboText.gameObject.SetActive(true);
        comboText.text = $"Combo x{comboCount}";
    }

    private void PlayComboPop()
    {
        if (comboTransform == null) return;

        if (comboAnimRoutine != null)
            StopCoroutine(comboAnimRoutine);
        comboAnimRoutine = StartCoroutine(ComboPopRoutine());
    }

    private IEnumerator ComboPopRoutine()
    {
        Vector3 baseScale = Vector3.one;
        Vector3 targetScale = baseScale * comboPopScale;
        float half = comboPopDuration * 0.5f;
        float t = 0f;

        while (t < half)
        {
            float k = t / half;
            comboTransform.localScale = Vector3.Lerp(baseScale, targetScale, k);
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            float k = t / half;
            comboTransform.localScale = Vector3.Lerp(targetScale, baseScale, k);
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        comboTransform.localScale = baseScale;
        comboAnimRoutine = null;
    }

    private IEnumerator ComboShakeLoop()
    {
        if (comboTransform == null) yield break;

        Vector3 baseScale = Vector3.one;

        while (isComboActive)
        {
            float t = Time.unscaledTime * comboShakeSpeed;
            float offsetX = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * comboShakeAmount;
            float offsetY = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * comboShakeAmount;

            comboTransform.localScale = baseScale + new Vector3(offsetX, offsetY, 0f);

            yield return null;
        }

        comboTransform.localScale = baseScale;
        comboShakeRoutine = null;
    }

    private void EndCombo(bool instant = false)
    {
        if (!isComboActive && comboCount < 3)
        {
            comboCount = Mathf.Min(comboCount, 2);
            if (comboText != null) comboText.gameObject.SetActive(false);
            return;
        }

        isComboActive = false;
        comboCount = 0;

        if (comboShakeRoutine != null)
        {
            StopCoroutine(comboShakeRoutine);
            comboShakeRoutine = null;
        }
        if (comboTransform != null)
            comboTransform.localScale = Vector3.one;

        if (comboText == null || comboTransform == null)
            return;

        if (instant)
        {
            comboText.gameObject.SetActive(false);
            return;
        }

        if (comboAnimRoutine != null)
            StopCoroutine(comboAnimRoutine);
        comboAnimRoutine = StartCoroutine(ComboEndRoutine());
    }

    private IEnumerator ComboEndRoutine()
    {
        Vector3 baseScale = Vector3.one;
        Vector3 smallScale = baseScale * 0.8f;
        float duration = comboPopDuration;
        float t = 0f;

        while (t < duration)
        {
            float k = t / duration;
            float eased = k * k * (3f - 2f * k);
            comboTransform.localScale = Vector3.Lerp(baseScale, smallScale, eased);
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        comboTransform.localScale = baseScale;
        if (comboText != null)
            comboText.gameObject.SetActive(false);

        comboAnimRoutine = null;
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

        int coinsEarned = ColorGamePuntos.Instance.GetCoinsEarned();

        CoinsRewardUI rewardUI = FindObjectOfType<CoinsRewardUI>(true);
        if (rewardUI != null)
            rewardUI.ShowReward(coinsEarned);
        else
            CurrencyManager.Instance.AddCoins(coinsEarned);

        if (DailyMissionManager.Instance != null)
        {
            DailyMissionManager.Instance.AddProgress("juega_1_partida", 1);
            DailyMissionManager.Instance.AddProgress("juega_3_partidas", 1);
            DailyMissionManager.Instance.AddProgress("juega_8_partidas", 1);
            DailyMissionManager.Instance.AddProgress("juega_10_partidas", 1);
            DailyMissionManager.Instance.AddProgress("juega_3_partidas_colores", 1);
        }
    }
}
