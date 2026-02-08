// GeometricModeManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class GeometricModeManager : MonoBehaviour, IGameOverClient
{
    public static GeometricModeManager Instance { get; private set; }

    // =========================
    // ADS / GameOverFlow (NEW)
    // =========================
    public bool HasUsedReviveOffer { get; set; } = false;
    private bool isPausedByOffer = false;

    public void PauseOnFail()
    {
        isPausedByOffer = true;
        FreezeActiveShapes(true);
    }

    public void Revive()
    {
        // Reanudar gameplay y restaurar tiempo al máximo posible en ese momento.
        // En este modo, lo más limpio es devolverlo al startTime actual (que baja con la dificultad).
        isPausedByOffer = false;
        hasEnded = false;

        currentTime = startTime;

        if (timeBarImage != null)
            timeBarImage.fillAmount = 1f;

        FreezeActiveShapes(false);
    }

    public void FinalGameOver()
    {
        StartCoroutine(SlowMotionAndEnd());
    }

    private void TriggerFail()
    {
        // Evita dobles entradas
        if (hasEnded || gameOverInvoked) return;

        hasEnded = true;

        if (GameOverFlowManager.Instance != null)
            GameOverFlowManager.Instance.NotifyFail(this);
        else
            FinalGameOver();
    }

    [Header("UI Elements")]
    public TextMeshProUGUI instructionText;
    public Image instructionIcon;
    public TextMeshProUGUI scoreText;
    public Image timeBarImage;
    public float startTime = 60f;

    [Header("Game Settings")]
    public float speedMultiplier = 1f;

    [Header("Difficulty (Option 1)")]
    [SerializeField] private float baseSpeedMult = 1f;
    [SerializeField] private float maxSpeedMult = 2.2f;
    [SerializeField] private int scoreToReachMaxSpeed = 60;

    [Header("Shapes")]
    public List<BouncingShape> shapes;

    [Header("Localization")]
    public LocalizedString scoreLabel;
    public LocalizedString tapShapeInstruction;

    private float currentTime;
    private int score = 0;
    private BouncingShape currentTarget;
    private bool hasEnded = false;
    private bool gameOverInvoked = false;
    private bool hasGameStarted = false;

    public event EventHandler OnGameOver;

    [Header("Intro / Countdown")]
    [SerializeField] private LocalizedString prepareInstruction;
    [SerializeField] private Sprite prepareIcon;
    [SerializeField] private float introMoveDuration = 0.45f;
    [SerializeField] private float introStagger = 0.08f;
    [SerializeField] private float introOffscreenPadding = 1.2f;
    [SerializeField] private float introWobbleDuration = 0.16f;

    [Header("Sounds")]
    [SerializeField] private AudioClip correctHitAudioClip;
    [SerializeField] private AudioClip incorrectHitAudioClip;
    [SerializeField] private AudioClip playerAudioClip;

    private bool introAnimDone = false;
    private bool movementStarted = false;

    private Coroutine playerSoundCoroutine;

    // -------------------------
    // Tutorial (global toggle)
    // -------------------------
    [Header("Tutorial Panel")]
    [SerializeField] private TutorialPanelUI tutorialPrefab;
    [SerializeField] private Transform tutorialParent;
    private TutorialPanelUI tutorialInstance;
    private const string ShowTutorialKey = "ShowTutorialOnStart";

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        playerSoundCoroutine = StartCoroutine(LoopCoroutine());
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;

        if (playerSoundCoroutine != null)
        {
            StopCoroutine(playerSoundCoroutine);
            playerSoundCoroutine = null;
        }
    }

    private void Start()
    {
        // Aseguramos siempre tiempo normal al entrar
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

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

        if (TransitionScript.Instance != null)
        {
            TransitionScript.Instance.OnTransitionOutFinished += HandleTransitionFinished;
        }
    }

    private void OnDestroy()
    {
        if (TransitionScript.Instance != null)
            TransitionScript.Instance.OnTransitionOutFinished -= HandleTransitionFinished;
    }

    // ==================
    // Tutorial flow
    // ==================
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

        PauseGameplay(); // evita input + timer + arranque movimiento
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
        StartGame(); // aquí dejas TODO como estaba: intro + countdown state
    }

    // ==================
    // Transition hook
    // ==================
    private void HandleTransitionFinished()
    {
        // Si tu transición dispara “segundo StartGame”, lo evitamos con hasGameStarted
        StartCoroutine(StartGameDelayed());
    }

    private IEnumerator StartGameDelayed()
    {
        yield return null;
        StartGame();
    }

    // ==================
    // Pause/Resume
    // ==================
    public void PauseGameplay()
    {
        // No gastamos tiempo ni arrancamos movimiento
        movementStarted = false;

        // Deja shapes congeladas si hay alguna activa
        FreezeActiveShapes(true);
    }

    // ==================
    // Sounds loop
    // ==================
    private IEnumerator LoopCoroutine()
    {
        while (true)
        {
            PlayerSounds();
            yield return new WaitForSeconds(GetRandomCoroutineWait());
        }
    }

    private int GetRandomCoroutineWait()
    {
        return UnityEngine.Random.Range(4, 8);
    }

    private void PlayerSounds()
    {
        SoundManager.Instance.PlaySound(playerAudioClip, 1f);
    }

    // ==================
    // Main start (intro kept)
    // ==================
    private void StartGame()
    {
        if (hasGameStarted) return;
        hasGameStarted = true;

        // ADS reset (NEW, minimal)
        HasUsedReviveOffer = false;
        isPausedByOffer = false;

        hasEnded = false;
        gameOverInvoked = false;
        score = 0;

        UpdateDifficulty();
        currentTime = startTime;

        if (timeBarImage != null)
            timeBarImage.fillAmount = 1f;

        if (scoreEffectTargets != null && scoreEffectTargets.Length > 0)
        {
            originalScales = new Vector3[scoreEffectTargets.Length];
            for (int i = 0; i < scoreEffectTargets.Length; i++)
            {
                if (scoreEffectTargets[i] == null) continue;
                originalScales[i] = scoreEffectTargets[i].localScale;
            }
        }

        UpdateScoreText();

        foreach (var s in shapes.FindAll(x => x.gameObject.activeSelf))
            StopShapeParticles(s, clear: true);

        // Activa solo las primeras 3 figuras al iniciar
        for (int i = 0; i < shapes.Count; i++)
            shapes[i].gameObject.SetActive(i < 3);

        // Intro state
        currentTarget = null;
        introAnimDone = false;
        movementStarted = false;

        SetPrepareUI();

        // Congelamos shapes hasta que se cumpla: introAnimDone && state.Playing
        FreezeActiveShapes(true);

        // Arranca countdown (GeometricState) SOLO aquí
        if (GeometricState.Instance != null)
            GeometricState.Instance.StartCountdown();

        // Arranca intro visual (spawn + wobble)
        StartCoroutine(IntroSpawnRoutine());
    }

    private void Update()
    {
        if (hasEnded) return;
        if (isPausedByOffer) return; // NEW: pausa durante offer

        // Espera a terminar la intro + que el estado sea Playing para arrancar el movimiento
        if (!movementStarted)
        {
            TryStartMovementAfterIntro();
            return;
        }

        if (GeometricState.Instance != null &&
            GeometricState.Instance.geometricGameState != GeometricState.GeometricGameStateEnum.Playing)
            return;

        currentTime -= Time.deltaTime;

        if (timeBarImage != null)
            timeBarImage.fillAmount = Mathf.Clamp01(currentTime / startTime);

        if (currentTime <= 0f)
            TriggerFail(); // CHANGED: antes SlowMotionAndEnd
    }

    private void SetPrepareUI()
    {
        if (instructionText != null)
        {
            string t = prepareInstruction.IsEmpty ? "Prepárate..." : prepareInstruction.GetLocalizedString();
            instructionText.text = t;
        }

        SetInstructionIcon(prepareIcon);
    }

    private void TryStartMovementAfterIntro()
    {
        bool isPlaying =
            GeometricState.Instance == null ||
            GeometricState.Instance.geometricGameState == GeometricState.GeometricGameStateEnum.Playing;

        if (!introAnimDone || !isPlaying) return;

        movementStarted = true;

        FreezeActiveShapes(false);
        foreach (var s in shapes.FindAll(x => x.gameObject.activeSelf))
            PlayShapeParticles(s);

        var activeShapes = shapes.FindAll(s => s.gameObject.activeSelf);
        foreach (var s in activeShapes)
        {
            s.RandomizeDirection();
            s.UpdateSpeed(speedMultiplier);

            var tr = s.GetComponentInChildren<TrailRenderer>(true);
            if (tr) tr.Clear();
        }

        ChooseNewTarget();
    }

    private void SetInstructionIcon(Sprite s)
    {
        if (instructionIcon == null) return;
        instructionIcon.sprite = s;
        instructionIcon.enabled = (s != null);
    }

    public void OnShapeTapped(BouncingShape shape)
    {
        if (hasEnded) return;
        if (isPausedByOffer) return; // NEW: bloquea input durante offer

        if (GeometricState.Instance != null &&
            GeometricState.Instance.geometricGameState != GeometricState.GeometricGameStateEnum.Playing)
            return;

        if (!movementStarted) return;

        shape.PlayTapSquish();
        shape.GetComponent<JellyFXTrailSparkles>()?.BurstTapWide();

        if (shape == currentTarget)
        {
            Animator anim = shape.GetComponent<Animator>();
            if (anim != null)
                anim.SetTrigger("isHitAnim");

            SoundManager.Instance.PlaySound(correctHitAudioClip, 1f);

            AddScore();

            shape.PlayScaredSprite();

            startTime = Mathf.Max(3.0f, startTime - 0.04f);
            currentTime = startTime;

            CheckForAdditionalShapes();
            ReverseAllActiveShapesDirections();
            ChooseNewTarget();
        }
        else
        {
            SoundManager.Instance.PlaySound(incorrectHitAudioClip, 1f);
            TriggerFail(); // CHANGED: antes SlowMotionAndEnd
        }
    }

    private IEnumerator IntroSpawnRoutine()
    {
        List<BouncingShape> active = shapes.FindAll(s => s.gameObject.activeSelf);
        int count = Mathf.Min(3, active.Count);
        if (count == 0) { introAnimDone = true; yield break; }

        Camera cam = Camera.main;

        Vector3[] endPos = new Vector3[count];
        for (int i = 0; i < count; i++) endPos[i] = active[i].transform.position;

        Vector3 GetOffscreenFrom(Vector3 target, int slot)
        {
            if (!cam) return target;

            float dist = Mathf.Abs(target.z - cam.transform.position.z);
            Vector3 left = cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, dist));
            Vector3 right = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, dist));
            Vector3 bot = cam.ViewportToWorldPoint(new Vector3(0.5f, 0f, dist));

            if (slot == 0) return new Vector3(left.x - introOffscreenPadding, target.y, target.z);
            if (slot == 1) return new Vector3(right.x + introOffscreenPadding, target.y, target.z);
            return new Vector3(target.x, bot.y - introOffscreenPadding, target.z);
        }

        for (int i = 0; i < count; i++)
        {
            active[i].transform.position = GetOffscreenFrom(endPos[i], i);
            var tr = active[i].GetComponentInChildren<TrailRenderer>(true);
            if (tr) tr.Clear();
            var ps = active[i].GetComponentInChildren<ParticleSystem>(true);
            if (ps) ps.Clear();
        }

        for (int i = 0; i < count; i++)
        {
            PlayShapeParticles(active[i]);
            StartCoroutine(MoveAndWobble(active[i], active[i].transform, endPos[i], introMoveDuration, introWobbleDuration));
            yield return new WaitForSeconds(introStagger);
        }

        yield return new WaitForSeconds(introMoveDuration + introWobbleDuration);
        introAnimDone = true;
    }

    private IEnumerator MoveAndWobble(BouncingShape owner, Transform root, Vector3 end, float moveDur, float wobbleDur)
    {
        Vector3 start = root.position;

        float t = 0f;
        while (t < moveDur)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / moveDur);
            u = u * u * (3f - 2f * u);
            root.position = Vector3.LerpUnclamped(start, end, u);
            yield return null;
        }
        root.position = end;

        Transform vis = root.Find("Visual");
        if (!vis) vis = root;

        Vector3 baseScale = vis.localScale;
        yield return Wobble(vis, baseScale, wobbleDur);
        vis.localScale = baseScale;

        StopShapeParticles(owner, clear: false);
    }

    private IEnumerator Wobble(Transform t, Vector3 baseScale, float dur)
    {
        float part = dur / 4f;
        yield return ScaleTo(t, baseScale, new Vector3(baseScale.x * 1.25f, baseScale.y * 0.80f, baseScale.z), part);
        yield return ScaleTo(t, t.localScale, new Vector3(baseScale.x * 0.90f, baseScale.y * 1.10f, baseScale.z), part);
        yield return ScaleTo(t, t.localScale, new Vector3(baseScale.x * 1.05f, baseScale.y * 0.95f, baseScale.z), part);
        yield return ScaleTo(t, t.localScale, baseScale, part);
    }

    private IEnumerator ScaleTo(Transform t, Vector3 a, Vector3 b, float dur)
    {
        float time = 0f;
        while (time < dur)
        {
            time += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(time / dur);
            u = u * u * (3f - 2f * u);
            t.localScale = Vector3.LerpUnclamped(a, b, u);
            yield return null;
        }
        t.localScale = b;
    }

    private void FreezeActiveShapes(bool freeze)
    {
        var active = shapes.FindAll(s => s.gameObject.activeSelf);

        foreach (var s in active)
        {
            var rb = s.GetComponent<Rigidbody2D>();
            if (rb)
            {
                if (freeze)
                {
                    rb.simulated = false;
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
                else
                {
                    rb.simulated = true;
                }
            }

            var pss = s.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in pss)
            {
                if (!ps) continue;

                if (freeze) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                else ps.Play(true);
            }
        }
    }

    private void PlayShapeParticles(BouncingShape s)
    {
        if (!s) return;
        var pss = s.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in pss) ps.Play(true);
    }

    private void StopShapeParticles(BouncingShape s, bool clear = true)
    {
        if (!s) return;
        var pss = s.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in pss)
        {
            if (clear) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            else ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void AddScore()
    {
        score++;
        UpdateDifficulty();
        UpdateScoreText();
        Haptics.TryVibrate();
        PlayerLevelManager.Instance.AddXP(15);
        PlayScoreEffect();

        if (FallingShapesManager.Instance != null)
            FallingShapesManager.Instance.SpawnFallingShapes();
    }

    [SerializeField] private RectTransform[] scoreEffectTargets;
    private bool isAnimating = false;
    private Vector3[] originalScales;

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
        float mult = 1.2f;

        if (scoreEffectTargets == null || scoreEffectTargets.Length == 0)
        {
            isAnimating = false;
            yield break;
        }

        float time = 0;
        while (time < halfDuration)
        {
            float tt = time / halfDuration;

            for (int i = 0; i < scoreEffectTargets.Length; i++)
            {
                var rt = scoreEffectTargets[i];
                if (rt == null) continue;

                Vector3 a = (originalScales != null && i < originalScales.Length) ? originalScales[i] : rt.localScale;
                Vector3 b = a * mult;
                rt.localScale = Vector3.Lerp(a, b, tt);
            }

            time += Time.deltaTime;
            yield return null;
        }

        time = 0;
        while (time < halfDuration)
        {
            float tt = time / halfDuration;

            for (int i = 0; i < scoreEffectTargets.Length; i++)
            {
                var rt = scoreEffectTargets[i];
                if (rt == null) continue;

                Vector3 a = (originalScales != null && i < originalScales.Length) ? originalScales[i] : rt.localScale;
                Vector3 b = a * mult;
                rt.localScale = Vector3.Lerp(b, a, tt);
            }

            time += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < scoreEffectTargets.Length; i++)
        {
            var rt = scoreEffectTargets[i];
            if (rt == null) continue;

            if (originalScales != null && i < originalScales.Length)
                rt.localScale = originalScales[i];
        }

        isAnimating = false;
    }

    private void UpdateScoreText()
    {
        scoreText.text = scoreLabel.GetLocalizedString(score);
    }

    public int GetScore() => score;

    private void UpdateDifficulty()
    {
        float t = Mathf.Clamp01(score / (float)scoreToReachMaxSpeed);
        t = t * t * (3f - 2f * t);

        speedMultiplier = Mathf.Lerp(baseSpeedMult, maxSpeedMult, t);
        UpdateShapesSpeed();
    }

    private void ChooseNewTarget()
    {
        List<BouncingShape> activeShapes = shapes.FindAll(s => s.gameObject.activeSelf);

        if (activeShapes.Count == 0)
        {
            currentTarget = null;
            instructionText.text = "";
            SetInstructionIcon(null);
            return;
        }

        if (currentTarget == null)
        {
            currentTarget = activeShapes[UnityEngine.Random.Range(0, activeShapes.Count)];
        }
        else if (activeShapes.Count > 1)
        {
            BouncingShape newTarget;
            do
            {
                newTarget = activeShapes[UnityEngine.Random.Range(0, activeShapes.Count)];
            } while (newTarget == currentTarget);

            currentTarget = newTarget;
        }

        string shapeNameText = currentTarget.shapeName.GetLocalizedString();
        instructionText.text = tapShapeInstruction.GetLocalizedString(shapeNameText);
        SetInstructionIcon(currentTarget.GetUIIcon());
    }

    private void UpdateShapesSpeed()
    {
        foreach (BouncingShape s in shapes)
            s.UpdateSpeed(speedMultiplier);
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

    private IEnumerator SlowMotionAndEnd()
    {
        if (gameOverInvoked) yield break;  
        hasEnded = true;

        float previousTimeScale = Time.timeScale;
        float previousFixedDelta = Time.fixedDeltaTime;

        Time.timeScale = 0.3f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        EndGame();

        yield return new WaitForSecondsRealtime(1.5f);

        Time.timeScale = previousTimeScale;
        Time.fixedDeltaTime = previousFixedDelta;
    }

    private void ReverseAllActiveShapesDirections()
    {
        var activeShapes = shapes.FindAll(s => s.gameObject.activeSelf);
        foreach (var s in activeShapes)
            s.ReverseDirection();
    }

    private void EndGame()
    {
        if (gameOverInvoked) return;
        gameOverInvoked = true;

        if (GeometricState.Instance != null)
            GeometricState.Instance.geometricGameState = GeometricState.GeometricGameStateEnum.GameOver;

        OnGameOver?.Invoke(this, EventArgs.Empty);
        SaveRecordIfNeeded();
        Debug.Log("Engame CALLED");

        if (PlayFabLoginManager.Instance != null && PlayFabLoginManager.Instance.IsLoggedIn)
            PlayFabScoreManager.Instance.SubmitScore("GeometricScore", score);

        int coinsEarned = score / 3;

        CoinsRewardUI rewardUI = FindObjectOfType<CoinsRewardUI>(true);
        if (rewardUI != null) rewardUI.ShowReward(coinsEarned);
        else CurrencyManager.Instance.AddCoins(coinsEarned);

        if (DailyMissionManager.Instance != null)
        {
            DailyMissionManager.Instance.AddProgress("juega_1_partida", 1);
            DailyMissionManager.Instance.AddProgress("juega_3_partida", 1);
            DailyMissionManager.Instance.AddProgress("juega_8_partida", 1);
            DailyMissionManager.Instance.AddProgress("juega_10_partida", 1);

            if (score >= 10) DailyMissionManager.Instance.AddProgress("consigue_10_puntos_geométrico", 1);
            if (score >= 50) DailyMissionManager.Instance.AddProgress("consigue_50_puntos_geométrico", 1);

            DailyMissionManager.Instance.AddProgress("juega_5_partidas_geométrico", 1);
        }
    }

    private void OnLocaleChanged(Locale locale)
    {
        UpdateScoreText();

        if (!movementStarted)
        {
            SetPrepareUI();
            return;
        }

        if (currentTarget != null)
        {
            string shapeNameText = currentTarget.shapeName.GetLocalizedString();
            instructionText.text = tapShapeInstruction.GetLocalizedString(shapeNameText);
            SetInstructionIcon(currentTarget.GetUIIcon());
        }
    }
}
