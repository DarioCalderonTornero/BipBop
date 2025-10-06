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

    // Nuevo: retraso entre flechas cuando hay varias (ajustable en inspector)
    [Tooltip("Retraso entre el spawn/preview de flechas cuando se lanzan múltiples (0 = simultáneo)")]
    public float multiArrowDelay = 0.25f;

    [Header("UI Buttons")]
    public Button upButton;
    public Button downButton;
    public Button leftButton;
    public Button rightButton;

    [Header("UI Timer")]
    public Slider coinTimerSlider;

    private int playerX, playerY;
    private GameObject playerObj;
    private GameObject coinObj;
    private Transform[,] gridCells;

    private int score = 0;
    private bool isGameOver = false;
    private bool isMoving = false;

    private float coinTimer;
    private Vector3 originalScale;

    // valores mínimos y decrementos
    private const float minWarningTime = 0.5f;
    private const float minCoinTime = 7f;
    private const float decreaseAmount = 0.05f;

    private void Start()
    {
        // Inicializar grid
        gridCells = new Transform[gridSize, gridSize];
        int index = 0;
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                gridCells[x, y] = gridParent.GetChild(index);
                index++;
            }
        }

        // Crear jugador
        playerX = 0;
        playerY = 0;
        playerObj = Instantiate(playerPrefab, gridCells[playerX, playerY].position, Quaternion.identity, gridParent);
        originalScale = playerObj.transform.localScale;

        // Crear moneda
        SpawnCoin();

        // Botones
        upButton.onClick.AddListener(() => TryMove(0, -1));
        downButton.onClick.AddListener(() => TryMove(0, 1));
        leftButton.onClick.AddListener(() => TryMove(-1, 0));
        rightButton.onClick.AddListener(() => TryMove(1, 0));

        // Rutina flechas
        StartCoroutine(ArrowRoutine());

        // Timer inicial
        coinTimer = coinTimeLimit;
        coinTimerSlider.maxValue = coinTimeLimit;
        coinTimerSlider.value = coinTimeLimit;
    }

    private void Update()
    {
        if (isGameOver) return;

        // Timer de la moneda
        if (coinObj != null)
        {
            coinTimer -= Time.deltaTime;
            coinTimerSlider.value = coinTimer;

            if (coinTimer <= 0f)
            {
                GameOver();
            }
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

        // Recoge moneda
        if (coinObj != null && Vector3.Distance(playerObj.transform.position, coinObj.transform.position) < 0.1f)
        {
            Destroy(coinObj);
            score++;
            Debug.Log("Score: " + score);

            // cada 2 puntos: bajar tiempos
            if (score % 2 == 0)
            {
                warningTime = Mathf.Max(minWarningTime, warningTime - decreaseAmount);
                coinTimeLimit = Mathf.Max(minCoinTime, coinTimeLimit - decreaseAmount);
                Debug.Log($"Dificultad aumentada warningTime: {warningTime:F2}, coinTimeLimit: {coinTimeLimit:F2}");
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

        // Reset timer
        coinTimer = coinTimeLimit;
        coinTimerSlider.maxValue = coinTimeLimit;
        coinTimerSlider.value = coinTimeLimit;
    }

    IEnumerator ArrowRoutine()
    {
        Camera cam = Camera.main;
        float margin = 2f;

        while (!isGameOver)
        {
            yield return new WaitForSeconds(rowInterval);

            if (isGameOver) yield break; // <--- CORTA AQUÍ TAMBIÉN POR SEGURIDAD

            // --- determinar cuántas flechas lanzar según la puntuación ---
            int arrowCount = 1;
            if (score >= 40) arrowCount = 3;
            else if (score >= 20) arrowCount = 2;

            // lista de combinaciones ya usadas (para no repetir líneas iguales)
            List<(int, int)> usedCombinations = new List<(int, int)>();

            for (int i = 0; i < arrowCount; i++)
            {
                if (isGameOver) yield break; // <--- chequeo antes de lanzar cada flecha

                // elegir dirección y línea diferentes si es posible
                int mode, index;
                do
                {
                    mode = UnityEngine.Random.Range(0, 4); // 0=fila, 1=columna, 2=diag principal, 3=diag secundaria
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

                // --- warning line que atraviesa toda la pantalla ---
                Vector3 warningStart = worldStart;
                Vector3 warningEnd = worldEnd;

                if (mode == 0)
                {
                    warningStart = new Vector3(cam.ViewportToWorldPoint(new Vector3(0, 0, 0)).x, worldStart.y, 0);
                    warningEnd = new Vector3(cam.ViewportToWorldPoint(new Vector3(1, 0, 0)).x, worldStart.y, 0);
                }
                else if (mode == 1)
                {
                    warningStart = new Vector3(worldStart.x, cam.ViewportToWorldPoint(new Vector3(0, 0, 0)).y, 0);
                    warningEnd = new Vector3(worldStart.x, cam.ViewportToWorldPoint(new Vector3(0, 1, 0)).y, 0);
                }
                else
                {
                    warningStart = worldStart - dir * 10f;
                    warningEnd = worldEnd + dir * 10f;
                }

                // Si justo se activa el game over antes de instanciar
                if (isGameOver) yield break;

                // Instanciar el aviso
                GameObject warning = Instantiate(warningPrefab, gridParent);
                warning.transform.position = (warningStart + warningEnd) / 2f;
                warning.transform.right = (warningEnd - warningStart).normalized;
                float length = Vector3.Distance(warningStart, warningEnd);
                warning.transform.localScale = new Vector3(length, 0.1f, 1f);

                // Ejecutar la flecha asociada a este aviso en una coroutine separada
                StartCoroutine(ShootArrowAfterWarning(warning, worldStart, worldEnd, dir, margin));

                // ESPERA configurable entre múltiples flechas
                if (i < arrowCount - 1 && multiArrowDelay > 0f)
                    yield return new WaitForSeconds(multiArrowDelay);
            }
        }
    }

    IEnumerator ShootArrowAfterWarning(GameObject warning, Vector3 worldStart, Vector3 worldEnd, Vector3 dir, float margin)
    {
        yield return new WaitForSeconds(warningTime);

        if (isGameOver)
        {
            Destroy(warning);
            yield break;
        }

        Destroy(warning);

        if (isGameOver) yield break; // <--- chequeo extra justo antes de crear la flecha

        // Spawn de la flecha
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
        Debug.Log("GAME OVER - Score final: " + score);
    }
}
