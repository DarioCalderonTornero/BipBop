using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ClassifierInput))]
[RequireComponent(typeof(ClassifierTimer))]
[RequireComponent(typeof(ClassifierScore))]
public class ClassifierManager : MonoBehaviour
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

    private ClassifierInput inputModule;
    private ClassifierTimer timerModule;
    [SerializeField] private ClassifierScore scoreModule;

    private SwipeDirection currentCorrectDirection;
    private bool isGameOver = false;
    private bool hasEnded = false;

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
    // Tutorial (igual que Color)
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

        // 👇 Binding robusto: si ClassifierState aún no existe, esperamos.
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
        // Espera hasta que exista el State
        while (ClassifierState.Instance == null)
            yield return null;

        // Por seguridad, reenganchar siempre limpio
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
        activeMutators.Clear();
        presentationRoundsLeft = 0;

        if (categories == null || categories.Count != 4)
            Debug.LogError("ClassifierManager: 'categories' debe tener EXACTAMENTE 4 elementos.");

        // 1) Guardar slots físicos
        if (topCategoryIcon != null) { slotIconPos[SwipeDirection.Up] = topCategoryIcon.transform.position; slotBgPos[SwipeDirection.Up] = topCategoryBg.transform.position; }
        if (bottomCategoryIcon != null) { slotIconPos[SwipeDirection.Down] = bottomCategoryIcon.transform.position; slotBgPos[SwipeDirection.Down] = bottomCategoryBg.transform.position; }
        if (leftCategoryIcon != null) { slotIconPos[SwipeDirection.Left] = leftCategoryIcon.transform.position; slotBgPos[SwipeDirection.Left] = leftCategoryBg.transform.position; }
        if (rightCategoryIcon != null) { slotIconPos[SwipeDirection.Right] = rightCategoryIcon.transform.position; slotBgPos[SwipeDirection.Right] = rightCategoryBg.transform.position; }

        // 2) Vincular packs iniciales
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

        // Item inicial, pero SIN mecánicas
        SpawnNewItem(false);

        // 🔒 Pausa real: input/timer off
        PauseGameplay();

        // Tutorial flow (igual que Color)
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

    // ==================
    // Tutorial flow
    // ==================
    private void ShowTutorial()
    {
        if (tutorialInstance != null)
        {
            tutorialInstance.gameObject.SetActive(true);
        }
        else
        {
            // 1) Primero intenta encontrar uno ya en escena (aunque esté inactivo)
            var existing = FindObjectOfType<TutorialPanelUI>(true);
            if (existing != null)
            {
                tutorialInstance = existing;
                tutorialInstance.gameObject.SetActive(true);
            }
            else
            {
                // 2) Si no hay, intenta instanciar el prefab si está asignado
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
        // 👇 Arranque robusto: espera a que exista el state y entonces inicia countdown
        StartCoroutine(BeginCountdownWhenStateReady());
    }

    private IEnumerator BeginCountdownWhenStateReady()
    {
        while (ClassifierState.Instance == null)
            yield return null;

        // Reinicio de flags run
        isGameOver = false;
        hasEnded = false;

        // Asegurar que gameplay está parado hasta Playing
        PauseGameplay();

        ClassifierState.Instance.StartCountdown();
    }

    private void PauseGameplay()
    {
        inputModule.isInputActive = false;
        timerModule.StopTimer();
    }

    // ==================
    // Estado: empieza el juego al pasar a Playing
    // ==================
    private void ClassifierState_OnPlayingClassifierGame(object sender, EventArgs e)
    {
        if (isGameOver) return;

        // ✅ ESTE ERA EL PUNTO CLAVE: si no te suscribes, nunca llega aquí
        timerModule.ResetAndStartTimer();
        inputModule.isInputActive = true;

        // Por si venías del “spawn inicial” sin mecánicas:
        // Ya hay sprite puesto, no hace falta respawnear aquí.
    }

    private void TimerModule_OnTimeOut(object sender, EventArgs e)
    {
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

            if (pack.icon != null && slotIconPos.ContainsKey(targetSlot))
                pack.icon.transform.position = slotIconPos[targetSlot];

            if (pack.bg != null && slotBgPos.ContainsKey(targetSlot))
                pack.bg.transform.position = slotBgPos[targetSlot];
        }
    }

    // --- ANIMACIONES UI ---
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

    // --- MUTADORES (tu lógica igual) ---
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

        if (currentScore > memoryFlashRoundBegin && presentationRoundsLeft <= 0)
        {
            int chance = UnityEngine.Random.Range(0, 100);
            if (chance < 25)
            {
                List<MutatorType> available = new List<MutatorType>();

                if (currentScore >= memoryFlashRoundBegin && !activeMutators.ContainsKey(MutatorType.MemoryFlash))
                    available.Add(MutatorType.MemoryFlash);

                if (currentScore >= chaosRoundBegin && !activeMutators.ContainsKey(MutatorType.Chaos))
                    available.Add(MutatorType.Chaos);

                if (currentScore >= inverseRoundBegin && !activeMutators.ContainsKey(MutatorType.Inverse))
                    available.Add(MutatorType.Inverse);

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

        float flyDuration = 0.25f;
        GameObject item = centerItemRenderer.gameObject;

        LeanTween.cancel(item);

        LeanTween.moveLocal(item, targetFlyPosition, flyDuration)
            .setEase(LeanTweenType.easeInBack)
            .setOvershoot(0.4f);

        LeanTween.scale(item, Vector3.zero, flyDuration)
            .setEase(LeanTweenType.easeInCubic)
            .setOnComplete(() =>
            {
                item.transform.localPosition = Vector3.zero;

                if (!isGameOver)
                {
                    PunchIcon(dir);
                    SpawnNewItem(true);
                }
            });
    }

    private void HandleGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        inputModule.isInputActive = false;
        timerModule.StopTimer();

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
    }

    public int GetCurrentScore()
    {
        return scoreModule.CurrentScore;
    }
}