using System;
using System.Collections;
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

    [Header("Sprites de colores")]
    [SerializeField] private Sprite[] colorSprites;

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

    public enum RoundMode
    {
        BackgroundColor,   // elegir color del fondo
        WordMeaning,       // elegir el color que indica la palabra
        TextColor          // elegir el color con el que está escrita
    }

    [Header("Modo desbloqueo ruleta")]
    [SerializeField] private int modeWheelUnlockScore = 20;

    [Header("Modo de ronda")]
    [SerializeField] private Image[] modeIcons;   // 0=fondo, 1=palabra, 2=texto
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

    [SerializeField] private float comboShakeAmount = 0.05f;   // 5% de escala
    [SerializeField] private float comboShakeSpeed = 15f;

    private Coroutine comboShakeRoutine;

    private void Awake()
    {
        Instance = this;
        deathType = DeathType.None;
        currentMode = RoundMode.WordMeaning;
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

        if (deathType != DeathType.None)
            return;

        currentTime -= Time.deltaTime;
        timeSinceLastHit += Time.deltaTime;

        if (timeBarImage != null)
            timeBarImage.fillAmount = Mathf.Clamp01(currentTime / startTime);

        // Si estás en combo y te pasas del tiempo, lo rompes
        if (isComboActive && timeSinceLastHit > maxTimeBetweenHits)
        {
            EndCombo();
        }

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
        timeSinceLastHit = 0f;
        comboCount = 0;
        isComboActive = false;
        if (comboText != null) comboText.gameObject.SetActive(false);

        if (timeBarImage != null)
            timeBarImage.fillAmount = 1f;

        UpdateScoreText();
        SetupRound();
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

        // --- (MINI safety) no dejes que availableCount supere tus arrays ---
        if (colorValues != null) availableCount = Mathf.Min(availableCount, colorValues.Length);
        if (colorNames != null) availableCount = Mathf.Min(availableCount, colorNames.Length);
        // backgroundSprites puede ser más corto o igual, lo controlamos abajo

        // 1) Elegir colores base
        int backgroundColorIndex, textColorIndex, wordColorIndex;

        wordColorIndex = UnityEngine.Random.Range(0, availableCount);
        string correctColorName = colorNames[wordColorIndex].GetLocalizedString();

        do
        {
            textColorIndex = UnityEngine.Random.Range(0, availableCount);
        } while (textColorIndex == wordColorIndex);

        // ✅ CAMBIO MINIMO CLAVE: el fondo también sale del mismo pool (availableCount)
        do
        {
            backgroundColorIndex = UnityEngine.Random.Range(0, availableCount);
        } while (backgroundColorIndex == textColorIndex || backgroundColorIndex == wordColorIndex);

        colorWordText.text = correctColorName;
        colorWordText.color = colorValues[textColorIndex];

        // Fondo: se queda EXACTAMENTE como lo tenías (backgroundSprites independiente)
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

        // 2) Elegir modo en orden según puntuación
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

        // 3) Índice correcto según modo
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

        // 4) Construir candidatos
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

        // 5) Shuffle
        for (int i = 0; i < candidateIndices.Count; i++)
        {
            int r = UnityEngine.Random.Range(i, candidateIndices.Count);
            (candidateIndices[i], candidateIndices[r]) = (candidateIndices[r], candidateIndices[i]);
        }

        // 6) Pintar botones y listeners
        for (int i = 0; i < candidateButtons.Count; i++)
        {
            Button btn = candidateButtons[i];

            // (mini safety) si hay más botones que candidatos posibles, no rompas el juego
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

            // arrancamos una pequeña animación hacia el nuevo estado
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
            // easing suave
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
        if (hasEnded || !hasStarted || deathType != DeathType.None)
            return;

        if (selectedIndex == correctIndex)
        {
            // Shake de todas las opciones
            foreach (var btn in candidateButtons)
            {
                var fx = btn.GetComponent<ColorOptionButtonFX>();
                if (fx != null) fx.PlayShake();
            }

            // ------ COMBO ------
            if (timeSinceLastHit <= maxTimeBetweenHits)
            {
                comboCount++;
            }
            else
            {
                comboCount = 1;
            }

            timeSinceLastHit = 0f;

            // Activar combo a partir de 3
            if (comboCount >= 3)
            {
                if (!isComboActive)
                {
                    isComboActive = true;
                    UpdateComboText();
                    PlayComboPop();

                    // empezar shake continuo
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

            // Puntuación (x2 si combo activo)
            if (isComboActive)
            {
                // 1 punto con sonido/monedas/XP
                ColorGamePuntos.Instance.AddScore();
                // +1 punto silencioso → total x2
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
            DecideDeathType();
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

        // pop inverso: escala un poco hacia abajo y desaparece
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

        int coinsEarned = ColorGamePuntos.Instance.score;

        CoinsRewardUI rewardUI = FindObjectOfType<CoinsRewardUI>(true);
        if (rewardUI != null)
            rewardUI.ShowReward(coinsEarned);
        else
            CurrencyManager.Instance.AddCoins(coinsEarned);
    }
}
