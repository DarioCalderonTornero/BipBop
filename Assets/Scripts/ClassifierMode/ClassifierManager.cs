using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

[RequireComponent(typeof(ClassifierInput))]
[RequireComponent(typeof(ClassifierTimer))]
[RequireComponent(typeof(ClassifierScore))]
public class ClassifierManager : MonoBehaviour, IGameOverClient
{
    public static ClassifierManager Instance { get; private set; }

    public event EventHandler OnClassifierGameOver;

    [Header("Base de Datos (DEBEN SER 4)")]
    public List<CategoryData> categories = new List<CategoryData>();

    [Header("Visuales y Animación")]
    public SpriteRenderer centerItemRenderer;
    public float targetVisualSize = 2f;
    public float swipeSpeed = 25f;

    [Header("UI de Categorías (Iconos)")]
    [SerializeField] private Image topCategoryIcon;
    [SerializeField] private Image bottomCategoryIcon;
    [SerializeField] private Image leftCategoryIcon;
    [SerializeField] private Image rightCategoryIcon;

    [Header("UI de Categorías (Fondos)")]
    [SerializeField] private Image topCategoryBg;
    [SerializeField] private Image bottomCategoryBg;
    [SerializeField] private Image leftCategoryBg;
    [SerializeField] private Image rightCategoryBg;

    [Header("Ajustes de Vuelo")]
    [SerializeField] private float distX = 2.8f;
    [SerializeField] private float distY = 5.0f;

    [Header("Sounds")]
    [SerializeField] private AudioClip[] correctAudioclips;
    [SerializeField] private AudioClip[] errorAudioclip;
    [SerializeField] private AudioClip swipeAudioclip;
    [SerializeField] private AudioClip spawnPopAudioclip;

    private ClassifierInput inputModule;
    private ClassifierTimer timerModule;
    [SerializeField] private ClassifierScore scoreModule;

    private SwipeDirection currentCorrectDirection;
    private bool isGameOver = false;
    private bool hasEnded = false;

    // =========================
    // ADS / GameOverFlow
    // =========================
    public bool HasUsedReviveOffer { get; set; } = false;
    private bool isPausedByOffer = false;
    private bool gameOverInvoked = false;

    // --- MUTADORES ---
    private enum MutatorType { MemoryFlash, Chaos, Inverse }
    private Dictionary<MutatorType, int> activeMutators = new Dictionary<MutatorType, int>();

    [SerializeField] private int memoryFlashRoundBegin = 2;
    [SerializeField] private int chaosRoundBegin = 8;
    [SerializeField] private int inverseRoundBegin = 14;

    private int presentationRoundsLeft = 0;

    private Dictionary<CategoryData, SwipeDirection> originalDirections = new Dictionary<CategoryData, SwipeDirection>();

    private class CategoryUIPack
    {
        public Image icon;
        public Image bg;
    }
    private Dictionary<CategoryData, CategoryUIPack> catUIPacks = new Dictionary<CategoryData, CategoryUIPack>();

    private Dictionary<SwipeDirection, Vector3> slotIconPos = new Dictionary<SwipeDirection, Vector3>();
    private Dictionary<SwipeDirection, Vector3> slotBgPos = new Dictionary<SwipeDirection, Vector3>();

    // ==================
    // Tutorial
    // ==================
    [Header("Tutorial Panel")]
    [SerializeField] private TutorialPanelUI tutorialPrefab;
    [SerializeField] private Transform tutorialParent;
    private TutorialPanelUI tutorialInstance;
    private const string ShowTutorialKey = "ShowTutorialOnStart";

    private Coroutine bindStateRoutine;
    private bool boundToState = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        inputModule = GetComponent<ClassifierInput>();
        timerModule = GetComponent<ClassifierTimer>();
        if (scoreModule == null) scoreModule = GetComponent<ClassifierScore>();

        if (topCategoryBg == null || bottomCategoryBg == null || leftCategoryBg == null || rightCategoryBg == null)
            Debug.LogError("🚨 ¡FALTAN FONDOS POR ASIGNAR EN EL INSPECTOR! 🚨");
    }

    private void OnEnable()
    {
        inputModule.OnSwipeDetected += HandlePlayerSwipe;
        timerModule.OnTimeOut += TimerModule_OnTimeOut;

        if (bindStateRoutine != null) StopCoroutine(bindStateRoutine);
        bindStateRoutine = StartCoroutine(BindToClassifierStateWhenReady());
    }

    private void OnDisable()
    {
        inputModule.OnSwipeDetected -= HandlePlayerSwipe;
        timerModule.OnTimeOut -= TimerModule_OnTimeOut;

        UnbindFromClassifierState();
    }

    private IEnumerator BindToClassifierStateWhenReady()
    {
        while (ClassifierState.Instance == null)
            yield return null;

        UnbindFromClassifierState();

        ClassifierState.Instance.OnPlayingClassifierGame += ClassifierState_OnPlayingClassifierGame;
        boundToState = true;
        bindStateRoutine = null;
    }

    private void UnbindFromClassifierState()
    {
        if (boundToState && ClassifierState.Instance != null)
        {
            ClassifierState.Instance.OnPlayingClassifierGame -= ClassifierState_OnPlayingClassifierGame;
        }
        boundToState = false;
    }

    private void Start()
    {
        isGameOver = false;
        hasEnded = false;
        gameOverInvoked = false;
        HasUsedReviveOffer = false;
        isPausedByOffer = false;

        activeMutators.Clear();
        presentationRoundsLeft = 0;

        if (categories == null || categories.Count != 4)
            Debug.LogError("ClassifierManager: 'categories' debe tener EXACTAMENTE 4 elementos.");

        if (topCategoryIcon != null) { slotIconPos[SwipeDirection.Up] = topCategoryIcon.transform.position; slotBgPos[SwipeDirection.Up] = topCategoryBg.transform.position; }
        if (bottomCategoryIcon != null) { slotIconPos[SwipeDirection.Down] = bottomCategoryIcon.transform.position; slotBgPos[SwipeDirection.Down] = bottomCategoryBg.transform.position; }
        if (leftCategoryIcon != null) { slotIconPos[SwipeDirection.Left] = leftCategoryIcon.transform.position; slotBgPos[SwipeDirection.Left] = leftCategoryBg.transform.position; }
        if (rightCategoryIcon != null) { slotIconPos[SwipeDirection.Right] = rightCategoryIcon.transform.position; slotBgPos[SwipeDirection.Right] = rightCategoryBg.transform.position; }

        originalDirections.Clear();
        catUIPacks.Clear();

        foreach (var cat in categories)
        {
            if (cat == null) continue;
            originalDirections[cat] = cat.correctDirection;

            CategoryUIPack pack = new CategoryUIPack();
            switch (cat.correctDirection)
            {
                case SwipeDirection.Up: pack.icon = topCategoryIcon; pack.bg = topCategoryBg; break;
                case SwipeDirection.Down: pack.icon = bottomCategoryIcon; pack.bg = bottomCategoryBg; break;
                case SwipeDirection.Left: pack.icon = leftCategoryIcon; pack.bg = leftCategoryBg; break;
                case SwipeDirection.Right: pack.icon = rightCategoryIcon; pack.bg = rightCategoryBg; break;
            }
            catUIPacks[cat] = pack;
        }

        SetupCategoryUI();
        StartUIBreathing();

        SpawnNewItem(false);

        PauseGameplay();

        bool showTutorialOnStart = PlayerPrefs.GetInt(ShowTutorialKey, 1) == 1;

        if (showTutorialOnStart)
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
        if (tutorialInstance != null)
        {
            tutorialInstance.gameObject.SetActive(true);
        }
        else
        {
            var existing = FindObjectOfType<TutorialPanelUI>(true);
            if (existing != null)
            {
                tutorialInstance = existing;
                tutorialInstance.gameObject.SetActive(true);
            }
            else
            {
                if (tutorialPrefab != null)
                {
                    Transform parent = tutorialParent;
                    if (parent == null)
                    {
                        Canvas c = FindObjectOfType<Canvas>();
                        parent = (c != null) ? c.transform : transform;
                    }
                    tutorialInstance = Instantiate(tutorialPrefab, parent);
                }
                else
                {
                    Debug.LogWarning("ClassifierManager: Tutorial marcado pero 'tutorialPrefab' es null y no hay TutorialPanelUI en escena. Se continúa sin tutorial.");
                    BeginGameAfterTutorial();
                    return;
                }
            }
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
        StartCoroutine(BeginCountdownWhenStateReady());
    }

    private IEnumerator BeginCountdownWhenStateReady()
    {
        while (ClassifierState.Instance == null)
            yield return null;

        isGameOver = false;
        hasEnded = false;

        PauseGameplay();

        ClassifierState.Instance.StartCountdown();
    }

    private void PauseGameplay()
    {
        inputModule.isInputActive = false;
        timerModule.StopTimer();
    }

    private void ClassifierState_OnPlayingClassifierGame(object sender, EventArgs e)
    {
        if (isGameOver) return;

        timerModule.ResetAndStartTimer();
        inputModule.isInputActive = true;
    }

    private void TimerModule_OnTimeOut(object sender, EventArgs e)
    {
        if (isPausedByOffer) return;
        HandleGameOver();
    }

    private void SetupCategoryUI()
    {
        foreach (CategoryData category in categories)
        {
            if (category == null || !catUIPacks.ContainsKey(category)) continue;

            CategoryUIPack pack = catUIPacks[category];

            if (pack.icon != null && category.categoryIcon != null)
                pack.icon.sprite = category.categoryIcon;

            SwipeDirection targetSlot = category.correctDirection;

            // Vuelve a ser instantáneo para evitar bugs visuales
            if (pack.icon != null && slotIconPos.ContainsKey(targetSlot))
                pack.icon.transform.position = slotIconPos[targetSlot];

            if (pack.bg != null && slotBgPos.ContainsKey(targetSlot))
                pack.bg.transform.position = slotBgPos[targetSlot];
        }
    }

    private void StartUIBreathing()
    {
        if (topCategoryIcon != null) AnimateBreathing(topCategoryIcon);
        if (bottomCategoryIcon != null) AnimateBreathing(bottomCategoryIcon);
        if (leftCategoryIcon != null) AnimateBreathing(leftCategoryIcon);
        if (rightCategoryIcon != null) AnimateBreathing(rightCategoryIcon);
    }

    private void AnimateBreathing(Image icon)
    {
        if (icon == null) return;
        LeanTween.scale(icon.gameObject, Vector3.one * 1.08f, 1.2f)
            .setEase(LeanTweenType.easeInOutSine)
            .setLoopPingPong();
    }

    private void CheckAndApplyMutators()
    {
        int currentScore = GetCurrentScore();

        if (currentScore == memoryFlashRoundBegin || currentScore == chaosRoundBegin || currentScore == inverseRoundBegin)
        {
            ClearAllMutators();

            if (currentScore == memoryFlashRoundBegin) ActivateMutator(MutatorType.MemoryFlash, 3);
            else if (currentScore == chaosRoundBegin) ActivateMutator(MutatorType.Chaos, 3);
            else if (currentScore == inverseRoundBegin) ActivateMutator(MutatorType.Inverse, 3);

            presentationRoundsLeft = 3;
            return;
        }

        if (presentationRoundsLeft > 0) presentationRoundsLeft--;

        List<MutatorType> currentKeys = new List<MutatorType>(activeMutators.Keys);
        foreach (MutatorType key in currentKeys)
        {
            activeMutators[key]--;
            if (activeMutators[key] <= 0) DeactivateMutator(key);
        }

        // ✅ AQUI ESTÁ LA MAGIA:
        // Solo empezamos a lanzar mutadores al azar cuando hemos superado el INICIO del último mutador 
        // + las 3 rondas que dura su presentación. (Ej: 30 + 3 = 33).
        if (currentScore >= (inverseRoundBegin + 3) && presentationRoundsLeft <= 0)
        {
            int chance = UnityEngine.Random.Range(0, 100);
            if (chance < 25)
            {
                List<MutatorType> available = new List<MutatorType>();

                // Como ya estamos más allá del tutorial del último, sabemos que TODOS están disponibles
                if (!activeMutators.ContainsKey(MutatorType.MemoryFlash)) available.Add(MutatorType.MemoryFlash);
                if (!activeMutators.ContainsKey(MutatorType.Chaos)) available.Add(MutatorType.Chaos);
                if (!activeMutators.ContainsKey(MutatorType.Inverse)) available.Add(MutatorType.Inverse);

                if (available.Count > 0)
                {
                    MutatorType pick = available[UnityEngine.Random.Range(0, available.Count)];
                    int rounds = UnityEngine.Random.Range(3, 6);
                    ActivateMutator(pick, rounds);
                }
            }
        }
    }

    private void ActivateMutator(MutatorType type, int rounds)
    {
        activeMutators[type] = rounds;

        if (type == MutatorType.MemoryFlash)
        {
            FadeIconGroup(0f, 0.5f);
        }
        else if (type == MutatorType.Chaos)
        {
            List<SwipeDirection> dirs = new List<SwipeDirection>
            {
                SwipeDirection.Up, SwipeDirection.Down, SwipeDirection.Left, SwipeDirection.Right
            };

            for (int i = 0; i < dirs.Count; i++)
            {
                int j = UnityEngine.Random.Range(i, dirs.Count);
                (dirs[i], dirs[j]) = (dirs[j], dirs[i]);
            }

            for (int i = 0; i < 4 && i < categories.Count; i++)
                categories[i].correctDirection = dirs[i];

            SetupCategoryUI();

            PunchIcon(SwipeDirection.Up);
            PunchIcon(SwipeDirection.Down);
            PunchIcon(SwipeDirection.Left);
            PunchIcon(SwipeDirection.Right);
        }
        else if (type == MutatorType.Inverse)
        {
            if (centerItemRenderer != null)
                centerItemRenderer.color = new Color(1f, 0.4f, 0.4f);
        }
    }

    private void DeactivateMutator(MutatorType type)
    {
        if (type == MutatorType.MemoryFlash)
        {
            FadeIconGroup(1f, 0.5f);
        }
        else if (type == MutatorType.Chaos)
        {
            foreach (var cat in categories)
            {
                if (cat == null) continue;
                if (originalDirections.TryGetValue(cat, out var dir))
                    cat.correctDirection = dir;
            }

            SetupCategoryUI();
        }
        else if (type == MutatorType.Inverse)
        {
            if (centerItemRenderer != null)
                centerItemRenderer.color = Color.white;
        }

        activeMutators.Remove(type);
    }

    private void ClearAllMutators()
    {
        List<MutatorType> keys = new List<MutatorType>(activeMutators.Keys);
        foreach (MutatorType key in keys)
            DeactivateMutator(key);
    }

    private SwipeDirection GetInverseDirection(SwipeDirection dir)
    {
        return dir switch
        {
            SwipeDirection.Up => SwipeDirection.Down,
            SwipeDirection.Down => SwipeDirection.Up,
            SwipeDirection.Left => SwipeDirection.Right,
            SwipeDirection.Right => SwipeDirection.Left,
            _ => dir
        };
    }

    private void FadeIconGroup(float targetAlpha, float time)
    {
        if (topCategoryIcon != null) { LeanTween.cancel(topCategoryIcon.gameObject); LeanTween.alpha(topCategoryIcon.rectTransform, targetAlpha, time); }
        if (bottomCategoryIcon != null) { LeanTween.cancel(bottomCategoryIcon.gameObject); LeanTween.alpha(bottomCategoryIcon.rectTransform, targetAlpha, time); }
        if (leftCategoryIcon != null) { LeanTween.cancel(leftCategoryIcon.gameObject); LeanTween.alpha(leftCategoryIcon.rectTransform, targetAlpha, time); }
        if (rightCategoryIcon != null) { LeanTween.cancel(rightCategoryIcon.gameObject); LeanTween.alpha(rightCategoryIcon.rectTransform, targetAlpha, time); }
    }

    private void PunchIcon(SwipeDirection dir)
    {
        Image targetIcon = null;

        foreach (var cat in categories)
        {
            if (cat.correctDirection == dir)
            {
                targetIcon = catUIPacks[cat].icon;
                break;
            }
        }

        if (targetIcon != null)
        {
            LeanTween.cancel(targetIcon.gameObject);
            targetIcon.transform.localScale = Vector3.one;

            LeanTween.scale(targetIcon.gameObject, Vector3.one * 1.3f, 0.15f)
                .setEase(LeanTweenType.easeOutQuad)
                .setLoopPingPong(1)
                .setOnComplete(() => { AnimateBreathing(targetIcon); });
        }
    }

    private Vector3 CalculateTargetScale()
    {
        if (centerItemRenderer == null || centerItemRenderer.sprite == null) return Vector3.one;

        float maxDim = Mathf.Max(centerItemRenderer.sprite.bounds.size.x, centerItemRenderer.sprite.bounds.size.y);
        if (maxDim > 0f)
        {
            float scale = targetVisualSize / maxDim;
            return new Vector3(scale, scale, 1f);
        }
        return Vector3.one;
    }

    private void SpawnNewItem(bool startMechanics = true)
    {
        if (startMechanics) CheckAndApplyMutators();

        int randomCategoryIndex = UnityEngine.Random.Range(0, categories.Count);
        CategoryData selectedCategory = categories[randomCategoryIndex];
        if (selectedCategory == null || selectedCategory.validSprites == null || selectedCategory.validSprites.Count == 0) return;

        Sprite selectedSprite = selectedCategory.validSprites[UnityEngine.Random.Range(0, selectedCategory.validSprites.Count)];
        centerItemRenderer.sprite = selectedSprite;

        LeanTween.cancel(centerItemRenderer.gameObject);
        Vector3 finalScale = CalculateTargetScale();
        centerItemRenderer.transform.localScale = Vector3.zero;
        LeanTween.scale(centerItemRenderer.gameObject, finalScale, 0.35f).setEase(LeanTweenType.easeOutBack);

        if (spawnPopAudioclip != null && SoundManager.Instance != null)
        {
           //SoundManager.Instance.PlaySound(spawnPopAudioclip, 1.0f);
        }

        currentCorrectDirection = selectedCategory.correctDirection;

        if (startMechanics)
        {
            timerModule.ResetAndStartTimer();
            inputModule.isInputActive = true;
        }
        else
        {
            inputModule.isInputActive = false;
        }
    }

    private void HandlePlayerSwipe(SwipeDirection playerDirection)
    {
        if (isPausedByOffer) return;

        if (swipeAudioclip != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(swipeAudioclip, 1.0f);
            SoundManager.Instance.PlaySound(swipeAudioclip, 1.0f);
            SoundManager.Instance.PlaySound(swipeAudioclip, 1.0f);
        }

        inputModule.isInputActive = false;
        timerModule.StopTimer();

        SwipeDirection effectiveDirection = playerDirection;
        if (activeMutators.ContainsKey(MutatorType.Inverse))
            effectiveDirection = GetInverseDirection(playerDirection);

        if (effectiveDirection == currentCorrectDirection)
        {
            scoreModule.AddPoint(1);
            timerModule.ApplySuccessReduction();
            AnimateSuccessAndRespawn(currentCorrectDirection);
        }
        else
        {
            if (errorAudioclip != null && errorAudioclip.Length > 0 && SoundManager.Instance != null)
            {
                AudioClip randomErrorAudioClip = errorAudioclip[UnityEngine.Random.Range(0, errorAudioclip.Length)];
                SoundManager.Instance.PlaySound(randomErrorAudioClip, 1.0f);
            }

            HandleGameOver();
        }
    }

    private void AnimateSuccessAndRespawn(SwipeDirection dir)
    {
        Vector3 targetFlyPosition = dir switch
        {
            SwipeDirection.Up => Vector3.up * distY,
            SwipeDirection.Down => Vector3.down * distY,
            SwipeDirection.Left => Vector3.left * distX,
            SwipeDirection.Right => Vector3.right * distX,
            _ => Vector3.zero
        };

        // En lugar de float flyDuration = 0.25f;
        // Haz que baje un poquito con cada punto, con un mínimo de 0.1 segundos:
        float flyDuration = Mathf.Max(0.1f, 0.25f - (scoreModule.CurrentScore * 0.002f)); GameObject item = centerItemRenderer.gameObject;

        LeanTween.cancel(item);

        // 🌟 NUEVO: Le damos un giro aleatorio (entre -90 y 90 grados) para que parezca que la lanzas físicamente
        float randomRotation = UnityEngine.Random.Range(-90f, 90f);
        LeanTween.rotateZ(item, randomRotation, flyDuration)
            .setEase(LeanTweenType.easeInCubic);

        LeanTween.moveLocal(item, targetFlyPosition, flyDuration)
            .setEase(LeanTweenType.easeInBack)
            .setOvershoot(0.4f);

        LeanTween.scale(item, Vector3.zero, flyDuration)
            .setEase(LeanTweenType.easeInCubic)
            .setOnComplete(() =>
            {
                // Acierto
                if (correctAudioclips.Length > 0 && SoundManager.Instance != null)
                {
                    AudioClip randomClip = correctAudioclips[UnityEngine.Random.Range(0, correctAudioclips.Length)];
                    SoundManager.Instance.PlaySound(randomClip, 1.0f);
                }

                // Reseteamos posición y TAMBIÉN LA ROTACIÓN para que la siguiente salga recta
                item.transform.localPosition = Vector3.zero;
                item.transform.localRotation = Quaternion.identity; // <-- ¡Clave para no romper el juego!

                if (!isGameOver)
                {
                    PunchIcon(dir);
                    SpawnNewItem(true);
                }
            });
    }

    // ==========================================
    // LÓGICA IGameOverClient / AD FLOW
    // ==========================================

    private void HandleGameOver()
    {
        if (isGameOver || gameOverInvoked) return;
        isGameOver = true;

        inputModule.isInputActive = false;
        timerModule.StopTimer();

        if (GameOverFlowManager.Instance != null)
            GameOverFlowManager.Instance.NotifyFail(this);
        else
            FinalGameOver();
    }

    public void PauseOnFail()
    {
        isPausedByOffer = true;
        inputModule.isInputActive = false;
        timerModule.StopTimer();
    }

    public void Revive()
    {
        isPausedByOffer = false;
        isGameOver = false;
        hasEnded = false;
        gameOverInvoked = false;

        timerModule.ResetAndStartTimer();
        inputModule.isInputActive = true;
    }

    public void FinalGameOver()
    {
        if (gameOverInvoked) return;
        gameOverInvoked = true;

        EndGame();
    }

    private void EndGame()
    {
        if (hasEnded) return;
        hasEnded = true;

        ClassifierScore.Instance.SafeRecordIfNeeded();
        OnClassifierGameOver?.Invoke(this, EventArgs.Empty);

        if (PlayFabLoginManager.Instance != null && PlayFabLoginManager.Instance.IsLoggedIn)
        {
            PlayFabScoreManager.Instance.SubmitScore("ClassifierScore", ClassifierScore.Instance.GetScore());
        }

        int coinsEarned = ClassifierScore.Instance.GetCoinsEarned();
        CoinsRewardUI rewardUI = FindObjectOfType<CoinsRewardUI>(true);
        if (rewardUI != null) rewardUI.ShowReward(coinsEarned);
        else CurrencyManager.Instance.AddCoins(coinsEarned);

        if (DailyMissionManager.Instance != null)
        {
            DailyMissionManager.Instance.AddProgress("juega_1_partida", 1);
            DailyMissionManager.Instance.AddProgress("juega_3_partidas", 1);
            DailyMissionManager.Instance.AddProgress("juega_8_partidas", 1);
            DailyMissionManager.Instance.AddProgress("juega_10_partidas", 1);

            if (ClassifierScore.Instance.GetScore() >= 20) DailyMissionManager.Instance.AddProgress("consigue_20_puntos_clasificar", 1);
        }
    }

    public int GetCurrentScore()
    {
        return scoreModule.CurrentScore;
    }
}