// DifferentManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class DifferentManager : MonoBehaviour, IGameOverClient
{
    public static DifferentManager Instance { get; private set; }

    public event EventHandler OnDifferentGameOver;

    public event Action OnRoundChanged;
    public event Action<int> OnScoreChanged;

    [Header("UI (Opcional)")]
    [SerializeField] private TextMeshProUGUI scoreText;
    public Color scoreHighlightColor = Color.yellow; // <-- NUEVO: Color del destello
    private Vector3 originalScoreScale; // <-- NUEVO: Escala original guardada

    [SerializeField] private Image timeBarImage;
    [SerializeField] private TextMeshProUGUI instructionText;

    [Header("Timer")]
    [SerializeField] private bool useTimer = true;
    [SerializeField] private float startTime = 30f;

    [Header("Grid")]
    [SerializeField] private RectTransform gridRoot;
    [SerializeField] private DifferentTile tilePrefab;
    [SerializeField] private int itemCount = 16;

    [Header("Gameplay")]
    [SerializeField] private bool failOnWrongClick = true;
    [SerializeField] private float timeBonusOnCorrect = 0.75f;

    public enum DifferenceType
    {
        TintColor,
        Rotation,
        Scale,
        MirrorX,
        UpsideDown,
        Sprite
    }

    [Header("Patterns (Opcional)")]
    [SerializeField] private Sprite[] patternSprites;
    [SerializeField] private Color[] patternColors;

    [Header("Odd Tint Colors (para TintColor)")]
    [SerializeField] private Color[] oddTintColors;

    [Header("Allowed Differences")]
    [SerializeField] private bool allowColor = true;
    [SerializeField] private bool allowRotation = true;
    [SerializeField] private bool allowScale = true;
    [SerializeField] private bool allowSprite = true;

    [Header("Tuning")]
    [SerializeField] private Vector2 rotationDeltaRange = new Vector2(12f, 60f);
    [SerializeField] private Vector2 oddScaleMultiplierRange = new Vector2(0.78f, 1.28f);
    [SerializeField] private float minScaleDeltaFromOne = 0.10f;

    [Header("Different moves")]
    [SerializeField] private float oddSwapInterval = 1.5f;
    [SerializeField] private float oddSwapAnimDuration = 0.12f;

    [Header("Sounds")]
    [SerializeField] private AudioClip correctAudioClip1;
    [SerializeField] private AudioClip correctAudioClip2;
    [SerializeField] private AudioClip errorAudioClip;

    private readonly List<DifferentTile> tiles = new List<DifferentTile>();
    private int oddIndex = -1;
    private DifferenceType currentType;

    private int score = 0;
    private float currentTime;
    private bool isRunning;

    // estado del patrón actual
    private Sprite currentBaseSprite;
    private Color currentBaseColor;

    private Color currentOddColor;
    private Sprite currentOddSprite;
    private float currentOddRotDelta;
    private float currentOddScaleMul;

    private Coroutine oddSwapRoutine;

    private Color currentOddTintColor;

    [SerializeField] private int scoreToEnableOddSwap = 50;

    [Header("Round Transition FX")]
    [SerializeField] private bool playRoundTransitionOnCorrect = true;
    [SerializeField] private float roundTransitionDuration = 0.16f;
    [SerializeField] private float roundTransitionMinScaleMul = 0.08f;

    private bool isTransitioning;
    private Coroutine transitionRoutine;

    public enum RoundMode
    {
        FindDifferent,
        FindSingleton
    }

    [Header("Modes")]
    [SerializeField] private bool allowSingletonMode = true;
    [SerializeField, Range(0f, 1f)] private float singletonModeChance = 0.20f;

    private RoundMode currentMode = RoundMode.FindDifferent;

    [Header("Economy / Meta")]
    [SerializeField] private string playFabStatName = "DifferentScore";
    [SerializeField] private string recordPlayerPrefsKey = "MaxRecordDifferent";
    [SerializeField] private int xpPerPoint = 10;
    [SerializeField] private int coinsDivisor = 3;

    [Header("Countdown + Preview Shuffle")]
    [SerializeField] private bool useCountdown = true;
    [SerializeField] private float countdownSeconds = 3f;
    [SerializeField] private float previewSwapInterval = 0.5f;

    [SerializeField] private Vector2 previewScaleMulRange = new Vector2(0.85f, 1.10f);
    [SerializeField] private bool previewAllowRotation = true;
    [SerializeField] private bool previewAllowFlip = true;
    [SerializeField] private bool previewAllowTint = true;

    private Coroutine countdownRoutine;
    private Coroutine previewRoutine;

    [Header("Tutorial Panel")]
    [SerializeField] private bool showTutorialOnStart = true;
    [SerializeField] private TutorialPanelUI tutorialPrefab;
    [SerializeField] private Transform tutorialParent;

    private TutorialPanelUI tutorialInstance;

    private const string ShowTutorialKey = "ShowTutorialOnStart";

    [Header("Instruction Localization")]
    [SerializeField] private LocalizedString lsInstructionFindDifferent;
    [SerializeField] private LocalizedString lsInstructionFindSingleton;

    [Header("Fail Reveal (Before GameOver)")]
    [SerializeField] private bool playFailReveal = true;
    [SerializeField] private float failRevealDuration = 0.9f;
    [SerializeField] private float failRevealScaleMul = 1.65f;
    [SerializeField] private int failRevealBlinks = 6;
    [SerializeField] private float failRevealTimeScale = 0.15f;
    [SerializeField] private Color failRevealBlinkColor = new Color(1f, 0.15f, 0.15f, 1f);

    // =========================
    // ADS / GameOverFlow (NEW)
    // =========================
    public bool HasUsedReviveOffer { get; set; } = false;
    private bool isPausedByOffer = false;
    private bool gameOverInvoked = false;

    private bool hasEnded = false;

    private void Awake()
    {
        Instance = this;

        // <-- NUEVO: Guardamos la escala original al arrancar
        if (scoreText != null)
        {
            originalScoreScale = scoreText.transform.localScale;
        }
    }

    private void Start()
    {
        isRunning = false;
        hasEnded = false;
        gameOverInvoked = false;

        // Reset de flow por partida
        HasUsedReviveOffer = false;
        isPausedByOffer = false;

        showTutorialOnStart = PlayerPrefs.GetInt(ShowTutorialKey, 1) == 1;

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

    private void HideAnyExistingTutorialPanel()
    {
        var existing = FindObjectOfType<TutorialPanelUI>(true);
        if (existing != null)
            existing.gameObject.SetActive(false);
    }

    private void HandleTutorialClosed()
    {
        if (tutorialInstance != null)
            tutorialInstance.OnClosed -= HandleTutorialClosed;

        tutorialInstance = null;
        StartGame();
    }

    private void BeginGameAfterTutorial()
    {
        StartGame();
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
                Canvas c = (gridRoot != null) ? gridRoot.GetComponentInParent<Canvas>() : FindObjectOfType<Canvas>();
                parent = (c != null) ? c.transform : transform;
            }

            tutorialInstance = Instantiate(tutorialPrefab, parent);
        }

        tutorialInstance.OnClosed -= HandleTutorialClosed;
        tutorialInstance.OnClosed += HandleTutorialClosed;

        PauseGameplay();
    }

    public void StartGame()
    {
        // Reset run
        score = 0;
        currentTime = startTime;
        isRunning = false;

        hasEnded = false;
        gameOverInvoked = false;

        HasUsedReviveOffer = false;
        isPausedByOffer = false;

        BuildGrid();
        UpdateUI(false); // Reset UI sin animar
        SetupRound();

        PauseGameplay();

        if (useCountdown && DifferentState.Instance != null)
        {
            if (previewRoutine != null) StopCoroutine(previewRoutine);
            previewRoutine = StartCoroutine(PreviewShuffleRoutine());

            DifferentState.Instance.StartCountdown();
        }
        else
        {
            if (useCountdown)
            {
                if (countdownRoutine != null) StopCoroutine(countdownRoutine);
                countdownRoutine = StartCoroutine(CountdownAndPreviewRoutine());
            }
            else
            {
                ResumeGameplay();
            }
        }
    }

    public void ResumeGameplay()
    {
        if (hasEnded) return;

        if (previewRoutine != null)
        {
            StopCoroutine(previewRoutine);
            previewRoutine = null;
        }

        isRunning = true;
    }

    public void PauseGameplay()
    {
        isRunning = false;
    }

    private void Update()
    {
        if (!isRunning) return;
        if (isPausedByOffer) return;

        if (useTimer)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0f)
            {
                currentTime = 0f;
                TriggerFail();
                return;
            }
            UpdateUI(false); // Actualizar barra de tiempo (sin animar score)
        }
    }

    private void BuildGrid()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            if (tiles[i] != null) Destroy(tiles[i].gameObject);
        }
        tiles.Clear();

        if (gridRoot == null || tilePrefab == null) return;

        for (int i = 0; i < itemCount; i++)
        {
            var t = Instantiate(tilePrefab, gridRoot);
            int captured = i;
            t.Bind(captured, OnTileClicked);
            tiles.Add(t);
        }
    }

    private void SetupRound()
    {
        if (hasEnded) return;
        if (tiles.Count == 0) return;

        bool doSingleton = allowSingletonMode
                           && patternSprites != null
                           && patternSprites.Length >= 3
                           && UnityEngine.Random.value < singletonModeChance;

        if (doSingleton)
        {
            currentMode = RoundMode.FindSingleton;
            SetupSingletonRound();
            OnRoundChanged?.Invoke();
            return;
        }

        currentMode = RoundMode.FindDifferent;

        RefreshInstructionText();

        currentType = PickAllowedType();

        currentBaseSprite = (patternSprites != null && patternSprites.Length > 0)
            ? patternSprites[UnityEngine.Random.Range(0, patternSprites.Length)]
            : null;

        currentBaseColor = (patternColors != null && patternColors.Length > 0)
            ? patternColors[UnityEngine.Random.Range(0, patternColors.Length)]
            : Color.white;

        for (int i = 0; i < tiles.Count; i++)
            tiles[i].ApplyBase(currentBaseSprite, currentBaseColor);

        oddIndex = UnityEngine.Random.Range(0, tiles.Count);
        PrepareOddDelta();
        ApplyOddInstant(oddIndex);

        RestartOddSwapRoutine();
        OnRoundChanged?.Invoke();
    }

    private IEnumerator CountdownAndPreviewRoutine()
    {
        if (previewRoutine != null) StopCoroutine(previewRoutine);
        previewRoutine = StartCoroutine(PreviewShuffleRoutine());

        float t = countdownSeconds;

        while (t > 0f)
        {
            if (instructionText != null)
                instructionText.text = Mathf.CeilToInt(t).ToString();

            t -= Time.deltaTime;
            yield return null;
        }

        if (previewRoutine != null)
        {
            StopCoroutine(previewRoutine);
            previewRoutine = null;
        }

        SetupRound();
        RefreshInstructionText();

        isRunning = true;
        countdownRoutine = null;
    }

    private IEnumerator PreviewShuffleRoutine()
    {
        var wait = new WaitForSeconds(previewSwapInterval);

        while (true)
        {
            ApplyPreviewToAllTiles();
            yield return wait;
        }
    }

    private void ApplyPreviewToAllTiles()
    {
        if (tiles.Count == 0) return;
        if (patternSprites == null || patternSprites.Length == 0) return;

        for (int i = 0; i < tiles.Count; i++)
        {
            DifferentTile t = tiles[i];
            if (t == null) continue;

            Sprite sp = patternSprites[UnityEngine.Random.Range(0, patternSprites.Length)];

            Color baseCol = (patternColors != null && patternColors.Length > 0)
                ? patternColors[UnityEngine.Random.Range(0, patternColors.Length)]
                : Color.white;

            Color col = baseCol;
            if (previewAllowTint)
            {
                if (oddTintColors != null && oddTintColors.Length > 0 && UnityEngine.Random.value < 0.55f)
                    col = oddTintColors[UnityEngine.Random.Range(0, oddTintColors.Length)];
                else if (UnityEngine.Random.value < 0.55f)
                    col = GenerateTintedColor(baseCol);
            }

            float rotZ = 0f;
            if (previewAllowRotation)
                rotZ = UnityEngine.Random.Range(0f, 360f);

            bool flipX = previewAllowFlip && (UnityEngine.Random.value < 0.5f);
            bool flipY = previewAllowFlip && (UnityEngine.Random.value < 0.15f);

            float scaleMul = UnityEngine.Random.Range(previewScaleMulRange.x, previewScaleMulRange.y);

            t.ApplyPreview(sp, col, rotZ, flipX, flipY, scaleMul, stopCurrentAnim: true);
        }
    }

    private void SetupSingletonRound()
    {
        RefreshInstructionText();

        currentBaseColor = (patternColors != null && patternColors.Length > 0)
            ? patternColors[UnityEngine.Random.Range(0, patternColors.Length)]
            : Color.white;

        Sprite unique = patternSprites[UnityEngine.Random.Range(0, patternSprites.Length)];

        oddIndex = UnityEngine.Random.Range(0, tiles.Count);

        Sprite[] assigned = GenerateSingletonDistribution(unique, tiles.Count);

        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].ApplyBase(assigned[i], currentBaseColor);
        }

        if (oddSwapRoutine != null)
        {
            StopCoroutine(oddSwapRoutine);
            oddSwapRoutine = null;
        }
    }

    private Sprite[] GenerateSingletonDistribution(Sprite unique, int count)
    {
        Sprite[] result = new Sprite[count];
        result[oddIndex] = unique;

        List<int> free = new List<int>(count - 1);
        for (int i = 0; i < count; i++)
            if (i != oddIndex) free.Add(i);

        int repeatedTypes = Mathf.Clamp(UnityEngine.Random.Range(3, 7), 3, Mathf.Min(7, free.Count / 2));

        List<Sprite> pool = new List<Sprite>(patternSprites.Length);
        for (int i = 0; i < patternSprites.Length; i++)
            if (patternSprites[i] != null && patternSprites[i] != unique)
                pool.Add(patternSprites[i]);

        for (int i = 0; i < pool.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        repeatedTypes = Mathf.Min(repeatedTypes, pool.Count);

        List<Sprite> chosen = pool.GetRange(0, repeatedTypes);

        List<Sprite> bag = new List<Sprite>(free.Count);
        for (int i = 0; i < chosen.Count; i++)
        {
            bag.Add(chosen[i]);
            bag.Add(chosen[i]);
        }

        while (bag.Count < free.Count)
            bag.Add(chosen[UnityEngine.Random.Range(0, chosen.Count)]);

        for (int i = 0; i < bag.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, bag.Count);
            (bag[i], bag[j]) = (bag[j], bag[i]);
        }

        for (int k = 0; k < free.Count; k++)
            result[free[k]] = bag[k];

        return result;
    }

    private void RestartOddSwapRoutine()
    {
        if (currentMode == RoundMode.FindSingleton)
        {
            if (oddSwapRoutine != null)
            {
                StopCoroutine(oddSwapRoutine);
                oddSwapRoutine = null;
            }
            return;
        }

        if (score < scoreToEnableOddSwap)
        {
            if (oddSwapRoutine != null)
            {
                StopCoroutine(oddSwapRoutine);
                oddSwapRoutine = null;
            }
            return;
        }

        if (oddSwapRoutine != null)
            StopCoroutine(oddSwapRoutine);

        oddSwapRoutine = StartCoroutine(OddSwapLoop());
    }

    private IEnumerator OddSwapLoop()
    {
        while (isRunning && !isPausedByOffer && !hasEnded)
        {
            yield return new WaitForSeconds(oddSwapInterval);

            if (!isRunning || isPausedByOffer || hasEnded) yield break;
            if (tiles.Count <= 1) continue;

            int newIndex = oddIndex;
            int guard = 0;
            while (newIndex == oddIndex && guard++ < 20)
                newIndex = UnityEngine.Random.Range(0, tiles.Count);

            SwapOddTo(newIndex);
        }
    }

    private void SwapOddTo(int newIndex)
    {
        if (newIndex == oddIndex) return;

        int oldIndex = oddIndex;
        oddIndex = newIndex;

        AnimateToBase(oldIndex);
        AnimateToOdd(newIndex);
    }

    private DifferenceType PickAllowedType()
    {
        var allowed = new List<DifferenceType>(6);

        if (allowColor) allowed.Add(DifferenceType.TintColor);

        if (allowRotation) allowed.Add(DifferenceType.Rotation);
        if (allowScale) allowed.Add(DifferenceType.Scale);

        if (allowRotation) allowed.Add(DifferenceType.MirrorX);
        if (allowRotation) allowed.Add(DifferenceType.UpsideDown);

        if (allowSprite && patternSprites != null && patternSprites.Length >= 2)
            allowed.Add(DifferenceType.Sprite);

        return allowed.Count == 0 ? DifferenceType.Rotation : allowed[UnityEngine.Random.Range(0, allowed.Count)];
    }

    private void PrepareOddDelta()
    {
        switch (currentType)
        {
            case DifferenceType.TintColor:
                {
                    if (oddTintColors != null && oddTintColors.Length > 0)
                    {
                        Color picked = currentBaseColor;
                        int guard = 0;
                        while (picked == currentBaseColor && guard++ < 20)
                            picked = oddTintColors[UnityEngine.Random.Range(0, oddTintColors.Length)];

                        currentOddTintColor = picked;
                    }
                    else
                    {
                        currentOddTintColor = GenerateTintedColor(currentBaseColor);
                    }
                    break;
                }

            case DifferenceType.Sprite:
                {
                    Sprite odd = currentBaseSprite;
                    int guard = 0;
                    while (odd == currentBaseSprite && guard++ < 20)
                        odd = patternSprites[UnityEngine.Random.Range(0, patternSprites.Length)];
                    currentOddSprite = odd;
                    break;
                }

            case DifferenceType.Scale:
                {
                    float m = 1f;
                    int guard = 0;
                    while (Mathf.Abs(m - 1f) < minScaleDeltaFromOne && guard++ < 30)
                        m = UnityEngine.Random.Range(oddScaleMultiplierRange.x, oddScaleMultiplierRange.y);
                    currentOddScaleMul = m;
                    break;
                }

            case DifferenceType.UpsideDown:
                break;

            case DifferenceType.MirrorX:
                break;

            case DifferenceType.Rotation:
            default:
                {
                    float delta = UnityEngine.Random.Range(rotationDeltaRange.x, rotationDeltaRange.y);
                    if (UnityEngine.Random.value < 0.5f) delta = -delta;
                    currentOddRotDelta = delta;
                    break;
                }
        }
    }

    private Color GenerateTintedColor(Color baseColor)
    {
        Color.RGBToHSV(baseColor, out float h, out float s, out float v);

        v = Mathf.Clamp01(v - UnityEngine.Random.Range(0.10f, 0.20f));

        float[] shifts = { -0.06f, -0.03f, 0.03f, 0.06f };
        h = Mathf.Repeat(h + shifts[UnityEngine.Random.Range(0, shifts.Length)], 1f);

        s = Mathf.Clamp01(s + UnityEngine.Random.Range(0.05f, 0.15f));

        return Color.HSVToRGB(h, s, v);
    }

    private void ApplyOddInstant(int index)
    {
        var t = tiles[index];

        switch (currentType)
        {
            case DifferenceType.TintColor:
                t.SetColor(currentOddTintColor);
                break;

            case DifferenceType.Sprite:
                t.SetSprite(currentOddSprite);
                break;

            case DifferenceType.Scale:
                t.SetScale(t.GetBaseScale() * currentOddScaleMul);
                break;

            case DifferenceType.MirrorX:
                {
                    Vector3 s = t.GetBaseScale();
                    s.x = -Mathf.Abs(s.x);
                    t.SetScale(s);
                    break;
                }

            case DifferenceType.UpsideDown:
                t.SetRotation(t.GetBaseRotation() * Quaternion.Euler(0f, 0f, 180f));
                break;

            case DifferenceType.Rotation:
            default:
                {
                    float baseZ = t.GetBaseRotation().eulerAngles.z;
                    t.SetRotationZ(baseZ + currentOddRotDelta);
                    break;
                }
        }
    }

    public void OnCountdownFinishedStartPlaying()
    {
        if (hasEnded) return;

        if (previewRoutine != null)
        {
            StopCoroutine(previewRoutine);
            previewRoutine = null;
        }

        for (int i = 0; i < tiles.Count; i++)
        {
            if (tiles[i] == null) continue;
            tiles[i].ResetVisualToBase(stopCurrentAnim: true);
            tiles[i].ApplyBase(currentBaseSprite, currentBaseColor);
        }

        SetupRound();
        ResumeGameplay();
    }

    private void AnimateToBase(int index)
    {
        if (index < 0 || index >= tiles.Count) return;

        var t = tiles[index];

        if (currentType == DifferenceType.Sprite)
            t.AnimateToSprite(currentBaseSprite, oddSwapAnimDuration);
        else
            t.SetSprite(currentBaseSprite);

        if (currentType == DifferenceType.TintColor)
            t.AnimateToColor(currentBaseColor, oddSwapAnimDuration);
        else
            t.SetColor(currentBaseColor);

        if (currentType == DifferenceType.Scale)
            t.AnimateToScale(t.GetBaseScale(), oddSwapAnimDuration);
        else
            t.SetScale(t.GetBaseScale());

        if (currentType == DifferenceType.Rotation)
        {
            float baseZ = t.GetBaseRotation().eulerAngles.z;
            t.AnimateToRotationZ(baseZ, oddSwapAnimDuration);
        }
        else if (currentType == DifferenceType.MirrorX)
        {
            t.AnimateToScale(t.GetBaseScale(), oddSwapAnimDuration);
        }
        else
        {
            t.SetRotation(t.GetBaseRotation());
        }
    }

    private void AnimateToOdd(int index)
    {
        if (index < 0 || index >= tiles.Count) return;

        var t = tiles[index];

        switch (currentType)
        {
            case DifferenceType.TintColor:
                t.SetColor(currentBaseColor);
                t.AnimateToColor(currentOddTintColor, oddSwapAnimDuration);
                break;

            case DifferenceType.Sprite:
                t.SetSprite(currentBaseSprite);
                t.AnimateToSprite(currentOddSprite, oddSwapAnimDuration);
                break;

            case DifferenceType.Scale:
                t.SetScale(t.GetBaseScale());
                t.AnimateToScale(t.GetBaseScale() * currentOddScaleMul, oddSwapAnimDuration);
                break;

            case DifferenceType.MirrorX:
                {
                    t.SetScale(t.GetBaseScale());
                    Vector3 target = t.GetBaseScale();
                    target.x = -Mathf.Abs(target.x);
                    t.AnimateToScale(target, oddSwapAnimDuration);
                    break;
                }

            case DifferenceType.UpsideDown:
                t.SetRotation(t.GetBaseRotation());
                t.AnimateToRotation(t.GetBaseRotation() * Quaternion.Euler(0f, 0f, 180f), oddSwapAnimDuration);
                break;

            case DifferenceType.Rotation:
            default:
                {
                    float baseZ = t.GetBaseRotation().eulerAngles.z;
                    t.SetRotationZ(baseZ);
                    t.AnimateToRotationZ(baseZ + currentOddRotDelta, oddSwapAnimDuration);
                    break;
                }
        }
    }

    private AudioClip GetRandomCorrectAudio()
    {
        int randomNumber = UnityEngine.Random.Range(0, 10);
        return (randomNumber >= 5) ? correctAudioClip1 : correctAudioClip2;
    }

    private void OnTileClicked(int clickedIndex)
    {
        if (!isRunning) return;
        if (isPausedByOffer) return;
        if (hasEnded) return;
        if (isTransitioning) return;

        if (clickedIndex == oddIndex)
        {
            tiles[clickedIndex].Pop(0.10f, 1.20f);

            score += 1;

            SoundManager.Instance.PlaySound(GetRandomCorrectAudio(), 1f);

            Haptics.TryVibrate();

            if (score == scoreToEnableOddSwap)
                RestartOddSwapRoutine();

            if (useTimer)
                currentTime = Mathf.Min(startTime, currentTime + timeBonusOnCorrect);

            // <-- AÑADIDO: Pasamos "true" para que haga el pop
            UpdateUI(true);
            OnScoreChanged?.Invoke(score);

            if (playRoundTransitionOnCorrect)
            {
                if (transitionRoutine != null) StopCoroutine(transitionRoutine);
                transitionRoutine = StartCoroutine(RoundTransitionRoutine());
            }
            else
            {
                SetupRound();
            }
        }
        else
        {
            if (failOnWrongClick)
            {
                SoundManager.Instance.PlaySound(errorAudioClip, 1f);
                TriggerFail();
            }
        }
    }

    private void RefreshInstructionText()
    {
        if (instructionText == null) return;

        instructionText.text = (currentMode == RoundMode.FindSingleton)
            ? lsInstructionFindSingleton.GetLocalizedString()
            : lsInstructionFindDifferent.GetLocalizedString();
    }

    private IEnumerator RoundTransitionRoutine()
    {
        isTransitioning = true;

        Vector3[] startSpriteScales = new Vector3[tiles.Count];
        for (int i = 0; i < tiles.Count; i++)
        {
            RectTransform imgRt = tiles[i].GetImageRect();
            startSpriteScales[i] = imgRt != null ? imgRt.localScale : Vector3.one;
        }

        float half = Mathf.Max(0.01f, roundTransitionDuration * 0.5f);

        float t = 0f;
        while (t < half)
        {
            if (hasEnded || isPausedByOffer) yield break;

            t += Time.deltaTime;
            float u = Smooth01(t / half);
            float m = Mathf.Lerp(1f, roundTransitionMinScaleMul, u);

            for (int i = 0; i < tiles.Count; i++)
            {
                RectTransform imgRt = tiles[i].GetImageRect();
                if (imgRt == null) continue;
                imgRt.localScale = startSpriteScales[i] * m;
            }

            yield return null;
        }

        for (int i = 0; i < tiles.Count; i++)
        {
            RectTransform imgRt = tiles[i].GetImageRect();
            if (imgRt == null) continue;
            imgRt.localScale = startSpriteScales[i] * roundTransitionMinScaleMul;
        }

        SetupRound();

        Vector3[] targetSpriteScales = new Vector3[tiles.Count];
        for (int i = 0; i < tiles.Count; i++)
        {
            RectTransform imgRt = tiles[i].GetImageRect();
            if (imgRt == null)
            {
                targetSpriteScales[i] = Vector3.one;
                continue;
            }

            targetSpriteScales[i] = imgRt.localScale;
            imgRt.localScale = targetSpriteScales[i] * roundTransitionMinScaleMul;
        }

        t = 0f;
        while (t < half)
        {
            if (hasEnded || isPausedByOffer) yield break;

            t += Time.deltaTime;
            float u = Smooth01(t / half);
            float m = Mathf.Lerp(roundTransitionMinScaleMul, 1f, u);

            for (int i = 0; i < tiles.Count; i++)
            {
                RectTransform imgRt = tiles[i].GetImageRect();
                if (imgRt == null) continue;
                imgRt.localScale = targetSpriteScales[i] * m;
            }

            yield return null;
        }

        for (int i = 0; i < tiles.Count; i++)
        {
            RectTransform imgRt = tiles[i].GetImageRect();
            if (imgRt == null) continue;
            imgRt.localScale = targetSpriteScales[i];
        }

        isTransitioning = false;
        transitionRoutine = null;
    }

    private float Smooth01(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    private IEnumerator FailRevealThenTriggerFailRoutine(int revealIndex)
    {
        float prevTimeScale = Time.timeScale;
        float prevFixedDelta = Time.fixedDeltaTime;

        Time.timeScale = Mathf.Clamp(failRevealTimeScale, 0.01f, 1f);
        Time.fixedDeltaTime = prevFixedDelta * Time.timeScale;

        isTransitioning = true;

        DifferentTile t = (revealIndex >= 0 && revealIndex < tiles.Count) ? tiles[revealIndex] : null;
        if (t != null)
        {
            yield return t.PlayFailReveal(
                duration: failRevealDuration,
                scaleMul: failRevealScaleMul,
                blinkColor: failRevealBlinkColor,
                blinks: failRevealBlinks
            );
        }
        else
        {
            float wait = failRevealDuration;
            while (wait > 0f) { wait -= Time.unscaledDeltaTime; yield return null; }
        }

        Time.timeScale = prevTimeScale;
        Time.fixedDeltaTime = prevFixedDelta;

        isTransitioning = false;

        TriggerFail();
    }

    // =========================
    // =========================
    private void TriggerFail()
    {
        if (hasEnded) return; // hasEnded se marca en Finish/TriggerFail flow
        Finish();
    }

    private void Finish()
    {
        if (hasEnded) return;

        // Cortar gameplay ya
        isRunning = false;
        hasEnded = true;

        if (oddSwapRoutine != null) { StopCoroutine(oddSwapRoutine); oddSwapRoutine = null; }
        if (transitionRoutine != null) { StopCoroutine(transitionRoutine); transitionRoutine = null; }
        if (countdownRoutine != null) { StopCoroutine(countdownRoutine); countdownRoutine = null; }
        if (previewRoutine != null) { StopCoroutine(previewRoutine); previewRoutine = null; }

        // Si quieres reveal, lo hacemos y al terminar notificamos al flow
        if (playFailReveal && oddIndex >= 0 && oddIndex < tiles.Count && tiles[oddIndex] != null)
        {
            StartCoroutine(FailRevealThenNotifyFlowRoutine(oddIndex));
        }
        else
        {
            NotifyFlowFail();
        }
    }

    private IEnumerator FailRevealThenNotifyFlowRoutine(int revealIndex)
    {
        // Reveal
        yield return FailRevealThenTriggerFailRoutine(revealIndex);

        // OJO: FailRevealThenTriggerFailRoutine ya llama TriggerFail() -> Finish()
        // pero Finish() ya está hecho, así que aquí solo aseguramos flow:
        NotifyFlowFail();
    }

    private void NotifyFlowFail()
    {
        if (gameOverInvoked) return; // evita doble notify
        gameOverInvoked = true;

        if (GameOverFlowManager.Instance != null)
        {
            GameOverFlowManager.Instance.NotifyFail(this);
        }
        else
        {
            // fallback: final directo
            FinalGameOver();
        }
    }

    // =========================
    // IGameOverClient (NEW)
    // =========================
    public void PauseOnFail()
    {
        isPausedByOffer = true;
        PauseGameplay();

        // IMPORTANTE: no tocar timeScale aquí. Tu reveal ya lo tocó y lo restauró.
    }

    public void Revive()
    {
        // Reseteo estado para seguir jugando
        isPausedByOffer = false;
        hasEnded = false;
        gameOverInvoked = false;

        // Reanudar timer a tope (como otros modos)
        currentTime = startTime;
        if (timeBarImage != null) timeBarImage.fillAmount = 1f;

        // Reponemos ronda limpia (importante si estabas en reveal/preview)
        SetupRound();
        RefreshInstructionText();

        ResumeGameplay();
    }

    public void FinalGameOver()
    {
        // GameOver real
        EndGame();
    }

    // =========================
    // GAME OVER REAL (sin offer)
    // =========================
    private void EndGame()
    {
        OnDifferentGameOver?.Invoke(this, EventArgs.Empty);

        SaveRecordIfNeeded();

        if (PlayFabLoginManager.Instance != null &&
            PlayFabLoginManager.Instance.IsLoggedIn &&
            PlayFabScoreManager.Instance != null)
        {
            PlayFabScoreManager.Instance.SubmitScore(playFabStatName, score);
        }

        int coinsEarned = Mathf.Max(0, score / Mathf.Max(1, coinsDivisor));

        CoinsRewardUI rewardUI = FindObjectOfType<CoinsRewardUI>(true);
        if (rewardUI != null)
            rewardUI.ShowReward(coinsEarned);
        else if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.AddCoins(coinsEarned);

        if (PlayerLevelManager.Instance != null)
            PlayerLevelManager.Instance.AddXP(score * Mathf.Max(0, xpPerPoint));

        if (DailyMissionManager.Instance != null)
        {
            DailyMissionManager.Instance.AddProgress("juega_1_partida", 1);
            DailyMissionManager.Instance.AddProgress("juega_3_partidas", 1);
            DailyMissionManager.Instance.AddProgress("juega_8_partidas", 1);
            DailyMissionManager.Instance.AddProgress("juega_10_partidas", 1);

            if (score >= 10) DailyMissionManager.Instance.AddProgress("consigue_10_puntos_diferente", 1);
            if (score >= 50) DailyMissionManager.Instance.AddProgress("consigue_50_puntos_diferente", 1);
        }

#if UNITY_ANDROID || UNITY_IOS
        Haptics.TryVibrate();
#endif
    }

    private void SaveRecordIfNeeded()
    {
        int currentRecord = PlayerPrefs.GetInt(recordPlayerPrefsKey, 0);
        if (score > currentRecord)
        {
            PlayerPrefs.SetInt(recordPlayerPrefsKey, score);
            PlayerPrefs.Save();
        }
    }

    // <-- MÉTODO ACTUALIZADO: Recibe un parámetro booleano y hace la animación LeanTween
    private void UpdateUI(bool animate = false)
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();

            if (animate && score > 0)
            {
                LeanTween.cancel(scoreText.gameObject);

                // 1. Efecto de Escala (Pop) basado en tu escala real
                scoreText.transform.localScale = originalScoreScale;
                LeanTween.scale(scoreText.gameObject, originalScoreScale * 1.4f, 0.2f)
                    .setEase(LeanTweenType.easeOutBack)
                    .setLoopPingPong(1);

                // 2. Efecto de Color (Compatible con TextMeshPro)
                scoreText.color = scoreHighlightColor;

                LeanTween.value(scoreText.gameObject, scoreHighlightColor, Color.white, 0.4f)
                    .setEase(LeanTweenType.easeOutQuad)
                    .setOnUpdate((Color colorAnimado) =>
                    {
                        scoreText.color = colorAnimado;
                    });
            }
        }

        if (timeBarImage != null)
        {
            if (useTimer)
                timeBarImage.fillAmount = Mathf.Clamp01(currentTime / startTime);
            else
                timeBarImage.fillAmount = 1f;
        }
    }

    public int GetScore() => score;
    public bool IsRunning() => isRunning;
}