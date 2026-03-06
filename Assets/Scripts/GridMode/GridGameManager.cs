using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GridGameManager : MonoBehaviour, IGameOverClient
{
    public static GridGameManager Instance { get; private set; }

    public event EventHandler OnGridGameOver;
    public event EventHandler OnGameOver;

    [Header("Grid")]
    public Transform gridParent;
    public int gridSize = 4;

    [Header("Prefabs")]
    public GameObject playerPrefab;
    public GameObject warningPrefab;

    [Header("Arrow Settings")]
    public float arrowSpeed = 8f;
    [SerializeField] private GameObject[] arrowPrefabs;

    [Header("Coins")]
    [SerializeField] private GameObject[] coinPrefabs;

    [Header("Gameplay")]
    public float warningTime = 1f;
    public float rowInterval = 2f;
    public float moveDuration = 1f;
    public float coinTimeLimit = 5f;
    public float multiArrowDelay = 0.25f;

    [Header("UI Buttons")]
    public Button upButton;
    public Button downButton;
    public Button leftButton;
    public Button rightButton;

    [Header("Timer Colors")]
    public Color fullColor = Color.green;
    public Color midColor = Color.yellow;
    public Color lowColor = Color.red;

    [Header("UI Timer")]
    public Image coinTimerImage;

    [Header("AudioClips")]
    [SerializeField] private AudioClip jumpAudioClip;
    [SerializeField] private AudioClip pickUpAudioClip;
    [SerializeField] private AudioClip arrowAudioClip;
    [SerializeField] private AudioClip deathAudioClip;

    private int playerX, playerY;
    private GameObject playerObj;
    private GameObject coinObj;
    private Transform[,] gridCells;

    private int score = 0;
    private bool isGameOver = false;
    private bool isMoving = false;

    private bool isHoldingButton = false;
    private int holdDx = 0;
    private int holdDy = 0;

    private float coinTimer;
    private Vector3 originalScale;

    private const float minWarningTime = 0.5f;
    private const float minCoinTime = 7f;
    private const float decreaseAmount = 0.05f;

    [Header("Hit Settings")]
    [SerializeField] private float cellHitRadius = 0.4f;

    [Header("UI Score")]
    [SerializeField] private TextMeshProUGUI scoreText;

    private bool isDyingByArrow = false;

    // =========================
    // ADS / GameOverFlow (NEW)
    // =========================
    public bool HasUsedReviveOffer { get; set; } = false;
    private bool isPausedByOffer = false;
    private bool gameOverInvoked = false; // evita doble final

    // Squash
    public enum Axis { Y_DOWN, Y_UP }
    private Dictionary<Transform, Coroutine> platformSquashRoutines = new Dictionary<Transform, Coroutine>();
    private Vector3 cellBaseScale = new Vector3(0.59f, 1.01773f, 0.59f);
    [SerializeField] private float playerCellScaleMultiplier = 1.28f;

    private GridPlayerVisual playerVisual;

    [Header("Positions")]
    [SerializeField] private Vector3 playerCellOffset = new Vector3(0f, 0.2f, 0f);
    [SerializeField] private Vector3 coinCellOffset = new Vector3(0f, 0.2f, 0f);

    [Header("Arrow Warning")]
    [SerializeField] private Color warningCellColor = Color.red;
    [SerializeField] private Color defaultCellColor = Color.white;

    [SerializeField] private GridGemUI gemUI;

    [Header("FX")]
    [SerializeField] private GemSpawnFX gemSpawnFxPrefab;
    [SerializeField] private Color blueGemColor = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color greenGemColor = new Color(0.3f, 0.9f, 0.3f);
    [SerializeField] private Color purpleGemColor = new Color(0.8f, 0.2f, 0.9f);

    [Header("Intro Drop")]
    [SerializeField] private float introDropHeight = 3f;
    [SerializeField] private float introDropDuration = 0.8f;

    [Header("Tutorial Panel")]
    [SerializeField] private bool showTutorialOnStart = true;
    [SerializeField] private TutorialPanelUI tutorialPrefab;
    [SerializeField] private Transform tutorialParent;

    private const string ShowTutorialKey = "ShowTutorialOnStart";
    private TutorialPanelUI tutorialInstance;

    private bool hasStarted = false;

    private Color GetGemColorFromPrefab(GameObject prefab)
    {
        if (prefab.name.Contains("GemaAzul")) return blueGemColor;
        if (prefab.name.Contains("GemaVerde")) return greenGemColor;
        if (prefab.name.Contains("GemaMorada")) return purpleGemColor;
        return Color.white;
    }

    private void Awake()
    {
        Instance = this;

        showTutorialOnStart = PlayerPrefs.GetInt(ShowTutorialKey, 1) == 1;

        gridCells = new Transform[gridSize, gridSize];
        int index = 0;
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
                gridCells[x, y] = gridParent.GetChild(index++);
        }

        ApplyBaseScaleToAllCells();
    }

    private void Start()
    {
        // SUSTITUIMOS LOS onClick POR NUESTRO NUEVO SISTEMA HOLD
        AddHoldEvent(upButton, 0, -1);
        AddHoldEvent(downButton, 0, 1);
        AddHoldEvent(leftButton, -1, 0);
        AddHoldEvent(rightButton, 1, 0);

        coinTimerImage.fillAmount = 1f;
        coinTimerImage.color = fullColor;

        UpdateScoreText();

        if (showTutorialOnStart && tutorialPrefab != null) ShowTutorial();
        else { HideAnyExistingTutorialPanel(); BeginGameAfterTutorial(); }
    }

    // --- MÉTODO NUEVO ---
    private void AddHoldEvent(Button btn, int dx, int dy)
    {
        // Le añadimos un EventTrigger al botón por código
        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

        // Evento al PULSAR el dedo (PointerDown)
        EventTrigger.Entry pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) => {
            isHoldingButton = true;
            holdDx = dx;
            holdDy = dy;
        });
        trigger.triggers.Add(pointerDown);

        // Evento al LEVANTAR el dedo (PointerUp)
        EventTrigger.Entry pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((data) => {
            isHoldingButton = false;
        });
        trigger.triggers.Add(pointerUp);
    }

    private void HideAnyExistingTutorialPanel()
    {
        var existing = FindObjectOfType<TutorialPanelUI>(true);
        if (existing != null) existing.gameObject.SetActive(false);
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

    private void BeginGameAfterTutorial()
    {
        StartGame();
    }

    public void StartGame()
    {
        if (hasStarted) return;
        hasStarted = true;

        // ADS reset
        HasUsedReviveOffer = false;
        isPausedByOffer = false;
        gameOverInvoked = false;

        score = 0;
        isGameOver = false;
        isDyingByArrow = false;
        isMoving = false;

        coinObj = null;
        coinTimer = coinTimeLimit;
        coinTimerImage.fillAmount = 1f;
        coinTimerImage.color = fullColor;

        UpdateScoreText();

        if (GridState.Instance != null)
            GridState.Instance.StartCountdown();
        else
        {
            StartIntroDropDuringCountdown();
            StartGameplayAfterCountdown();
        }
    }

    public void PauseGameplay()
    {
        // no hace falta nada; tu Update ya depende de GridState.Playing
    }

    public void StartIntroDropDuringCountdown()
    {
        if (gridCells == null || gridCells.Length == 0) return;
        if (playerPrefab == null) return;

        playerX = 0;
        playerY = 0;

        Transform firstCell = gridCells[playerX, playerY];
        if (firstCell == null) return;

        Vector3 cellPos = firstCell.position + playerCellOffset;
        Vector3 spawnFrom = cellPos + Vector3.up * introDropHeight;

        playerObj = Instantiate(playerPrefab, spawnFrom, Quaternion.identity, gridParent);
        originalScale = playerObj.transform.localScale;

        playerVisual = playerObj.GetComponent<GridPlayerVisual>();
        if (playerVisual == null)
            playerVisual = playerObj.GetComponentInChildren<GridPlayerVisual>(true);

        playerVisual?.SetInAir();
        StartCoroutine(IntroDropRoutine(cellPos));
    }

    private IEnumerator IntroDropRoutine(Vector3 targetPos)
    {
        float elapsed = 0f;
        Vector3 startPos = playerObj.transform.position;

        while (elapsed < introDropDuration)
        {
            float t = elapsed / introDropDuration;
            float eased = t * t * (3f - 2f * t);
            playerObj.transform.position = Vector3.Lerp(startPos, targetPos, eased);

            elapsed += Time.deltaTime;
            yield return null;
        }

        playerObj.transform.position = targetPos;
        playerVisual?.SetOnCell();
        ForceCellScale(gridCells[playerX, playerY]);
    }

    private Vector3 GetRestScaleForCell(Transform cell)
    {
        if (cell == null) return cellBaseScale;

        Transform playerCell = null;
        if (gridCells != null &&
            playerX >= 0 && playerX < gridSize &&
            playerY >= 0 && playerY < gridSize)
        {
            playerCell = gridCells[playerX, playerY];
        }

        return (cell == playerCell) ? (cellBaseScale * playerCellScaleMultiplier) : cellBaseScale;
    }

    private void ApplyBaseScaleToAllCells()
    {
        for (int y = 0; y < gridSize; y++)
            for (int x = 0; x < gridSize; x++)
                if (gridCells[x, y] != null)
                    gridCells[x, y].localScale = cellBaseScale;
    }

    private void ForceCellScale(Transform cell)
    {
        if (cell == null) return;

        if (platformSquashRoutines.TryGetValue(cell, out var running) && running != null)
        {
            StopCoroutine(running);
            platformSquashRoutines[cell] = null;
        }

        cell.localScale = GetRestScaleForCell(cell);
    }

    public void StartGameplayAfterCountdown()
    {
        SpawnCoin();
        StartCoroutine(ArrowRoutine());

        coinTimer = coinTimeLimit;
        coinTimerImage.fillAmount = 1f;
        coinTimerImage.color = fullColor;
    }

    private void Update()
    {
        if (isPausedByOffer) return;

        // Modificamos esta línea para apagar el hold si nos matan
        if (isGameOver || isDyingByArrow)
        {
            isHoldingButton = false;
            return;
        }

        if (GridState.Instance != null &&
            GridState.Instance.gridGameState == GridState.GridGameStateEnum.Playing)
        {
            // --- NUEVO: Magia del Hold to Move ---
            if (isHoldingButton && !isMoving)
            {
                TryMove(holdDx, holdDy);
            }
            // -------------------------------------

            if (coinObj != null)
            {
                coinTimer -= Time.deltaTime;

                float t = Mathf.Clamp01(coinTimer / coinTimeLimit);
                coinTimerImage.fillAmount = t;

                if (t > 0.5f)
                {
                    float lerpT = (t - 0.5f) * 2f;
                    coinTimerImage.color = Color.Lerp(midColor, fullColor, lerpT);
                }
                else
                {
                    float lerpT = t * 2f;
                    coinTimerImage.color = Color.Lerp(lowColor, midColor, lerpT);
                }

                if (coinTimer <= 0f)
                    TriggerFail();
            }
        }
    }

    private void UpdateScoreText()
    {
        if (scoreText == null) return;
        scoreText.text = score.ToString();
    }

    void TryMove(int dx, int dy)
    {
        if (isPausedByOffer) return;
        if (playerObj == null) return;
        if (isGameOver || isDyingByArrow || isMoving) return;
        if (GridState.Instance != null && GridState.Instance.gridGameState != GridState.GridGameStateEnum.Playing) return;

        int newX = playerX + dx;
        int newY = playerY + dy;

        if (newX >= 0 && newX < gridSize && newY >= 0 && newY < gridSize)
        {
            int oldX = playerX;
            int oldY = playerY;

            playerX = newX;
            playerY = newY;

            Transform fromCell = gridCells[oldX, oldY];
            Transform toCell = gridCells[playerX, playerY];

            float angleZ = 0f;
            if (dx == 0 && dy == -1) angleZ = 180f;
            else if (dx == 0 && dy == 1) angleZ = 0f;
            else if (dx == 1 && dy == 0) angleZ = 90f;
            else if (dx == -1 && dy == 0) angleZ = -90f;

            playerObj.transform.rotation = Quaternion.Euler(0f, 0f, angleZ);

            Vector3 targetPos = toCell.position + playerCellOffset;
            StartCoroutine(MovePlayer(targetPos, fromCell, toCell));
        }
    }

    IEnumerator MovePlayer(Vector3 targetPos, Transform fromCell, Transform toCell)
    {
        isMoving = true;

        playerVisual?.SetInAir();
        SoundManager.Instance.PlaySound(jumpAudioClip, 0.5f);

        if (fromCell != null)
        {
            if (platformSquashRoutines.TryGetValue(fromCell, out var r) && r != null) StopCoroutine(r);
            fromCell.localScale = cellBaseScale;
        }

        if (fromCell != null)
            PlayPlatformSquash(fromCell, 0.12f, 0.12f, Axis.Y_DOWN);

        Vector3 startPos = playerObj.transform.position;
        Vector3 startScale = originalScale;
        Vector3 peakScale = originalScale * 1.3f;

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            if (isPausedByOffer) { yield return null; continue; }

            float t = elapsed / moveDuration;
            playerObj.transform.position = Vector3.Lerp(startPos, targetPos, t);

            if (t < 0.5f)
                playerObj.transform.localScale = Vector3.Lerp(startScale, peakScale, t * 2f);
            else
                playerObj.transform.localScale = Vector3.Lerp(peakScale, startScale, (t - 0.5f) * 2f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        playerObj.transform.position = targetPos;
        playerObj.transform.localScale = originalScale;
        isMoving = false;

        playerVisual?.SetOnCell();

        ForceCellScale(toCell);

        if (toCell != null)
            PlayPlatformSquash(toCell, 0.1f, 0.08f, Axis.Y_UP);

        if (coinObj != null)
        {
            int coinCellX = -1;
            int coinCellY = -1;
            float bestDist = float.MaxValue;

            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    Vector3 cellPos = gridCells[x, y].position + coinCellOffset;
                    float d = Vector3.Distance(coinObj.transform.position, cellPos);

                    if (d < bestDist)
                    {
                        bestDist = d;
                        coinCellX = x;
                        coinCellY = y;
                    }
                }
            }

            if (coinCellX == playerX && coinCellY == playerY)
            {
                Destroy(coinObj);
                score++;
                SoundManager.Instance.PlaySound(pickUpAudioClip, 0.75f);

                if (gemUI != null)
                    gemUI.PlayGemCollected();

#if UNITY_ANDROID || UNITY_IOS
                Haptics.TryVibrate();
#endif

                UpdateScoreText();

                if (score % 2 == 0)
                {
                    warningTime = Mathf.Max(minWarningTime, warningTime - decreaseAmount);
                    coinTimeLimit = Mathf.Max(minCoinTime, coinTimeLimit - decreaseAmount);
                }

                SpawnCoin();
            }
        }
    }

    void PlayPlatformSquash(Transform target, float duration, float amount, Axis axis)
    {
        if (target == null) return;

        if (platformSquashRoutines.TryGetValue(target, out var running) && running != null)
        {
            StopCoroutine(running);
            target.localScale = GetRestScaleForCell(target);
        }

        var routine = StartCoroutine(PlatformSquash(target, duration, amount, axis));
        platformSquashRoutines[target] = routine;
    }

    IEnumerator PlatformSquash(Transform target, float duration, float amount, Axis axis)
    {
        if (target == null) yield break;

        Vector3 original = GetRestScaleForCell(target);
        Vector3 squashed = original;

        squashed.y = original.y * (1f - amount);
        squashed.x = original.x * (1f + amount);

        float half = duration * 0.5f;
        float t = 0f;

        while (t < half)
        {
            float k = t / half;
            target.localScale = Vector3.Lerp(original, squashed, k);
            t += Time.deltaTime;
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            float k = t / half;
            target.localScale = Vector3.Lerp(squashed, original, k);
            t += Time.deltaTime;
            yield return null;
        }

        target.localScale = original;
    }

    void SpawnCoin()
    {
        int x, y;
        do
        {
            x = UnityEngine.Random.Range(0, gridSize);
            y = UnityEngine.Random.Range(0, gridSize);
        } while (x == playerX && y == playerY);

        if (coinPrefabs == null || coinPrefabs.Length == 0) return;

        int index = UnityEngine.Random.Range(0, coinPrefabs.Length);
        GameObject chosenCoinPrefab = coinPrefabs[index];

        Vector3 spawnPos = gridCells[x, y].position + coinCellOffset;

        coinObj = Instantiate(chosenCoinPrefab, spawnPos, Quaternion.identity, gridParent);

        if (gemSpawnFxPrefab != null)
        {
            var fx = Instantiate(gemSpawnFxPrefab, spawnPos, Quaternion.identity, gridParent);
            fx.Play(GetGemColorFromPrefab(chosenCoinPrefab));
        }

        coinTimer = coinTimeLimit;
        coinTimerImage.fillAmount = 1f;
        coinTimerImage.color = fullColor;
    }

    IEnumerator ArrowRoutine()
    {
        float margin = 2f;

        while (!isGameOver && !isDyingByArrow)
        {
            if (isPausedByOffer) { yield return null; continue; }

            yield return new WaitForSeconds(rowInterval);
            if (isGameOver || isDyingByArrow) yield break;

            int arrowCount = 1;
            if (score >= 30) arrowCount = 3;
            else if (score >= 15) arrowCount = 2;

            List<(int, int)> usedCombinations = new List<(int, int)>();

            for (int i = 0; i < arrowCount; i++)
            {
                if (isGameOver || isDyingByArrow) yield break;
                if (isPausedByOffer) { yield return null; i--; continue; }

                int mode, idx;
                do
                {
                    mode = UnityEngine.Random.Range(0, 4);
                    idx = UnityEngine.Random.Range(0, gridSize);
                }
                while (usedCombinations.Contains((mode, idx)) && usedCombinations.Count < gridSize * 4);

                usedCombinations.Add((mode, idx));

                Transform start = null, end = null;
                if (mode == 0) { start = gridCells[0, idx]; end = gridCells[gridSize - 1, idx]; }
                else if (mode == 1) { start = gridCells[idx, 0]; end = gridCells[idx, gridSize - 1]; }
                else if (mode == 2) { start = gridCells[0, 0]; end = gridCells[gridSize - 1, gridSize - 1]; }
                else if (mode == 3) { start = gridCells[gridSize - 1, 0]; end = gridCells[0, gridSize - 1]; }

                bool reverse = UnityEngine.Random.value < 0.5f;
                Vector3 worldStart = reverse ? end.position : start.position;
                Vector3 worldEnd = reverse ? start.position : end.position;
                Vector3 dir = (worldEnd - worldStart).normalized;

                List<Vector2Int> cellsOnLine = new List<Vector2Int>();

                if (mode == 0)
                    for (int x = 0; x < gridSize; x++) cellsOnLine.Add(new Vector2Int(x, idx));
                else if (mode == 1)
                    for (int y = 0; y < gridSize; y++) cellsOnLine.Add(new Vector2Int(idx, y));
                else if (mode == 2)
                    for (int k = 0; k < gridSize; k++) cellsOnLine.Add(new Vector2Int(k, k));
                else if (mode == 3)
                    for (int k = 0; k < gridSize; k++) cellsOnLine.Add(new Vector2Int(gridSize - 1 - k, k));

                StartCoroutine(HighlightCellsAndShoot(worldStart, worldEnd, dir, margin, cellsOnLine));

                if (i < arrowCount - 1 && multiArrowDelay > 0f)
                    yield return new WaitForSeconds(multiArrowDelay);
            }
        }
    }

    IEnumerator HighlightCellsAndShoot(Vector3 worldStart, Vector3 worldEnd, Vector3 dir, float margin, List<Vector2Int> cellsOnLine)
    {
        if (isPausedByOffer) yield break;

        List<SpriteRenderer> renderers = new List<SpriteRenderer>();

        foreach (var cell in cellsOnLine)
        {
            Transform cellTf = gridCells[cell.x, cell.y];
            if (cellTf == null) continue;

            SpriteRenderer sr = cellTf.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                renderers.Add(sr);
                sr.color = warningCellColor;
            }
        }

        float elapsedWarn = 0f;
        while (elapsedWarn < warningTime)
        {
            if (isGameOver || isDyingByArrow || isPausedByOffer)
            {
                foreach (var sr in renderers)
                    if (sr != null) sr.color = defaultCellColor;
                yield break;
            }

            elapsedWarn += Time.deltaTime;
            yield return null;
        }

        foreach (var sr in renderers)
            if (sr != null) sr.color = defaultCellColor;

        if (isGameOver || isDyingByArrow || isPausedByOffer) yield break;

        Vector3 offStart = worldStart - dir * margin;
        Vector3 offEnd = worldEnd + dir * margin;

        if (arrowPrefabs == null || arrowPrefabs.Length == 0) yield break;

        GameObject chosenPrefab = arrowPrefabs[UnityEngine.Random.Range(0, arrowPrefabs.Length)];
        GameObject arrow = Instantiate(chosenPrefab, gridParent);
        arrow.transform.position = offStart;
        arrow.transform.right = dir;

        SoundManager.Instance.PlaySound(arrowAudioClip, 0.75f);

        float travelDist = Vector3.Distance(offStart, offEnd);
        float travelTime = travelDist / arrowSpeed;

        float elapsed = 0f;
        bool playerAttached = false;

        while (elapsed < travelTime)
        {
            if (isPausedByOffer) { yield return null; continue; }

            float t = elapsed / travelTime;
            arrow.transform.position = Vector3.Lerp(offStart, offEnd, t);

            if (!playerAttached && !isGameOver && !isDyingByArrow)
            {
                foreach (var cell in cellsOnLine)
                {
                    if (cell.x == playerX && cell.y == playerY)
                    {
                        Vector3 cellPos = gridCells[cell.x, cell.y].position;
                        float dist = Vector3.Distance(arrow.transform.position, cellPos);

                        if (dist <= cellHitRadius)
                        {
                            isDyingByArrow = true;
                            playerAttached = true;

                            if (playerObj != null)
                            {
                                playerObj.transform.SetParent(arrow.transform);
                                playerObj.transform.position = arrow.transform.position;
                            }

#if UNITY_ANDROID || UNITY_IOS
                            Haptics.TryVibrate();
#endif
                            break;
                        }
                    }
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Si el player quedó enganchado a la flecha, lo despegamos ANTES de destruirla
        if (playerAttached && playerObj != null)
        {
            playerObj.transform.SetParent(gridParent, true);
            playerObj.SetActive(false); // lo ocultas durante offer / fail (reversible)
        }

        Destroy(arrow);

        if (playerAttached)
        {
            TriggerFail();
        }

    }

    // =========================
    // Centralized fail entry
    // =========================
    private void TriggerFail()
    {
        if (gameOverInvoked) return;
        if (isPausedByOffer) return;

        // Parar gameplay
        isGameOver = true;

        if (GameOverFlowManager.Instance != null)
            GameOverFlowManager.Instance.NotifyFail(this);
        else
            FinalGameOver(); // fallback
    }

    // =========================
    // IGameOverClient
    // =========================
    public void PauseOnFail()
    {
        isPausedByOffer = true;
        // no tocar timeScale aquí
    }

    public void Revive()
    {
        isPausedByOffer = false;

        isGameOver = false;
        isDyingByArrow = false;
        isMoving = false;

        if (playerObj == null)
        {
            RespawnPlayerAtCurrentCell();
        }
        else
        {
            playerObj.transform.SetParent(gridParent, true);
            playerObj.SetActive(true);

            Transform cell = gridCells[playerX, playerY];
            if (cell != null)
                playerObj.transform.position = cell.position + playerCellOffset;

            playerVisual = playerObj.GetComponent<GridPlayerVisual>();
            if (playerVisual == null)
                playerVisual = playerObj.GetComponentInChildren<GridPlayerVisual>(true);

            playerVisual?.SetOnCell();
            ForceCellScale(gridCells[playerX, playerY]);
        }

        // reset timer
        coinTimer = coinTimeLimit;
        if (coinTimerImage != null)
        {
            coinTimerImage.fillAmount = 1f;
            coinTimerImage.color = fullColor;
        }

        if (coinObj == null) SpawnCoin();

        StartCoroutine(ArrowRoutine());
    }

    private void RespawnPlayerAtCurrentCell()
    {
        if (playerPrefab == null) return;

        Transform cell = gridCells[playerX, playerY];
        if (cell == null) return;

        Vector3 pos = cell.position + playerCellOffset;

        playerObj = Instantiate(playerPrefab, pos, Quaternion.identity, gridParent);
        originalScale = playerObj.transform.localScale;

        playerVisual = playerObj.GetComponent<GridPlayerVisual>();
        if (playerVisual == null)
            playerVisual = playerObj.GetComponentInChildren<GridPlayerVisual>(true);

        playerVisual?.SetOnCell();
        ForceCellScale(cell);
    }



    public void FinalGameOver()
    {
        if (gameOverInvoked) return;
        gameOverInvoked = true;

        // Aquí ejecutamos TU lógica original de GameOver
        SoundManager.Instance.PlaySound(deathAudioClip, 1f);

        Debug.Log($"GAME OVER - Score final: {score}");

        OnGridGameOver?.Invoke(this, EventArgs.Empty);

        if (score > 59)
            AvatarUnlockHelper.UnlockAvatar("AvatarCaballero");

        SaveRecordIfNeeded();

        PlayFabScoreManager.Instance.SubmitScore("GridScore", score);

        int coinsEarned = score / 3;
        int totalCoins = PlayerPrefs.GetInt("CoinCount", 0);
        totalCoins += coinsEarned;
        PlayerPrefs.SetInt("CoinCount", totalCoins);
        PlayerPrefs.Save();

        CoinsRewardUI rewardUI = FindObjectOfType<CoinsRewardUI>(true);
        if (rewardUI != null) rewardUI.ShowReward(coinsEarned);
        else CurrencyManager.Instance.AddCoins(coinsEarned);

        Haptics.TryVibrate();

        if (DailyMissionManager.Instance != null && score >= 30)
            DailyMissionManager.Instance.AddProgress("consigue_30_puntos_plataformas", 1);

        if (DailyMissionManager.Instance != null && score >= 20)
            DailyMissionManager.Instance.AddProgress("consigue_20_puntos_plataformas", 1);

        if (DailyMissionManager.Instance != null)
        {
            DailyMissionManager.Instance.AddProgress("juega_1_partida", 1);
            DailyMissionManager.Instance.AddProgress("juega_3_partidas", 1);
            DailyMissionManager.Instance.AddProgress("juega_8_partidas", 1);
            DailyMissionManager.Instance.AddProgress("juega_10_partidas", 1);
        }

        OnGameOver?.Invoke(this, EventArgs.Empty);
    }

    public int GetScore() => score;

    private void SaveRecordIfNeeded()
    {
        int currentRecord = PlayerPrefs.GetInt("MaxRecordGrid", 0);
        if (score > currentRecord)
        {
            PlayerPrefs.SetInt("MaxRecordGrid", score);
            PlayerPrefs.Save();
        }
    }
}
