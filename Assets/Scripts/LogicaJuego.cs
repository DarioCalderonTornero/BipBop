using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LogicaJuego : MonoBehaviour
{
    public static LogicaJuego Instance { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI instructionText;   // Texto para mostrar la instrucción
    public Image instructionIcon;            // <-- NUEVO: icono de la instrucción
    public Image timerUI;                    // Imagen radial para mostrar el tiempo restante
    public float startTime;                  // Tiempo inicial en segundos

    private float currentTime;
    private bool isGameActive = false; // Se desactiva hasta que pasen los 3 segundos

    public event EventHandler OnGameOver;

    private bool isTaskCompleted = false;
    private TaskInfo currentTask;
    private TaskType lastTaskType;
    public TaskInfo[] tasks;

    private bool hasEnded = false;

    public enum TaskType
    {
        Tap,
        ZoomIn,
        ZoomOut,
        Shake,
        LookDown,
        SwipeRight,
        SwipeLeft,
        SwipeUp,
        SwipeDown,
        RotateRight,
        RotateLeft
    }

    [System.Serializable]
    public class TaskInfo
    {
        public TaskType type;             // Identificador lógico
        public LocalizedString text;      // Texto localizado
        public Sprite icon;               // <-- NUEVO: icono para esta tarea
    }

    [Header("Localization")]
    public LocalizedString readyText;
    public LocalizedString goText;
    public LocalizedString gameOverText;

    [Header("Countdown Icon Shuffle")]
    [SerializeField] private float iconShuffleInterval = 0.08f; 
    [SerializeField] private float iconShuffleDuration = 2f;

    private void Awake()
    {
        Instance = this;

        instructionText.text = readyText.GetLocalizedString();
        SetInstructionIcon(null); // Oculta icono en "Ready"

        timerUI.fillAmount = 0f;
    }

    void Start()
    {
        StartCoroutine(StartGameAfterDelay(3.1f));
    }

    private IEnumerator StartGameAfterDelay(float delay)
    {
        CountDownUI.Instance.Show();

        StartCoroutine(FillTimerDuringCountdown(2f));
        StartCoroutine(ShuffleTaskIcons(iconShuffleDuration));

        yield return new WaitForSeconds(delay);

        CountDownUI.Instance.ShowMessage(goText.GetLocalizedString());

        yield return new WaitForSeconds(0.8f);

        CountDownUI.Instance.Hide();
        isGameActive = true;

        instructionText.text = "";
        SetInstructionIcon(null); // Oculta icono al arrancar

        StartNewTask();
    }

    void Update()
    {
        if (!isGameActive || hasEnded) return;

        currentTime -= Time.deltaTime;

        timerUI.fillAmount = currentTime / startTime;

        if (currentTime <= 0f)
        {
            OnGameOver?.Invoke(this, EventArgs.Empty);
            EndGame();
        }
    }

    public void OnTaskAction(TaskType actionType)
    {
        if (!isGameActive || isTaskCompleted || hasEnded || currentTask == null) return;

        if (actionType == currentTask.type)
        {
            if (ClassicModeUIEffects.Instance != null)
            {
                ClassicModeUIEffects.Instance.PlayEffectForTask(actionType);
            }

            isTaskCompleted = true;
            StartNewTask();
            isTaskCompleted = false;
        }
    }

    private void StartNewTask()
    {
        if (hasEnded) return;

        MainGamePoints.Instance.AddScore();
        UpdateScoreText();

        List<TaskInfo> availableTasks = new List<TaskInfo>(tasks);

        // Filtrar por PlayerPrefs
        if (PlayerPrefs.GetInt("MotionTasks", 1) == 0)
        {
            availableTasks.RemoveAll(t =>
                t.type == TaskType.Shake ||
                t.type == TaskType.LookDown ||
                t.type == TaskType.RotateRight ||
                t.type == TaskType.RotateLeft
            );
        }

        if (availableTasks.Count == 0)
        {
            // Por seguridad, si el jugador desactiva todo, forzamos TAP
            currentTask = null;
            instructionText.text = "";
            SetInstructionIcon(null);
            return;
        }

        // Elegir nueva tarea distinta de la anterior
        TaskInfo newTask;
        do
        {
            newTask = availableTasks[UnityEngine.Random.Range(0, availableTasks.Count)];
        } while (currentTask != null && newTask.type == currentTask.type && availableTasks.Count > 1);

        currentTask = newTask;

        // Texto + Icono
        instructionText.text = currentTask.text.GetLocalizedString();
        SetInstructionIcon(currentTask.icon);

        // Reset tiempo
        currentTime = startTime;
        startTime = Mathf.Max(2f, startTime - 0.05f);

        // Si quieres que al reiniciar el tiempo se vea lleno:
        timerUI.fillAmount = 1f;
        // (tu versión ponía 0f; eso visualmente parece “vacío” al empezar)
    }

    private void SetInstructionIcon(Sprite sprite)
    {
        if (instructionIcon == null) return;

        instructionIcon.sprite = sprite;
        instructionIcon.enabled = (sprite != null);
    }

    private IEnumerator FillTimerDuringCountdown(float duration)
    {
        float t = 0f;
        timerUI.fillAmount = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            timerUI.fillAmount = Mathf.Clamp01(t / duration);
            yield return null;
        }

        timerUI.fillAmount = 1f; // asegúralo al final
    }

    private IEnumerator ShuffleTaskIcons(float duration)
    {
        if (instructionIcon == null || tasks == null || tasks.Length == 0) yield break;

        float t = 0f;

        // Lista de iconos disponibles (respetando MotionTasks si quieres)
        List<Sprite> icons = new List<Sprite>();
        bool motionEnabled = PlayerPrefs.GetInt("MotionTasks", 1) == 1;

        foreach (var task in tasks)
        {
            if (task == null || task.icon == null) continue;

            if (!motionEnabled)
            {
                if (task.type == TaskType.Shake ||
                    task.type == TaskType.LookDown ||
                    task.type == TaskType.RotateRight ||
                    task.type == TaskType.RotateLeft)
                    continue;
            }

            icons.Add(task.icon);
        }

        if (icons.Count == 0) yield break;

        instructionIcon.enabled = true;

        while (t < duration)
        {
            instructionIcon.sprite = icons[UnityEngine.Random.Range(0, icons.Count)];
            yield return new WaitForSeconds(iconShuffleInterval);
            t += iconShuffleInterval;
        }
    }

    private void UpdateScoreText()
    {
        MainGamePoints.Instance.ShowScore();
    }

    public bool IsGameActive()
    {
        return isGameActive;
    }

    private void SaveRecordIfNeeded()
    {
        int currentRecord = PlayerPrefs.GetInt("MaxRecord", 0);
        MainGamePoints.Instance.SafeRecordIfNeeded();
    }

    private void EndGame()
    {
        if (hasEnded) return;

        isGameActive = false;
        hasEnded = true;

        instructionText.text = gameOverText.GetLocalizedString();
        SetInstructionIcon(null); // Oculta icono en Game Over

        int coinsEarned = MainGamePoints.Instance.GetCoinsEarned();

        SaveRecordIfNeeded();

        CoinsRewardUI rewardUI = FindObjectOfType<CoinsRewardUI>(true);
        if (rewardUI != null)
        {
            rewardUI.ShowReward(coinsEarned);
        }
        else
        {
            CurrencyManager.Instance.AddCoins(coinsEarned);
        }

        if (PlayFabLoginManager.Instance != null && PlayFabLoginManager.Instance.IsLoggedIn)
        {
            PlayFabScoreManager.Instance.SubmitScore("HighScore", MainGamePoints.Instance.GetScore());
        }

        int totalCoins = PlayerPrefs.GetInt("CoinCount", 0);
        totalCoins += coinsEarned;
        PlayerPrefs.SetInt("CoinCount", totalCoins);
        PlayerPrefs.Save();

        if (DailyMissionManager.Instance != null)
        {
            DailyMissionManager.Instance.AddProgress("juega_1_partida", 1);
        }

        if (DailyMissionManager.Instance != null)
        {
            DailyMissionManager.Instance.AddProgress("juega_3_partida", 1);
        }

        if (DailyMissionManager.Instance != null)
        {
            DailyMissionManager.Instance.AddProgress("juega_8_partida", 1);
        }

        if (DailyMissionManager.Instance != null)
        {
            DailyMissionManager.Instance.AddProgress("juega_10_partida", 1);
        }

        if (DailyMissionManager.Instance != null && MainGamePoints.Instance.GetScore() >= 10)
        {
            DailyMissionManager.Instance.AddProgress("consigue_10_puntos_clásico", 1);
        }

        if (DailyMissionManager.Instance != null)
        {
            DailyMissionManager.Instance.AddProgress("juega_5_partidas_clásico", 1);
        }
    }
}
