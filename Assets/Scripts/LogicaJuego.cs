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

    public TextMeshProUGUI instructionText; // Texto para mostrar la instrucción
    public Image timerUI; // Slider para mostrar el tiempo restante
    public float startTime; // Tiempo inicial en segundos

    private float currentTime;
    private bool isGameActive = false; // Se desactiva hasta que pasen los 3 segundos

    public event EventHandler OnGameOver;

    private bool isTaskCompleted = false; // Verifica si la tarea actual ya fue completada
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
        public LocalizedString text;      // Texto localizado (ES/EN), ej: "¡Toca la pantalla!"
    }

    [Header("Localization")]
    public LocalizedString readyText;        // "Prepárate..." / "Get ready..."
    public LocalizedString goText;           // "GO!"
    public LocalizedString gameOverText;

    private void Awake()
    {
        Instance = this;
        instructionText.text = readyText.GetLocalizedString();
        timerUI.fillAmount = 1;
    }

    void Start()
    {
        StartCoroutine(StartGameAfterDelay(3.1f));
    }

    private IEnumerator StartGameAfterDelay(float delay)
    {
        CountDownUI.Instance.Show();

        yield return new WaitForSeconds(delay);

        CountDownUI.Instance.ShowMessage(goText.GetLocalizedString());

        yield return new WaitForSeconds(0.8f);

        CountDownUI.Instance.Hide();
        isGameActive = true;
        instructionText.text = "";

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

        // Copia lista de tareas disponibles
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

        // Elegir nueva tarea distinta de la anterior
        TaskInfo newTask;
        do
        {
            newTask = availableTasks[UnityEngine.Random.Range(0, availableTasks.Count)];
        } while (currentTask != null && newTask.type == currentTask.type);

        currentTask = newTask;

        // Texto localizado según idioma actual
        instructionText.text = currentTask.text.GetLocalizedString();

        currentTime = startTime;
        startTime = Mathf.Max(2f, startTime - 0.05f);
        timerUI.fillAmount = 0f;
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
        // Recupera el récord actual
        int currentRecord = PlayerPrefs.GetInt("MaxRecord", 0);

        MainGamePoints.Instance.SafeRecordIfNeeded();
    }

    private void EndGame()
    {
        if (hasEnded) return;

        isGameActive = false;
        hasEnded = true;
        instructionText.text = gameOverText.GetLocalizedString();

        int coinsEarned = MainGamePoints.Instance.GetScore() / 15;

        CoinsRewardUI rewardUI = FindObjectOfType<CoinsRewardUI>(true);
        if (rewardUI != null)
        {
            rewardUI.ShowReward(coinsEarned);
        }
        else
        {
            CurrencyManager.Instance.AddCoins(coinsEarned);
        }

        //PlayFabScoreManager.Instance.SubmitScore("HighScore", MainGamePoints.Instance.GetScore());
        int totalCoins = PlayerPrefs.GetInt("CoinCount", 0);
        totalCoins += coinsEarned;
        PlayerPrefs.SetInt("CoinCount", totalCoins);
        PlayerPrefs.Save();
    }
}
