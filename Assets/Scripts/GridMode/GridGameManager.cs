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
    public float moveDuration = 1f; // Duración animación de salto
    public float coinTimeLimit = 5f; // Tiempo para recoger moneda

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

        // Botones UI
        upButton.onClick.AddListener(() => TryMove(0, -1));
        downButton.onClick.AddListener(() => TryMove(0, 1));
        leftButton.onClick.AddListener(() => TryMove(-1, 0));
        rightButton.onClick.AddListener(() => TryMove(1, 0));

        // Rutina de flechas
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
        Vector3 peakScale = originalScale * 1.3f; //  ahora crece en proporción a su tamaño real

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            float t = elapsed / moveDuration;

            // Movimiento suave
            playerObj.transform.position = Vector3.Lerp(startPos, targetPos, t);

            // Escala tipo "salto"
            if (t < 0.5f)
                playerObj.transform.localScale = Vector3.Lerp(startScale, peakScale, t * 2f);
            else
                playerObj.transform.localScale = Vector3.Lerp(peakScale, startScale, (t - 0.5f) * 2f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        playerObj.transform.position = targetPos;
        playerObj.transform.localScale = originalScale; //  volver a su escala original real

        isMoving = false;

        // ¿Recoge moneda?
        if (coinObj != null && Vector3.Distance(playerObj.transform.position, coinObj.transform.position) < 0.1f)
        {
            Destroy(coinObj);
            score++;
            Debug.Log("Score: " + score);
            SpawnCoin();
        }
    }

    void SpawnCoin()
    {
        int x, y;
        do
        {
            x = Random.Range(0, gridSize);
            y = Random.Range(0, gridSize);
        } while (x == playerX && y == playerY);

        coinObj = Instantiate(coinPrefab, gridCells[x, y].position, Quaternion.identity, gridParent);

        // Resetear timer
        coinTimer = coinTimeLimit;
        coinTimerSlider.maxValue = coinTimeLimit;
        coinTimerSlider.value = coinTimeLimit;
    }

    IEnumerator ArrowRoutine()
    {
        Camera cam = Camera.main;
        float margin = 2f; // cuanto más grande, más lejos spawnea la flecha fuera de la pantalla

        while (!isGameOver)
        {
            yield return new WaitForSeconds(rowInterval);

            // elegir dirección
            int mode = Random.Range(0, 4); // 0=fila,1=columna,2=diag principal,3=diag secundaria
            int index = Random.Range(0, gridSize);

            Transform start = null;
            Transform end = null;

            if (mode == 0) { start = gridCells[0, index]; end = gridCells[gridSize - 1, index]; }
            else if (mode == 1) { start = gridCells[index, 0]; end = gridCells[index, gridSize - 1]; }
            else if (mode == 2) { start = gridCells[0, 0]; end = gridCells[gridSize - 1, gridSize - 1]; }
            else if (mode == 3) { start = gridCells[gridSize - 1, 0]; end = gridCells[0, gridSize - 1]; }

            // aleatorizar dirección
            bool reverse = Random.value < 0.5f;
            Vector3 worldStart = reverse ? end.position : start.position;
            Vector3 worldEnd = reverse ? start.position : end.position;

            Vector3 dir = (worldEnd - worldStart).normalized;

            // **SPAWN DE LA LINEA DE PREVIEW (WARNING) QUE ATRAVIESA TODA LA PANTALLA**
            Vector3 warningStart = worldStart;
            Vector3 warningEnd = worldEnd;

            if (mode == 0) // horizontal
            {
                warningStart = new Vector3(cam.ViewportToWorldPoint(new Vector3(0, 0, 0)).x, worldStart.y, 0);
                warningEnd = new Vector3(cam.ViewportToWorldPoint(new Vector3(1, 0, 0)).x, worldStart.y, 0);
            }
            else if (mode == 1) // vertical
            {
                warningStart = new Vector3(worldStart.x, cam.ViewportToWorldPoint(new Vector3(0, 0, 0)).y, 0);
                warningEnd = new Vector3(worldStart.x, cam.ViewportToWorldPoint(new Vector3(0, 1, 0)).y, 0);
            }
            else // diagonales
            {
                warningStart = worldStart - dir * 10f; // valor grande para que atraviese pantalla
                warningEnd = worldEnd + dir * 10f;
            }

            GameObject warning = Instantiate(warningPrefab, gridParent);
            warning.transform.position = (warningStart + warningEnd) / 2f;
            warning.transform.right = (warningEnd - warningStart).normalized;
            float length = Vector3.Distance(warningStart, warningEnd);
            warning.transform.localScale = new Vector3(length, 0.1f, 1f);

            yield return new WaitForSeconds(warningTime);
            Destroy(warning);

            if (isGameOver) yield break; // prevenir flechas tras game over

            // **SPAWN DE LA FLECHA REAL**
            Vector3 offStart = worldStart - dir * margin;
            Vector3 offEnd = worldEnd + dir * margin;

            GameObject arrow = Instantiate(arrowPrefab, gridParent);
            arrow.transform.position = offStart;
            arrow.transform.right = dir;

            float speed = 8f; // velocidad de la flecha
            float travelDist = Vector3.Distance(offStart, offEnd);
            float travelTime = travelDist / speed;

            float elapsed = 0f;
            while (elapsed < travelTime)
            {
                if (isGameOver) { Destroy(arrow); yield break; }

                arrow.transform.position = Vector3.Lerp(offStart, offEnd, elapsed / travelTime);
                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(arrow);
        }
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        Debug.Log("GAME OVER - Score final: " + score);
    }
}
