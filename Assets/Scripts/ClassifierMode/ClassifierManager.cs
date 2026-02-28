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

    [Header("Base de Datos")]
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

    // --- VARIABLES DE MUTADORES ---
    private enum MutatorType { MemoryFlash, Chaos, Inverse }

    private Dictionary<MutatorType, int> activeMutators = new Dictionary<MutatorType, int>();

    // ¡NUEVO! Actualizados a las rondas que pediste
    [SerializeField] private int memoryFlashRoundBegin = 2;
    [SerializeField] private int chaosRoundBegin = 8;
    [SerializeField] private int inverseRoundBegin = 14;

    private int presentationRoundsLeft = 0;

    private Dictionary<CategoryData, SwipeDirection> originalDirections = new Dictionary<CategoryData, SwipeDirection>();

    // Variables ultra-simples para guardar cómo eran los fondos al arrancar
    private Sprite origTopBgSprite, origBottomBgSprite, origLeftBgSprite, origRightBgSprite;
    private Color origTopBgColor = Color.white, origBottomBgColor = Color.white, origLeftBgColor = Color.white, origRightBgColor = Color.white;

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        inputModule = GetComponent<ClassifierInput>();
        timerModule = GetComponent<ClassifierTimer>();
        if (scoreModule == null) scoreModule = GetComponent<ClassifierScore>();
    }

    void OnEnable()
    {
        inputModule.OnSwipeDetected += HandlePlayerSwipe;
        timerModule.OnTimeOut += TimerModule_OnTimeOut;
    }

    void OnDisable()
    {
        inputModule.OnSwipeDetected -= HandlePlayerSwipe;
        timerModule.OnTimeOut -= TimerModule_OnTimeOut;

        if (ClassifierState.Instance != null)
        {
            ClassifierState.Instance.OnPlayingClassifierGame -= ClassifierState_OnPlayingClassifierGame;
        }
    }

    void Start()
    {
        isGameOver = false;
        activeMutators.Clear();
        presentationRoundsLeft = 0;

        // Guardamos las direcciones originales
        foreach (var cat in categories)
        {
            originalDirections[cat] = cat.correctDirection;
        }

        // Guardamos una copia exacta de los sprites y colores de los fondos al empezar
        if (topCategoryBg != null) { origTopBgSprite = topCategoryBg.sprite; origTopBgColor = topCategoryBg.color; }
        if (bottomCategoryBg != null) { origBottomBgSprite = bottomCategoryBg.sprite; origBottomBgColor = bottomCategoryBg.color; }
        if (leftCategoryBg != null) { origLeftBgSprite = leftCategoryBg.sprite; origLeftBgColor = leftCategoryBg.color; }
        if (rightCategoryBg != null) { origRightBgSprite = rightCategoryBg.sprite; origRightBgColor = rightCategoryBg.color; }

        if (ClassifierState.Instance != null)
        {
            ClassifierState.Instance.OnPlayingClassifierGame += ClassifierState_OnPlayingClassifierGame;
        }

        SetupCategoryUI();
        StartUIBreathing();
        SpawnNewItem(false);
    }

    private void ClassifierState_OnPlayingClassifierGame(object sender, EventArgs e)
    {
        if (isGameOver) return;
        timerModule.ResetAndStartTimer();
        inputModule.isInputActive = true;
    }

    private void TimerModule_OnTimeOut(object sender, System.EventArgs e)
    {
        HandleGameOver();
    }

    private void SetupCategoryUI()
    {
        foreach (CategoryData category in categories)
        {
            if (category.categoryIcon == null) continue;

            // 1. Miramos dónde estaba esta categoría originalmente
            SwipeDirection origDir = originalDirections[category];
            Sprite targetBgSprite = null;
            Color targetBgColor = Color.white;

            // 2. Le asignamos su fondo original, sin importar dónde vaya a ir ahora
            switch (origDir)
            {
                case SwipeDirection.Up: targetBgSprite = origTopBgSprite; targetBgColor = origTopBgColor; break;
                case SwipeDirection.Down: targetBgSprite = origBottomBgSprite; targetBgColor = origBottomBgColor; break;
                case SwipeDirection.Left: targetBgSprite = origLeftBgSprite; targetBgColor = origLeftBgColor; break;
                case SwipeDirection.Right: targetBgSprite = origRightBgSprite; targetBgColor = origRightBgColor; break;
            }

            // 3. Colocamos el icono y su fondo fiel en la posición que le toque en esta ronda
            switch (category.correctDirection)
            {
                case SwipeDirection.Up:
                    if (topCategoryIcon != null) topCategoryIcon.sprite = category.categoryIcon;
                    if (topCategoryBg != null) { topCategoryBg.sprite = targetBgSprite; topCategoryBg.color = targetBgColor; }
                    break;
                case SwipeDirection.Down:
                    if (bottomCategoryIcon != null) bottomCategoryIcon.sprite = category.categoryIcon;
                    if (bottomCategoryBg != null) { bottomCategoryBg.sprite = targetBgSprite; bottomCategoryBg.color = targetBgColor; }
                    break;
                case SwipeDirection.Left:
                    if (leftCategoryIcon != null) leftCategoryIcon.sprite = category.categoryIcon;
                    if (leftCategoryBg != null) { leftCategoryBg.sprite = targetBgSprite; leftCategoryBg.color = targetBgColor; }
                    break;
                case SwipeDirection.Right:
                    if (rightCategoryIcon != null) rightCategoryIcon.sprite = category.categoryIcon;
                    if (rightCategoryBg != null) { rightCategoryBg.sprite = targetBgSprite; rightCategoryBg.color = targetBgColor; }
                    break;
            }
        }
    }

    // --- ANIMACIONES DE UI (LEANTWEEN) ---

    private void StartUIBreathing()
    {
        AnimateBreathing(topCategoryIcon);
        AnimateBreathing(bottomCategoryIcon);
        AnimateBreathing(leftCategoryIcon);
        AnimateBreathing(rightCategoryIcon);
    }

    private void AnimateBreathing(Image icon)
    {
        if (icon == null) return;
        LeanTween.scale(icon.gameObject, Vector3.one * 1.08f, 1.2f)
            .setEase(LeanTweenType.easeInOutSine)
            .setLoopPingPong();
    }

    private void PunchIcon(SwipeDirection dir)
    {
        Image targetIcon = dir switch
        {
            SwipeDirection.Up => topCategoryIcon,
            SwipeDirection.Down => bottomCategoryIcon,
            SwipeDirection.Left => leftCategoryIcon,
            SwipeDirection.Right => rightCategoryIcon,
            _ => null
        };

        if (targetIcon != null)
        {
            LeanTween.cancel(targetIcon.gameObject);
            targetIcon.transform.localScale = Vector3.one;

            LeanTween.scale(targetIcon.gameObject, Vector3.one * 1.3f, 0.15f)
                .setEase(LeanTweenType.easeOutQuad)
                .setLoopPingPong(1)
                .setOnComplete(() =>
                {
                    AnimateBreathing(targetIcon);
                });
        }
    }

    // --- SISTEMA DE MUTADORES (NUEVO CEREBRO) ---

    private void CheckAndApplyMutators()
    {
        int currentScore = GetCurrentScore();

        // 1. PRESENTACIONES OBLIGATORIAS (Hitos exactos)
        // Si el jugador llega exactamente a uno de estos puntos, limpieza nuclear.
        if (currentScore == memoryFlashRoundBegin || currentScore == chaosRoundBegin || currentScore == inverseRoundBegin)
        {
            ClearAllMutators();

            if (currentScore == memoryFlashRoundBegin) ActivateMutator(MutatorType.MemoryFlash, 3);
            else if (currentScore == chaosRoundBegin) ActivateMutator(MutatorType.Chaos, 3);
            else if (currentScore == inverseRoundBegin) ActivateMutator(MutatorType.Inverse, 3);

            presentationRoundsLeft = 3; // Activamos escudo
            return; // ¡Salimos de la función! Nada más puede ocurrir en esta ronda.
        }

        // Reducimos el escudo de presentación si está activo
        if (presentationRoundsLeft > 0)
        {
            presentationRoundsLeft--;
        }

        // 2. GESTIÓN DE RONDAS ACTIVAS 
        List<MutatorType> currentKeys = new List<MutatorType>(activeMutators.Keys);
        foreach (MutatorType key in currentKeys)
        {
            activeMutators[key]--;
            if (activeMutators[key] <= 0)
            {
                DeactivateMutator(key);
            }
        }

        // 3. ALEATORIEDAD (Apilar nuevos mutadores)
        if (currentScore > memoryFlashRoundBegin && presentationRoundsLeft <= 0)
        {
            int chance = UnityEngine.Random.Range(0, 100);
            if (chance < 25)
            {
                List<MutatorType> availableMutators = new List<MutatorType>();

                if (currentScore >= memoryFlashRoundBegin && !activeMutators.ContainsKey(MutatorType.MemoryFlash))
                    availableMutators.Add(MutatorType.MemoryFlash);

                if (currentScore >= chaosRoundBegin && !activeMutators.ContainsKey(MutatorType.Chaos))
                    availableMutators.Add(MutatorType.Chaos);

                if (currentScore >= inverseRoundBegin && !activeMutators.ContainsKey(MutatorType.Inverse))
                    availableMutators.Add(MutatorType.Inverse);

                if (availableMutators.Count > 0)
                {
                    MutatorType randomChoice = availableMutators[UnityEngine.Random.Range(0, availableMutators.Count)];
                    int randomRounds = UnityEngine.Random.Range(3, 6);
                    ActivateMutator(randomChoice, randomRounds);
                }
            }
        }
    }

    private void ActivateMutator(MutatorType type, int rounds)
    {
        activeMutators[type] = rounds;
        Debug.Log($"<color=orange>¡MUTADOR ACTIVADO!</color> Tipo: {type} por {rounds} rondas.");

        if (type == MutatorType.MemoryFlash)
        {
            FadeIconGroup(0f, 0.5f);
        }
        else if (type == MutatorType.Chaos)
        {
            List<SwipeDirection> dirs = new List<SwipeDirection> {
                SwipeDirection.Up, SwipeDirection.Down, SwipeDirection.Left, SwipeDirection.Right
            };

            for (int i = 0; i < dirs.Count; i++)
            {
                SwipeDirection temp = dirs[i];
                int randomIndex = UnityEngine.Random.Range(i, dirs.Count);
                dirs[i] = dirs[randomIndex];
                dirs[randomIndex] = temp;
            }

            for (int i = 0; i < categories.Count; i++)
            {
                categories[i].correctDirection = dirs[i];
            }

            SetupCategoryUI();
            PunchIcon(SwipeDirection.Up);
            PunchIcon(SwipeDirection.Down);
            PunchIcon(SwipeDirection.Left);
            PunchIcon(SwipeDirection.Right);
        }
        else if (type == MutatorType.Inverse)
        {
            centerItemRenderer.color = new Color(1f, 0.4f, 0.4f);
        }
    }

    private void DeactivateMutator(MutatorType type)
    {
        Debug.Log($"<color=cyan>Mutador {type} finalizado. Volviendo a la normalidad.</color>");

        if (type == MutatorType.MemoryFlash)
        {
            FadeIconGroup(1f, 0.5f);
        }
        else if (type == MutatorType.Chaos)
        {
            foreach (var cat in categories)
            {
                cat.correctDirection = originalDirections[cat];
            }
            SetupCategoryUI();
        }
        else if (type == MutatorType.Inverse)
        {
            centerItemRenderer.color = Color.white;
        }

        activeMutators.Remove(type);
    }

    private void ClearAllMutators()
    {
        List<MutatorType> currentKeys = new List<MutatorType>(activeMutators.Keys);
        foreach (MutatorType key in currentKeys)
        {
            DeactivateMutator(key);
        }
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

    private Vector3 CalculateTargetScale()
    {
        if (centerItemRenderer.sprite == null) return Vector3.one;
        float maxDim = Mathf.Max(centerItemRenderer.sprite.bounds.size.x, centerItemRenderer.sprite.bounds.size.y);

        if (maxDim > 0)
        {
            float scale = targetVisualSize / maxDim;
            return new Vector3(scale, scale, 1f);
        }
        return Vector3.one;
    }

    private void SpawnNewItem(bool startMechanics = true)
    {
        if (startMechanics)
        {
            CheckAndApplyMutators();
        }

        int randomCategoryIndex = UnityEngine.Random.Range(0, categories.Count);
        CategoryData selectedCategory = categories[randomCategoryIndex];

        if (selectedCategory.validSprites.Count == 0) return;

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
        {
            effectiveDirection = GetInverseDirection(playerDirection);
        }

        if (effectiveDirection == currentCorrectDirection)
        {
            Debug.Log("<color=green>¡ACIERTO!</color>");
            scoreModule.AddPoint(1);
            timerModule.ApplySuccessReduction();

            AnimateSuccessAndRespawn(currentCorrectDirection);
        }
        else
        {
            Debug.Log($"<color=red>¡FALLO FATAL!</color> Lo lanzaste de forma efectiva hacia {effectiveDirection} y era {currentCorrectDirection}.");
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
        OnClassifierGameOver?.Invoke(this, System.EventArgs.Empty);

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