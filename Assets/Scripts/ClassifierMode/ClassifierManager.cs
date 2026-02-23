using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    // Referencias a nuestros otros módulos
    private ClassifierInput inputModule;
    private ClassifierTimer timerModule;
    [SerializeField] private ClassifierScore scoreModule;

    private SwipeDirection currentCorrectDirection;

    // Cerrojo de seguridad para evitar bugs de resurrección o dobles Game Overs
    private bool isGameOver = false;

    void Awake()
    {
        // Singleton de Escena (Muere al recargar)
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        // Cogemos los componentes automáticamente
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
        // Nos desuscribimos por seguridad para evitar fugas de memoria
        inputModule.OnSwipeDetected -= HandlePlayerSwipe;
        timerModule.OnTimeOut -= TimerModule_OnTimeOut;

        if (ClassifierState.Instance != null)
        {
            ClassifierState.Instance.OnPlayingClassifierGame -= ClassifierState_OnPlayingClassifierGame;
        }
    }

    void Start()
    {
        isGameOver = false; // Reseteamos el cerrojo al empezar

        // Nos suscribimos aquí en lugar del OnEnable para evitar problemas de orden con el Awake
        if (ClassifierState.Instance != null)
        {
            ClassifierState.Instance.OnPlayingClassifierGame += ClassifierState_OnPlayingClassifierGame;
        }

        // Spawneamos la imagen congelada al arrancar la escena
        SpawnNewItem(false);
    }

    private void ClassifierState_OnPlayingClassifierGame(object sender, EventArgs e)
    {
        // Si el jugador ya ha perdido (bug raro), ignoramos la señal de arrancar
        if (isGameOver) return;

        timerModule.ResetAndStartTimer();
        inputModule.isInputActive = true;
    }

    private void TimerModule_OnTimeOut(object sender, System.EventArgs e)
    {
        HandleGameOver();
    }

    // Le añadimos el parámetro (bool startMechanics = true)
    private void SpawnNewItem(bool startMechanics = true)
    {
        int randomCategoryIndex = UnityEngine.Random.Range(0, categories.Count);
        CategoryData selectedCategory = categories[randomCategoryIndex];

        if (selectedCategory.validSprites.Count == 0) return;

        Sprite selectedSprite = selectedCategory.validSprites[UnityEngine.Random.Range(0, selectedCategory.validSprites.Count)];

        centerItemRenderer.sprite = selectedSprite;
        ResizeSpriteToFit();

        currentCorrectDirection = selectedCategory.correctDirection;

        // Arrancamos el reloj y el input SOLO si nos dan permiso
        if (startMechanics)
        {
            timerModule.ResetAndStartTimer();
            inputModule.isInputActive = true;
        }
        else
        {
            // Forzamos a que el input esté APAGADO durante la cuenta atrás
            inputModule.isInputActive = false;
        }
    }

    private void HandlePlayerSwipe(SwipeDirection playerDirection)
    {
        // 1. Bloqueamos controles y paramos el tiempo
        inputModule.isInputActive = false;
        timerModule.StopTimer();

        // 2. ¿Acertó o falló?
        if (playerDirection == currentCorrectDirection)
        {
            Debug.Log("<color=green>¡ACIERTO!</color>");
            scoreModule.AddPoint(1);
            timerModule.ApplySuccessReduction(); // Hacemos el juego más difícil

            // 3. Como ha acertado, calculamos hacia dónde vuela y seguimos jugando
            Vector3 targetFlyPosition = CalculateFlyPosition(playerDirection);
            StartCoroutine(AnimateSwipeAndRespawn(targetFlyPosition));
        }
        else
        {
            // ¡FALLO! Game Over inmediato
            Debug.Log($"<color=red>¡FALLO FATAL!</color> Lo lanzaste hacia {playerDirection} y era {currentCorrectDirection}.");
            HandleGameOver();
        }
    }

    private void HandleGameOver()
    {
        // Si ya habíamos ejecutado el Game Over, no lo hacemos dos veces
        if (isGameOver) return;
        isGameOver = true;

        inputModule.isInputActive = false;
        timerModule.StopTimer(); // Congelamos el tiempo por si acaso

        EndGame();
        Debug.Log("<color=red>¡FIN DEL JUEGO! El tiempo llegó a cero o te equivocaste.</color>");
    }

    private Vector3 CalculateFlyPosition(SwipeDirection dir)
    {
        float dist = 15f;
        return dir switch
        {
            SwipeDirection.Up => Vector3.up * dist,
            SwipeDirection.Down => Vector3.down * dist,
            SwipeDirection.Left => Vector3.left * dist,
            SwipeDirection.Right => Vector3.right * dist,
            _ => Vector3.zero
        };
    }

    private IEnumerator AnimateSwipeAndRespawn(Vector3 targetPosition)
    {
        Transform itemTransform = centerItemRenderer.transform;
        while (Vector3.Distance(itemTransform.localPosition, targetPosition) > 0.1f)
        {
            itemTransform.localPosition = Vector3.MoveTowards(itemTransform.localPosition, targetPosition, swipeSpeed * Time.deltaTime);
            yield return null;
        }

        itemTransform.localPosition = Vector3.zero;

        // ¡Mini-seguro extra! Solo spawneamos otro si el juego no se ha acabado mientras volaba
        if (!isGameOver)
        {
            SpawnNewItem(true);
        }
    }

    private void ResizeSpriteToFit()
    {
        if (centerItemRenderer.sprite == null) return;
        centerItemRenderer.transform.localScale = Vector3.one;
        float maxDim = Mathf.Max(centerItemRenderer.sprite.bounds.size.x, centerItemRenderer.sprite.bounds.size.y);
        if (maxDim > 0)
        {
            float scale = targetVisualSize / maxDim;
            centerItemRenderer.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
    
    private bool hasEnded = false;

    private void EndGame()
    {
        if (hasEnded)
            return;

        hasEnded = true;

        // 1) Record PlayerPrefs
        ClassifierScore.Instance.SafeRecordIfNeeded();

        // 2) Notificar UI game over
        OnClassifierGameOver?.Invoke(this, System.EventArgs.Empty);

        // 3) PlayFab (misma lógica que Colores)
        if (PlayFabLoginManager.Instance != null && PlayFabLoginManager.Instance.IsLoggedIn)
        {
            PlayFabScoreManager.Instance.SubmitScore("ClassifierScore", ClassifierScore.Instance.GetScore());
            // Si tu stat en PlayFab se llama distinto, cambia "ClassifierScore"
        }

        // 4) Coins reward (score/3)
        int coinsEarned = ClassifierScore.Instance.GetCoinsEarned();

        CoinsRewardUI rewardUI = FindObjectOfType<CoinsRewardUI>(true);
        if (rewardUI != null)
            rewardUI.ShowReward(coinsEarned);
        else
            CurrencyManager.Instance.AddCoins(coinsEarned);
    }


    // GETTERS 
    public int GetCurrentScore()
    {
        return scoreModule.CurrentScore;
    }
}