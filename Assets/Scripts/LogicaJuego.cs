// LogicaJuego.cs
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;

public class LogicaJuego : MonoBehaviour
{
    public static LogicaJuego Instance { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI instructionText;
    public Image instructionIcon;
    public Image timerUI;
    public float startTime;

    private float currentTime;
    private bool isGameActive = false;

    public event EventHandler OnGameOver;

    private bool isTaskCompleted = false;
    private TaskInfo currentTask;
    private TaskType lastTaskType;
    public TaskInfo[] tasks;

    [SerializeField] private AudioClip successAudioClip;
    [SerializeField] private AudioClip failAudioClip;

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
        public TaskType type;
        public LocalizedString text;
        public Sprite icon;
    }

    [Header("Localization")]
    public LocalizedString readyText;
    public LocalizedString goText;
    public LocalizedString gameOverText;

    [Header("Countdown Icon Shuffle")]
    [SerializeField] private float iconShuffleInterval = 0.08f;
    [SerializeField] private float iconShuffleDuration = 2f;

    // -------------------------
    // Tutorial (global toggle)
    // -------------------------
    [Header("Tutorial Panel")]
    [SerializeField] private TutorialPanelUI tutorialPrefab;
    [SerializeField] private Transform tutorialParent;
    private TutorialPanelUI tutorialInstance;

    private const string ShowTutorialKey = "ShowTutorialOnStart";

    // -------------------------
    // Countdown coroutines control
    // -------------------------
    private Coroutine countdownRoutine;
    private Coroutine fillRoutine;
    private Coroutine shuffleRoutine;

    private void Awake()
    {
        Instance = this;

        instructionText.text = readyText.GetLocalizedString();
        SetInstructionIcon(null);

        timerUI.fillAmount = 0f;
    }

    private void Start()
    {
        hasEnded = false;
        isGameActive = false;

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

    private void OnDisable()
    {
        // Por si cambias de escena o desactivas el GO a mitad countdown
        StopCountdownRoutines();
    }

    // ================
    // Tutorial flow
    // ================
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
        // Arranque limpio del countdown + preview
        StartCountdownSequence();
    }

    // ================
    // Countdown flow
    // ================
    private void StartCountdownSequence()
    {
        StopCountdownRoutines();

        // Estado inicial visual
        instructionText.text = readyText.GetLocalizedString();
        SetInstructionIcon(null);
        timerUI.fillAmount = 0f;

        if (CountDownUI.Instance != null)
            CountDownUI.Instance.Show();

        // FX durante countdown
        fillRoutine = StartCoroutine(FillTimerDuringCountdown(2f));
        shuffleRoutine = StartCoroutine(ShuffleTaskIcons(iconShuffleDuration));

        // Usa GameStates si existe; si no, fallback local
        if (GameStates.Instance != null)
        {
            GameStates.Instance.StartCountdown(); // método nuevo en GameStates
        }
        else
        {
            countdownRoutine = StartCoroutine(LocalCountdownRoutine(3.1f));
        }
    }

    private IEnumerator LocalCountdownRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (CountDownUI.Instance != null)
            CountDownUI.Instance.ShowMessage(goText.GetLocalizedString());

        yield return new WaitForSeconds(0.8f);

        OnCountdownFinishedStartPlaying();
        countdownRoutine = null;
    }

    public void OnCountdownFinishedStartPlaying()
    {
        if (hasEnded) return;

        // Parar preview/FX por seguridad
        StopCountdownRoutines();

        if (CountDownUI.Instance != null)
            CountDownUI.Instance.Hide();

        instructionText.text = "";
        SetInstructionIcon(null);

        isGameActive = true;
        StartNewTask();
    }

    private void StopCountdownRoutines()
    {
        if (countdownRoutine != null) { StopCoroutine(countdownRoutine); countdownRoutine = null; }
        if (fillRoutine != null) { StopCoroutine(fillRoutine); fillRoutine = null; }
        if (shuffleRoutine != null) { StopCoroutine(shuffleRoutine); shuffleRoutine = null; }
    }

    public void PauseGameplay()
    {
        isGameActive = false;
    }

    // ================
    // Game loop
    // ================
    private void Update()
    {
        if (!isGameActive || hasEnded) return;

        currentTime -= Time.deltaTime;
        timerUI.fillAmount = currentTime / startTime;

        if (currentTime <= 0f)
        {
            SoundManager.Instance.PlaySound(failAudioClip, 1f);
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
                ClassicModeUIEffects.Instance.PlayEffectForTask(actionType);

            SoundManager.Instance.PlaySound(successAudioClip, 1f);

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
            currentTask = null;
            instructionText.text = "";
            SetInstructionIcon(null);
            return;
        }

        TaskInfo newTask;
        do
        {
            newTask = availableTasks[UnityEngine.Random.Range(0, availableTasks.Count)];
        } while (currentTask != null && newTask.type == currentTask.type && availableTasks.Count > 1);

        currentTask = newTask;

        instructionText.text = currentTask.text.GetLocalizedString();
        SetInstructionIcon(currentTask.icon);

        currentTime = startTime;
        startTime = Mathf.Max(2f, startTime - 0.05f);

        timerUI.fillAmount = 1f;
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

        timerUI.fillAmount = 1f;
    }

    private IEnumerator ShuffleTaskIcons(float duration)
    {
        if (instructionIcon == null || tasks == null || tasks.Length == 0) yield break;

        float t = 0f;

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
        SetInstructionIcon(null);

        int coinsEarned = MainGamePoints.Instance.GetCoinsEarned();

        SaveRecordIfNeeded();

        CoinsRewardUI rewardUI = FindObjectOfType<CoinsRewardUI>(true);
        if (rewardUI != null) rewardUI.ShowReward(coinsEarned);
        else CurrencyManager.Instance.AddCoins(coinsEarned);

        if (PlayFabLoginManager.Instance != null && PlayFabLoginManager.Instance.IsLoggedIn)
            PlayFabScoreManager.Instance.SubmitScore("HighScore", MainGamePoints.Instance.GetScore());

        int totalCoins = PlayerPrefs.GetInt("CoinCount", 0);
        totalCoins += coinsEarned;
        PlayerPrefs.SetInt("CoinCount", totalCoins);
        PlayerPrefs.Save();

        if (DailyMissionManager.Instance != null)
        {
            DailyMissionManager.Instance.AddProgress("juega_1_partida", 1);
            DailyMissionManager.Instance.AddProgress("juega_3_partidas", 1);
            DailyMissionManager.Instance.AddProgress("juega_8_partidas", 1);
            DailyMissionManager.Instance.AddProgress("juega_10_partidas", 1);

            if (MainGamePoints.Instance.GetScore() >= 10)
                DailyMissionManager.Instance.AddProgress("consigue_10_puntos_clásico", 1);

            DailyMissionManager.Instance.AddProgress("juega_5_partidas_clásico", 1);
        }
    }
}
