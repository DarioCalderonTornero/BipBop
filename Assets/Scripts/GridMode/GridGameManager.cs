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
    public GameObject coinPrefab;
    public GameObject warningPrefab;
    public GameObject arrowPrefab;

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

    [Header("Arrow Settings")]
    public float arrowSpeed = 8f;

    [Header("UI Score")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Localization")]
    [SerializeField] private LocalizedString scoreLabel;

    private bool isDyingByArrow = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        gridCells = new Transform[gridSize, gridSize];
        int index = 0;
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
                gridCells[x, y] = gridParent.GetChild(index++);
        }

        playerX = 0;
        playerY = 0;
        playerObj = Instantiate(playerPrefab, gridCells[playerX, playerY].position, Quaternion.identity, gridParent);
        originalScale = playerObj.transform.localScale;

        SpawnCoin();

        upButton.onClick.AddListener(() => TryMove(0, -1));
        downButton.onClick.AddListener(() => TryMove(0, 1));
        leftButton.onClick.AddListener(() => TryMove(-1, 0));
        rightButton.onClick.AddListener(() => TryMove(1, 0));

        StartCoroutine(ArrowRoutine());

        coinTimer = coinTimeLimit;
        coinTimerImage.fillAmount = 1f;
        coinTimerImage.color = fullColor;

        UpdateScoreText();
    }

    private void Update()
    {
        if (isGameOver || isDyingByArrow) return;

        // Solo actualizamos el temporizador si el estado del juego es "Playing"
        if (GridState.Instance.gridGameState == GridState.GridGameStateEnum.Playing)
        {
            if (coinObj != null)
            {
                coinTimer -= Time.deltaTime;

                float t = Mathf.Clamp01(coinTimer / coinTimeLimit);
                coinTimerImage.fillAmount = t;

                // Interpolación de color
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
        scoreText.text = scoreLabel.GetLocalizedString(score);
    }

    void TryMove(int dx, int dy)
    {
        if (isGameOver || isDyingByArrow || isMoving) return;

        if (GridState.Instance.gridGameState != GridState.GridGameStateEnum.Playing)
            return;

        int newX = playerX + dx;
        int newY = playerY + dy;

        if (newX >= 0 && newX < gridSize && newY >= 0 && newY < gridSize)
        {
            playerX = newX;
            playerY = newY;
            StartCoroutine(MovePlayer(gridCells[playerX, playerY].position));
        }
    }

    IEnumerator MovePlayer(Vector3 targetPos)
    {
        isMoving = true;
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

        playerObj.transform.position = targetPos;
        playerObj.transform.localScale = originalScale;
        isMoving = false;

        if (coinObj != null && Vector3.Distance(playerObj.transform.position, coinObj.transform.position) < 0.1f)
        {
            Destroy(coinObj);
            score++;

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

    void SpawnCoin()
    {
        int x, y;
        do
        {
            x = UnityEngine.Random.Range(0, gridSize);
            y = UnityEngine.Random.Range(0, gridSize);
        } while (x == playerX && y == playerY);

        coinObj = Instantiate(coinPrefab, gridCells[x, y].position, Quaternion.identity, gridParent);

        coinTimer = coinTimeLimit;
        coinTimerImage.fillAmount = 1f;
        coinTimerImage.color = fullColor;
    }

    IEnumerator ArrowRoutine()
    {
        Camera cam = Camera.main;
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

                Vector3 warningStart = worldStart - dir * 10f;
                Vector3 warningEnd = worldEnd + dir * 10f;

                if (isGameOver) yield break;

                GameObject warning = Instantiate(warningPrefab, gridParent);
                warning.transform.position = (warningStart + warningEnd) / 2f;

                // IMPORTANTE: el sprite de warning apunta hacia la DERECHA (eje X local)
                warning.transform.right = dir;

                float length = Vector3.Distance(warningStart, warningEnd);
                warning.transform.localScale = new Vector3(length, 0.1f, 1f);

                // === NUEVO: lista de casillas por las que pasa ESTA flecha ===
                List<Vector2Int> cellsOnLine = new List<Vector2Int>();

                if (mode == 0) // fila horizontal
                {
                    for (int x = 0; x < gridSize; x++)
                        cellsOnLine.Add(new Vector2Int(x, index));
                }
                else if (mode == 1) // columna vertical
                {
                    for (int y = 0; y < gridSize; y++)
                        cellsOnLine.Add(new Vector2Int(index, y));
                }
                else if (mode == 2) // diagonal principal
                {
                    for (int k = 0; k < gridSize; k++)
                        cellsOnLine.Add(new Vector2Int(k, k));
                }
                else if (mode == 3) // diagonal secundaria
                {
                    for (int k = 0; k < gridSize; k++)
                        cellsOnLine.Add(new Vector2Int(gridSize - 1 - k, k));
                }

                StartCoroutine(ShootArrowAfterWarning(warning, worldStart, worldEnd, dir, margin, cellsOnLine));

                if (i < arrowCount - 1 && multiArrowDelay > 0f)
                    yield return new WaitForSeconds(multiArrowDelay);
            }
        }
    }

    IEnumerator ShootArrowAfterWarning(
    GameObject warning,
    Vector3 worldStart,
    Vector3 worldEnd,
    Vector3 dir,
    float margin,
    List<Vector2Int> cellsOnLine)
    {
        // Espera de aviso
        yield return new WaitForSeconds(warningTime);
        if (isGameOver || isDyingByArrow) { Destroy(warning); yield break; }

        Destroy(warning);
        if (isGameOver || isDyingByArrow) yield break;

        Vector3 offStart = worldStart - dir * margin;
        Vector3 offEnd = worldEnd + dir * margin;

        GameObject arrow = Instantiate(arrowPrefab, gridParent);
        arrow.transform.position = offStart;

        // IMPORTANTE: tu sprite de flecha debe estar "mirando" en el eje Y local
        arrow.transform.up = dir;

        float travelDist = Vector3.Distance(offStart, offEnd);
        float travelTime = travelDist / arrowSpeed;

        float elapsed = 0f;
        bool playerAttached = false;   // ¿ya hemos clavado al jugador?

        while (elapsed < travelTime)
        {
            // la flecha se sigue moviendo incluso si ya ha clavado al jugador
            float t = elapsed / travelTime;
            arrow.transform.position = Vector3.Lerp(offStart, offEnd, t);

            // Si está clavado, movemos al jugador con la flecha
            if (playerAttached && playerObj != null)
            {
                playerObj.transform.position = arrow.transform.position;
            }

            // Si todavía no está clavado, comprobamos impacto SOLO en la casilla actual del jugador
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
                            // Marcar que estamos en animación de muerte por flecha
                            isDyingByArrow = true;
                            playerAttached = true;

                            // "Clavamos" al jugador en la flecha
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

        // La flecha sale de pantalla
        Destroy(arrow);

        // Si hemos llevado al jugador con la flecha, ahora sí hacemos GameOver
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

        Debug.Log($"GAME OVER - Score final: {score}");

        OnGridGameOver?.Invoke(this, EventArgs.Empty);  

        // Guardar récord máximo
        SaveRecordIfNeeded();

        // Enviar puntuación a PlayFab
        PlayFabScoreManager.Instance.SubmitScore("GridScore", score);

        // Recompensa en monedas
        int coinsEarned = score;
        int totalCoins = PlayerPrefs.GetInt("CoinCount", 0);
        totalCoins += coinsEarned;
        PlayerPrefs.SetInt("CoinCount", totalCoins);
        PlayerPrefs.Save();

        // Time.timeScale = 0f;

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

    // Nuevo método para guardar récord máximo
    private void SaveRecordIfNeeded()
    {
        int currentRecord = PlayerPrefs.GetInt("MaxRecordGrid", 0);

        if (score > currentRecord)
        {
            PlayerPrefs.SetInt("MaxRecordGrid", score);
            PlayerPrefs.Save();
        }
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale newLocale)
    {
        UpdateScoreText();
    }
}
