using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GeometricModeManager : MonoBehaviour
{
    public static GeometricModeManager Instance { get; private set; }

    [Header("UI Elements")]
    public TextMeshProUGUI instructionText;  // Indica qué figura tocar
    public TextMeshProUGUI scoreText;        // Puntuación actual
    public Slider timeSlider;                // Barra de tiempo
    public float startTime = 60f;            // Tiempo inicial (en segundos)

    [Header("Game Settings")]
    public float speedMultiplier = 1f;         // Multiplicador de velocidad actual
    public float speedIncreaseFactor = 1.1f;   // Factor de incremento de velocidad al acertar
    public float timeDecreaseFactor = 0.95f;   // Factor de reducción del tiempo base al acertar

    [Header("Shapes")]
    public List<BouncingShape> shapes;         // Lista de todas las figuras geométricas (prefabs o instancias en escena)

    private float currentTime;
    private int score = 0;
    private BouncingShape currentTarget;
    private bool hasEnded = false; //  Nueva bandera de protección

    public event EventHandler OnGameOver;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartGame();
    }

    private void StartGame()
    {
        hasEnded = false; //  Reinicia la bandera al comenzar
        score = 0;
        currentTime = startTime;
        timeSlider.maxValue = startTime;
        timeSlider.value = startTime;
        UpdateScoreText();

        // Activa solo las primeras 3 figuras al iniciar (índices 0, 1 y 2)
        for (int i = 0; i < shapes.Count; i++)
        {
            shapes[i].gameObject.SetActive(i < 3);
        }

        ChooseNewTarget();
    }

    private void Update()
    {
        if (hasEnded) return; //  No seguir si el juego ya terminó

        currentTime -= Time.deltaTime;
        timeSlider.maxValue = startTime;
        timeSlider.value = currentTime;

        if (currentTime <= 0f)
        {
            EndGame();
        }
    }

    // Este método se llama cuando se toca una figura
    public void OnShapeTapped(BouncingShape shape)
    {
        if (hasEnded) return; //  Bloquea interacción si el juego terminó

        if (shape == currentTarget)
        {
            // Si se toca la figura objetivo, la ponemos verde y registramos el acierto
            shape.TemporarilyChangeColor(Color.green, 0.5f);
            AddScore();

            // Ajustar el tiempo: disminuir en 0.1 segundos, pero no por debajo de 1.5 segundos
            startTime = Mathf.Max(1.5f, startTime - 0.1f);
            currentTime = startTime;

            // Ajustar la velocidad: multiplicar por speedIncreaseFactor pero sin superar 4f
            speedMultiplier = Mathf.Min(4f, speedMultiplier * speedIncreaseFactor);
            UpdateShapesSpeed();

            // Activar nuevas figuras según la puntuación
            CheckForAdditionalShapes();

            ChooseNewTarget();
        }
        else
        {
            // Si se toca una figura que no es objetivo, se pone roja durante 0.5 segundos
            shape.TemporarilyChangeColor(Color.red, 0.5f);
        }
    }

    private void AddScore()
    {
        score++;
        UpdateScoreText();
        Haptics.TryVibrate();
        PlayerLevelManager.Instance.AddXP(50);

        // NUEVO: genera los objetos que caen
        if (FallingShapesManager.Instance != null)
            FallingShapesManager.Instance.SpawnFallingShapes();
    }

    private void UpdateScoreText()
    {
        scoreText.text = "Puntos: " + score;
    }

    public int GetScore()
    {
        return score;
    }

    // Escoge una nueva figura objetivo entre las activas, evitando repetir la misma
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

        instructionText.text = "¡Toca " + currentTarget.shapeName + "!";
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
        yield return null; // espera un frame
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

    private void EndGame()
    {
        if (hasEnded) return; // Previene llamadas múltiples
        hasEnded = true;

        OnGameOver?.Invoke(this, EventArgs.Empty);
        SaveRecordIfNeeded();

        if (PlayFabLoginManager.Instance != null && PlayFabLoginManager.Instance.IsLoggedIn)
            PlayFabScoreManager.Instance.SubmitScore("GeometricScore", score);

        // Calculamos las monedas ganadas (1 por punto)
        int coinsEarned = score;

        // Mostramos la animación de recompensa si existe el panel en la escena
        CoinsRewardUI rewardUI = FindObjectOfType<CoinsRewardUI>(true);
        if (rewardUI != null)
        {
            rewardUI.ShowReward(coinsEarned);
        }
        else
        {
            // Fallback: suma directa sin animación
            CurrencyManager.Instance.AddCoins(coinsEarned);
        }

        Debug.Log($"Fin de partida — Recompensa: {coinsEarned} monedas");
    }
}
