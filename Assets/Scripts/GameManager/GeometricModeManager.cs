using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GeometricModeManager : MonoBehaviour
{
    public static GeometricModeManager Instance { get; private set; }

    [Header("UI Elements")]
    public TextMeshProUGUI instructionText;  // Indica qué figura tocar
    public TextMeshProUGUI scoreText;        // Puntuación actual
    public Image timeBarImage;               // Barra de tiempo
    public float startTime = 60f;            // Tiempo inicial (en segundos)

    [Header("Game Settings")]
    public float speedMultiplier = 1f;         // Multiplicador de velocidad actual
    public float speedIncreaseFactor = 1.05f;  // Factor de incremento de velocidad al acertar
    public float timeDecreaseFactor = 0.95f;   // (no lo usas ahora, pero lo dejo)

    [Header("Shapes")]
    public List<BouncingShape> shapes;         // Lista de figuras en escena

    [Header("Localization")]
    public LocalizedString scoreLabel;              // Smart String: "Puntos: {0}" / "Points: {0}"
    public LocalizedString tapShapeInstruction;     // Smart String: "¡Toca {0}!" / "Tap the {0}!"

    private float currentTime;
    private int score = 0;
    private BouncingShape currentTarget;
    private bool hasEnded = false;

    public event EventHandler OnGameOver;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void Start()
    {
        StartGame();
    }

    private void StartGame()
    {
        hasEnded = false;
        score = 0;
        currentTime = startTime;

        if (timeBarImage != null)
            timeBarImage.fillAmount = 1f;

        UpdateScoreText();

        // Activa solo las primeras 3 figuras al iniciar
        for (int i = 0; i < shapes.Count; i++)
        {
            shapes[i].gameObject.SetActive(i < 3);
        }

        ChooseNewTarget();
    }

    private void Update()
    {
        if (hasEnded) return;

        currentTime -= Time.deltaTime;

        if (timeBarImage != null)
            timeBarImage.fillAmount = Mathf.Clamp01(currentTime / startTime);

        if (currentTime <= 0f)
        {
            StartCoroutine(SlowMotionAndEnd(false, null));
        }
    }

    // Se llama cuando se toca una figura
    public void OnShapeTapped(BouncingShape shape)
    {
        if (hasEnded) return;

        if (shape == currentTarget)
        {
            shape.TemporarilyChangeColor(Color.green, 0.5f);
            AddScore();

            startTime = Mathf.Max(1.5f, startTime - 0.1f);
            currentTime = startTime;

            speedMultiplier = Mathf.Min(4f, speedMultiplier * speedIncreaseFactor);
            UpdateShapesSpeed();

            CheckForAdditionalShapes();
            ChooseNewTarget();
        }
        else
        {
            StartCoroutine(SlowMotionAndEnd(true, shape));
        }
    }

    private void AddScore()
    {
        score++;
        UpdateScoreText();
        Haptics.TryVibrate();
        PlayerLevelManager.Instance.AddXP(50);
        PlayScoreEffect();

        if (FallingShapesManager.Instance != null)
            FallingShapesManager.Instance.SpawnFallingShapes();
    }

    [SerializeField] private RectTransform scoreEffectGroup;
    private bool isAnimating = false;

    private void PlayScoreEffect()
    {
        if (!isAnimating)
            StartCoroutine(AnimateScoreEffect());
    }

    private IEnumerator AnimateScoreEffect()
    {
        isAnimating = true;

        float duration = 0.05f;
        float halfDuration = duration / 2f;
        Vector3 originalScale = Vector3.one;
        Vector3 zoomScale = new Vector3(1.2f, 1.2f, 1);

        float time = 0;
        while (time < halfDuration)
        {
            scoreEffectGroup.localScale = Vector3.Lerp(originalScale, zoomScale, time / halfDuration);
            time += Time.deltaTime;
            yield return null;
        }

        time = 0;
        while (time < halfDuration)
        {
            scoreEffectGroup.localScale = Vector3.Lerp(zoomScale, originalScale, time / halfDuration);
            time += Time.deltaTime;
            yield return null;
        }

        scoreEffectGroup.localScale = originalScale;
        isAnimating = false;
    }

    private void UpdateScoreText()
    {
        // Localizado: "Puntos: {0}" / "Points: {0}"
        scoreText.text = scoreLabel.GetLocalizedString(score);
    }

    public int GetScore()
    {
        return score;
    }

    // Escoge una nueva figura objetivo entre las activas
    private void ChooseNewTarget()
    {
        List<BouncingShape> activeShapes = shapes.FindAll(s => s.gameObject.activeSelf);

        if (activeShapes.Count > 1 && currentTarget != null)
        {
            BouncingShape newTarget;
            do
            {
                int index = UnityEngine.Random.Range(0, activeShapes.Count);
                newTarget = activeShapes[index];
            } while (newTarget == currentTarget);
            currentTarget = newTarget;
        }
        else if (activeShapes.Count > 0)
        {
            currentTarget = activeShapes[0];
        }

        if (currentTarget != null)
        {
            // Primero localizamos el nombre de la figura
            string shapeNameText = currentTarget.shapeName.GetLocalizedString();

            // Luego lo metemos en la Smart String "Tap the {0}!"
            instructionText.text = tapShapeInstruction.GetLocalizedString(shapeNameText);
        }
    }

    private void UpdateShapesSpeed()
    {
        foreach (BouncingShape s in shapes)
        {
            s.UpdateSpeed(speedMultiplier);
        }
    }

    private void CheckForAdditionalShapes()
    {
        if (score >= 25 && shapes.Count > 3 && !shapes[3].gameObject.activeSelf)
        {
            shapes[3].gameObject.SetActive(true);
            StartCoroutine(ApplySpeedNextFrame(shapes[3]));
        }
        if (score >= 50 && shapes.Count > 4 && !shapes[4].gameObject.activeSelf)
        {
            shapes[4].gameObject.SetActive(true);
            StartCoroutine(ApplySpeedNextFrame(shapes[4]));
        }
    }

    private IEnumerator ApplySpeedNextFrame(BouncingShape shape)
    {
        yield return null;
        shape.UpdateSpeed(speedMultiplier);
    }

    private void SaveRecordIfNeeded()
    {
        int currentRecord = PlayerPrefs.GetInt("MaxRecordGeometric", 0);

        if (score > currentRecord)
        {
            PlayerPrefs.SetInt("MaxRecordGeometric", score);
            PlayerPrefs.Save();
        }
    }

    private IEnumerator SlowMotionAndEnd(bool wrongShape, BouncingShape touchedShape = null)
    {
        if (hasEnded) yield break;
        hasEnded = true;

        float previousTimeScale = Time.timeScale;
        float previousFixedDelta = Time.fixedDeltaTime;

        Time.timeScale = 0.3f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        if (wrongShape && touchedShape != null)
        {
            SpriteRenderer mainRenderer = touchedShape.GetComponentInChildren<SpriteRenderer>();

            if (mainRenderer != null)
            {
                Color originalColor = mainRenderer.color;
                Color redColor = Color.red;

                int flashes = 4;
                float flashInterval = 0.4f;

                for (int i = 0; i < flashes; i++)
                {
                    mainRenderer.color = redColor;
                    yield return new WaitForSecondsRealtime(flashInterval);
                    mainRenderer.color = originalColor;
                    yield return new WaitForSecondsRealtime(flashInterval);
                }
            }
        }

        yield return new WaitForSecondsRealtime(1.5f);

        Time.timeScale = previousTimeScale;
        Time.fixedDeltaTime = previousFixedDelta;

        EndGame();
    }

    private void EndGame()
    {
        if (hasEnded) return;
        hasEnded = true;

        OnGameOver?.Invoke(this, EventArgs.Empty);
        SaveRecordIfNeeded();

        if (PlayFabLoginManager.Instance != null && PlayFabLoginManager.Instance.IsLoggedIn)
            PlayFabScoreManager.Instance.SubmitScore("GeometricScore", score);

        int coinsEarned = score;

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

    private void OnLocaleChanged(Locale locale)
    {
        UpdateScoreText();

        if (currentTarget != null)
        {
            string shapeNameText = currentTarget.shapeName.GetLocalizedString();
            instructionText.text = tapShapeInstruction.GetLocalizedString(shapeNameText);
        }
    }
}
