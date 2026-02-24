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

    [Header("UI de Categorías (Bordes)")]
    public Image topCategoryIcon;
    public Image bottomCategoryIcon;
    public Image leftCategoryIcon;
    public Image rightCategoryIcon;

    // Referencias a nuestros otros módulos
    private ClassifierInput inputModule;
    private ClassifierTimer timerModule;
    [SerializeField] private ClassifierScore scoreModule;

    private SwipeDirection currentCorrectDirection;
    private bool isGameOver = false;
    private bool hasEnded = false;

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

        if (ClassifierState.Instance != null)
        {
            ClassifierState.Instance.OnPlayingClassifierGame += ClassifierState_OnPlayingClassifierGame;
        }

        SetupCategoryUI();
        StartUIBreathing(); // ¡NUEVO! Arrancamos la respiración de los bordes
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

            switch (category.correctDirection)
            {
                case SwipeDirection.Up:
                    if (topCategoryIcon != null) topCategoryIcon.sprite = category.categoryIcon;
                    break;
                case SwipeDirection.Down:
                    if (bottomCategoryIcon != null) bottomCategoryIcon.sprite = category.categoryIcon;
                    break;
                case SwipeDirection.Left:
                    if (leftCategoryIcon != null) leftCategoryIcon.sprite = category.categoryIcon;
                    break;
                case SwipeDirection.Right:
                    if (rightCategoryIcon != null) rightCategoryIcon.sprite = category.categoryIcon;
                    break;
            }
        }
    }

    // --- NUEVA SECCIÓN: ANIMACIONES DE UI (LEANTWEEN) ---

    private void StartUIBreathing()
    {
        // Ponemos a respirar a los 4 iconos
        AnimateBreathing(topCategoryIcon);
        AnimateBreathing(bottomCategoryIcon);
        AnimateBreathing(leftCategoryIcon);
        AnimateBreathing(rightCategoryIcon);
    }

    private void AnimateBreathing(Image icon)
    {
        if (icon == null) return;

        // Escala de 1.0 a 1.08 suavemente, y hace ping-pong (va y vuelve) para siempre
        LeanTween.scale(icon.gameObject, Vector3.one * 1.08f, 1.2f)
            .setEase(LeanTweenType.easeInOutSine)
            .setLoopPingPong();
    }

    private void PunchIcon(SwipeDirection dir)
    {
        // Buscamos cuál es el icono que ha acertado el jugador
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
            // 1. Cancelamos la respiración actual de este icono específico
            LeanTween.cancel(targetIcon.gameObject);

            // 2. Nos aseguramos de que parte de la escala base
            targetIcon.transform.localScale = Vector3.one;

            // 3. ¡Latigazo! Sube a 1.3 super rápido (0.15s), vuelve a bajar (pingPong 1 vez)
            LeanTween.scale(targetIcon.gameObject, Vector3.one * 1.3f, 0.15f)
                .setEase(LeanTweenType.easeOutQuad)
                .setLoopPingPong(1)
                .setOnComplete(() =>
                {
                    // 4. Cuando termina de celebrar el acierto, lo ponemos a respirar de nuevo
                    AnimateBreathing(targetIcon);
                });
        }
    }

    // ----------------------------------------------------

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
        int randomCategoryIndex = UnityEngine.Random.Range(0, categories.Count);
        CategoryData selectedCategory = categories[randomCategoryIndex];

        if (selectedCategory.validSprites.Count == 0) return;

        Sprite selectedSprite = selectedCategory.validSprites[UnityEngine.Random.Range(0, selectedCategory.validSprites.Count)];

        centerItemRenderer.sprite = selectedSprite;

        // Efecto POP del objeto central
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

        if (playerDirection == currentCorrectDirection)
        {
            Debug.Log("<color=green>¡ACIERTO!</color>");
            scoreModule.AddPoint(1);
            timerModule.ApplySuccessReduction();

            // ¡NUEVO! Damos el latigazo visual al icono correspondiente
            PunchIcon(playerDirection);

            Vector3 targetFlyPosition = CalculateFlyPosition(playerDirection);
            StartCoroutine(AnimateSwipeAndRespawn(targetFlyPosition));
        }
        else
        {
            Debug.Log($"<color=red>¡FALLO FATAL!</color> Lo lanzaste hacia {playerDirection} y era {currentCorrectDirection}.");
            HandleGameOver();
        }
    }

    private void HandleGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        inputModule.isInputActive = false;
        timerModule.StopTimer();

        EndGame();
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

        if (!isGameOver)
        {
            SpawnNewItem(true);
        }
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