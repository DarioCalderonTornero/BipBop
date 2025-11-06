using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridGameManager : MonoBehaviour
{
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
    }

    private void Update()
    {
        if (isGameOver) return;

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

    void TryMove(int dx, int dy)
    {
        if (isGameOver || isMoving) return;

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

        while (!isGameOver)
        {
            yield return new WaitForSeconds(rowInterval);
            if (isGameOver) yield break;

            int arrowCount = 1;
            if (score >= 40) arrowCount = 3;
            else if (score >= 20) arrowCount = 2;

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
                warning.transform.right = dir;
                float length = Vector3.Distance(warningStart, warningEnd);
                warning.transform.localScale = new Vector3(length, 0.1f, 1f);

                StartCoroutine(ShootArrowAfterWarning(warning, worldStart, worldEnd, dir, margin));

                if (i < arrowCount - 1 && multiArrowDelay > 0f)
                    yield return new WaitForSeconds(multiArrowDelay);
            }
        }
    }

    IEnumerator ShootArrowAfterWarning(GameObject warning, Vector3 worldStart, Vector3 worldEnd, Vector3 dir, float margin)
    {
        yield return new WaitForSeconds(warningTime);
        if (isGameOver) { Destroy(warning); yield break; }

        Destroy(warning);
        if (isGameOver) yield break;

        Vector3 offStart = worldStart - dir * margin;
        Vector3 offEnd = worldEnd + dir * margin;

        GameObject arrow = Instantiate(arrowPrefab, gridParent);
        arrow.transform.position = offStart;
        arrow.transform.right = dir;

        float speed = 8f;
        float travelDist = Vector3.Distance(offStart, offEnd);
        float travelTime = travelDist / speed;

        float elapsed = 0f;
        while (elapsed < travelTime)
        {
            if (isGameOver)
            {
                Destroy(arrow);
                yield break;
            }

            arrow.transform.position = Vector3.Lerp(offStart, offEnd, elapsed / travelTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(arrow);
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log($"GAME OVER - Score final: {score}");

        // Guardar récord máximo
        SaveRecordIfNeeded();

        // Enviar puntuación a PlayFab
        PlayFabScoreManager.Instance.SubmitScore("GridScore", score);

        // Recompensa en monedas
        int coinsEarned = score / 15;
        int totalCoins = PlayerPrefs.GetInt("CoinCount", 0);
        totalCoins += coinsEarned;
        PlayerPrefs.SetInt("CoinCount", totalCoins);
        PlayerPrefs.Save();

        Time.timeScale = 0f;

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
}
