using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class GridGameManager : MonoBehaviour
{
    public static GridGameManager Instance { get; private set; }

    public event EventHandler OnGridGameOver;

    [Header("Grid")]
    public Transform gridParent;
    public int gridSize = 4;

    [Header("Prefabs")]
    public GameObject playerPrefab;
    public GameObject warningPrefab;
    
    [Header("Arrow Settings")]
    public float arrowSpeed = 8f;
    [SerializeField] private GameObject[] arrowPrefabs; // 3 variantes

    [Header("Coins")]
    [SerializeField] private GameObject[] coinPrefabs;  // 3 variantes

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


    public event EventHandler OnGameOver;

    private int playerX, playerY;
    private GameObject playerObj;
    private GameObject coinObj;
    private Transform[,] gridCells;

    private int score = 0;
    private bool isGameOver = false;
    private bool isMoving = false;

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

    // Squash
    public enum Axis { Y_DOWN, Y_UP }

    // Una sola corrutina de squash por plataforma
    private Dictionary<Transform, Coroutine> platformSquashRoutines = new Dictionary<Transform, Coroutine>();

    // Escala base de las casillas
    // private Vector3 cellBaseScale = new Vector3(0.79671f, 1.35f, 0.79671f);
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

    // Si tus 3 prefabs de gemas coinciden con azul, verde, morado:
    Color GetGemColorFromPrefab(GameObject prefab)
    {
        if (prefab.name.Contains("GemaAzul")) return blueGemColor;
        if (prefab.name.Contains("GemaVerde")) return greenGemColor;
        if (prefab.name.Contains("GemaMorada")) return purpleGemColor;
        return Color.white;
    }

    private void Awake()
    {
        Instance = this;

        // IMPORTANTE: inicializar aquí
        gridCells = new Transform[gridSize, gridSize];
        ApplyBaseScaleToAllCells();
        int index = 0;
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
                gridCells[x, y] = gridParent.GetChild(index++);
        }
    }

    private void Start()
    {
        // YA NO rellenes gridCells aquí
        upButton.onClick.AddListener(() => TryMove(0, -1));
        downButton.onClick.AddListener(() => TryMove(0, 1));
        leftButton.onClick.AddListener(() => TryMove(-1, 0));
        rightButton.onClick.AddListener(() => TryMove(1, 0));

        coinTimer = coinTimeLimit;
        coinTimerImage.fillAmount = 1f;
        coinTimerImage.color = fullColor;

        UpdateScoreText();
    }

    // ===== NUEVO: caída durante la cuenta atrás =====
    public void StartIntroDropDuringCountdown()
    {
        if (gridCells == null || gridCells.Length == 0)
        {
            Debug.LogError("GridGameManager: gridCells no está inicializado");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("GridGameManager: playerPrefab no está asignado en el Inspector");
            return;
        }

        playerX = 0;
        playerY = 0;

        Transform firstCell = gridCells[playerX, playerY];
        if (firstCell == null)
        {
            Debug.LogError("GridGameManager: gridCells[0,0] es null");
            return;
        }

        Vector3 cellPos = firstCell.position + playerCellOffset;
        Vector3 spawnFrom = cellPos + Vector3.up * introDropHeight;

        playerObj = Instantiate(playerPrefab, spawnFrom, Quaternion.identity, gridParent);
        originalScale = playerObj.transform.localScale;

        playerVisual = playerObj.GetComponent<GridPlayerVisual>();
        if (playerVisual == null)
            playerVisual = playerObj.GetComponentInChildren<GridPlayerVisual>(true);

        if (playerVisual == null)
        {
            Debug.LogError("GridGameManager: el prefab del jugador no tiene GridPlayerVisual");
            return;
        }

        playerVisual.SetInAir();
        StartCoroutine(IntroDropRoutine(cellPos));
    }

    private IEnumerator IntroDropRoutine(Vector3 targetPos)
    {
        float elapsed = 0f;
        Vector3 startPos = playerObj.transform.position;

        while (elapsed < introDropDuration)
        {
            float t = elapsed / introDropDuration;
            float eased = t * t * (3f - 2f * t); // suavizado
            playerObj.transform.position = Vector3.Lerp(startPos, targetPos, eased);

            elapsed += Time.deltaTime;
            yield return null;
        }

        playerObj.transform.position = targetPos;
        // se queda en la primera plataforma en pose de estar en casilla
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

    // ✅ CAMBIO: aplica base a todas las casillas
    private void ApplyBaseScaleToAllCells()
    {
        for (int y = 0; y < gridSize; y++)
            for (int x = 0; x < gridSize; x++)
                if (gridCells[x, y] != null)
                    gridCells[x, y].localScale = cellBaseScale;
    }

    // ✅ CAMBIO: fuerza escala correcta a una casilla (corta squash si lo hubiera)
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

    // ===== NUEVO: arranque real del gameplay tras la cuenta atrás =====
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
        if (isGameOver || isDyingByArrow) return;

        if (GridState.Instance.gridGameState == GridState.GridGameStateEnum.Playing)
        {
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
                    GameOver();
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
        if (playerObj == null) return;
        if (isGameOver || isDyingByArrow || isMoving) return;
        if (GridState.Instance.gridGameState != GridState.GridGameStateEnum.Playing) return;

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

            // Rotación en Z según dirección (ajusta según cómo mire tu sprite)
            float angleZ = 0f;

            if (dx == 0 && dy == -1) angleZ = 180f;  // arriba
            else if (dx == 0 && dy == 1) angleZ = 0f;    // abajo
            else if (dx == 1 && dy == 0) angleZ = 90f;   // derecha
            else if (dx == -1 && dy == 0) angleZ = -90f;  // izquierda

            playerObj.transform.rotation = Quaternion.Euler(0f, 0f, angleZ);

            // Destino con offset hacia arriba
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
            // si tenía squash, lo cortamos y la dejamos en base
            if (platformSquashRoutines.TryGetValue(fromCell, out var r) && r != null) StopCoroutine(r);
            fromCell.localScale = cellBaseScale;
        }

        // Plataforma de salida: squash al inicio
        if (fromCell != null)
            PlayPlatformSquash(fromCell, 0.12f, 0.12f, Axis.Y_DOWN);

        Vector3 startPos = playerObj.transform.position;
        Vector3 startScale = originalScale;
        Vector3 peakScale = originalScale * 1.3f;

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            float t = elapsed / moveDuration;
            playerObj.transform.position = Vector3.Lerp(startPos, targetPos, t);

            if (t < 0.5f)
                playerObj.transform.localScale = Vector3.Lerp(startScale, peakScale, t * 2f);
            else
                playerObj.transform.localScale = Vector3.Lerp(peakScale, startScale, (t - 0.5f) * 2f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Posición final con offset
        playerObj.transform.position = targetPos;
        playerObj.transform.localScale = originalScale;
        isMoving = false;

        playerVisual?.SetOnCell();

        ForceCellScale(toCell);

        // Squash en plataforma de llegada
        if (toCell != null)
            PlayPlatformSquash(toCell, 0.1f, 0.08f, Axis.Y_UP);

        // Recoger moneda
        if (coinObj != null)
        {
            // Averiguar en qué celda está la moneda
            int coinCellX = -1;
            int coinCellY = -1;
            float bestDist = float.MaxValue;

            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    // Comparamos con la posición base de la celda + offset de moneda
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

            // Si la moneda está en la misma celda que el jugador, la recogemos
            if (coinCellX == playerX && coinCellY == playerY)
            {
                Destroy(coinObj);
                score++;
                SoundManager.Instance.PlaySound(pickUpAudioClip, 0.75f);

                // Efecto UI: sacudir saco + popup "+1"
                if (gemUI != null)
                    gemUI.PlayGemCollected();

#if UNITY_ANDROID || UNITY_IOS
                Haptics.TryVibrate();
#endif

                Debug.Log("Score: " + score);

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

    // Lanza squash garantizando 1 corrutina por celda y reseteando escala al empezar
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

        if (axis == Axis.Y_DOWN)
        {
            squashed.y = original.y * (1f - amount);
            squashed.x = original.x * (1f + amount);
        }
        else if (axis == Axis.Y_UP)
        {
            squashed.y = original.y * (1f - amount);
            squashed.x = original.x * (1f + amount);
        }

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

        if (coinPrefabs == null || coinPrefabs.Length == 0)
        {
            Debug.LogError("No hay coinPrefabs asignados en GridGameManager");
            return;
        }

        int index = UnityEngine.Random.Range(0, coinPrefabs.Length);
        GameObject chosenCoinPrefab = coinPrefabs[index];

        Vector3 spawnPos = gridCells[x, y].position + coinCellOffset;

        // Instanciar la gema
        coinObj = Instantiate(chosenCoinPrefab, spawnPos, Quaternion.identity, gridParent);

        // Instanciar FX de aparición
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
            yield return new WaitForSeconds(rowInterval);
            if (isGameOver || isDyingByArrow) yield break;

            int arrowCount = 1;
            if (score >= 30) arrowCount = 3;
            else if (score >= 15) arrowCount = 2;

            List<(int, int)> usedCombinations = new List<(int, int)>();

            for (int i = 0; i < arrowCount; i++)
            {
                if (isGameOver) yield break;

                int mode, index;
                do
                {
                    mode = UnityEngine.Random.Range(0, 4);
                    index = UnityEngine.Random.Range(0, gridSize);
                }
                while (usedCombinations.Contains((mode, index)) && usedCombinations.Count < gridSize * 4);
                usedCombinations.Add((mode, index));

                Transform start = null, end = null;
                if (mode == 0) { start = gridCells[0, index]; end = gridCells[gridSize - 1, index]; }
                else if (mode == 1) { start = gridCells[index, 0]; end = gridCells[index, gridSize - 1]; }
                else if (mode == 2) { start = gridCells[0, 0]; end = gridCells[gridSize - 1, gridSize - 1]; }
                else if (mode == 3) { start = gridCells[gridSize - 1, 0]; end = gridCells[0, gridSize - 1]; }

                bool reverse = UnityEngine.Random.value < 0.5f;
                Vector3 worldStart = reverse ? end.position : start.position;
                Vector3 worldEnd = reverse ? start.position : end.position;
                Vector3 dir = (worldEnd - worldStart).normalized;

                // === Calcular casillas por las que pasa la flecha (ya lo tenías) ===
                List<Vector2Int> cellsOnLine = new List<Vector2Int>();

                if (mode == 0)
                {
                    for (int x = 0; x < gridSize; x++)
                        cellsOnLine.Add(new Vector2Int(x, index));
                }
                else if (mode == 1)
                {
                    for (int y = 0; y < gridSize; y++)
                        cellsOnLine.Add(new Vector2Int(index, y));
                }
                else if (mode == 2)
                {
                    for (int k = 0; k < gridSize; k++)
                        cellsOnLine.Add(new Vector2Int(k, k));
                }
                else if (mode == 3)
                {
                    for (int k = 0; k < gridSize; k++)
                        cellsOnLine.Add(new Vector2Int(gridSize - 1 - k, k));
                }

                // === NUEVO: corrutina que ilumina las casillas y luego dispara la flecha ===
                StartCoroutine(HighlightCellsAndShoot(worldStart, worldEnd, dir, margin, cellsOnLine));

                if (i < arrowCount - 1 && multiArrowDelay > 0f)
                    yield return new WaitForSeconds(multiArrowDelay);
            }
        }
    }

    IEnumerator HighlightCellsAndShoot(
    Vector3 worldStart,
    Vector3 worldEnd,
    Vector3 dir,
    float margin,
    List<Vector2Int> cellsOnLine)
    {
        // 1) Iluminar casillas
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

        // 2) Esperar tiempo de aviso
        float elapsedWarn = 0f;
        while (elapsedWarn < warningTime)
        {
            if (isGameOver || isDyingByArrow)
            {
                // Restaurar colores y salir
                foreach (var sr in renderers)
                    if (sr != null) sr.color = defaultCellColor;
                yield break;
            }

            elapsedWarn += Time.deltaTime;
            yield return null;
        }

        // 3) Restaurar colores
        foreach (var sr in renderers)
            if (sr != null) sr.color = defaultCellColor;

        if (isGameOver || isDyingByArrow) yield break;

        // 4) Disparar flecha como antes
        Vector3 offStart = worldStart - dir * margin;
        Vector3 offEnd = worldEnd + dir * margin;

        GameObject chosenPrefab = null;
        if (arrowPrefabs != null && arrowPrefabs.Length > 0)
        {
            int index = UnityEngine.Random.Range(0, arrowPrefabs.Length);
            chosenPrefab = arrowPrefabs[index];
        }
        else
        {
            Debug.LogError("No hay arrowPrefabs asignados en GridGameManager");
            yield break;
        }

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
            float t = elapsed / travelTime;
            arrow.transform.position = Vector3.Lerp(offStart, offEnd, t);

            if (playerAttached && playerObj != null)
            {
                playerObj.transform.position = arrow.transform.position;
            }

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

                            playerObj.transform.SetParent(arrow.transform);
                            playerObj.transform.position = arrow.transform.position;

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

        Destroy(arrow);

        if (playerAttached)
        {
            GameOver();
        }
    }

    public int GetScore()
    {
        return score;
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        SoundManager.Instance.PlaySound(deathAudioClip, 1f);

        Debug.Log($"GAME OVER - Score final: {score}");

        OnGridGameOver?.Invoke(this, EventArgs.Empty);

        SaveRecordIfNeeded();

        PlayFabScoreManager.Instance.SubmitScore("GridScore", score);

        int coinsEarned = score / 3;
        int totalCoins = PlayerPrefs.GetInt("CoinCount", 0);
        totalCoins += coinsEarned;
        PlayerPrefs.SetInt("CoinCount", totalCoins);
        PlayerPrefs.Save();

        CoinsRewardUI rewardUI = FindObjectOfType<CoinsRewardUI>(true);
        if (rewardUI != null)
        {
            rewardUI.ShowReward(coinsEarned);
        }
        else
        {
            CurrencyManager.Instance.AddCoins(coinsEarned);
        }

        Haptics.TryVibrate();

        if (DailyMissionManager.Instance != null && score >= 30)
        {
            DailyMissionManager.Instance.AddProgress("consigue_30_puntos_plataformas", 1);
        }

        if (DailyMissionManager.Instance != null && score >= 20)
        {
            DailyMissionManager.Instance.AddProgress("consigue_20_puntos_plataformas", 1);
        }

        if (DailyMissionManager.Instance != null)
        {
            DailyMissionManager.Instance.AddProgress("juega_1_partida", 1);
        }

        if (DailyMissionManager.Instance != null)
        {
            DailyMissionManager.Instance.AddProgress("juega_3_partidas", 1);
        }

        if (DailyMissionManager.Instance != null)
        {
            DailyMissionManager.Instance.AddProgress("juega_8_partidas", 1);
        }

        if (DailyMissionManager.Instance != null)
        {
            DailyMissionManager.Instance.AddProgress("juega_10_partidas", 1);
        }

        OnGameOver?.Invoke(this, EventArgs.Empty);
    }

    private bool TryGetCellFromWorld(Vector3 worldPos, out int cellX, out int cellY)
    {
        float bestDist = float.MaxValue;
        int bestX = -1;
        int bestY = -1;

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                float d = Vector3.Distance(worldPos, gridCells[x, y].position);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestX = x;
                    bestY = y;
                }
            }
        }

        cellX = bestX;
        cellY = bestY;

        return bestX != -1;
    }

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
