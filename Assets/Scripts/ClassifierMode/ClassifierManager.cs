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

    private SwipeDirection currentCorrectDirection;

    [SerializeField] ClassifierScore scoreModule;

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        // Cogemos los componentes automáticamente
        inputModule = GetComponent<ClassifierInput>();
        timerModule = GetComponent<ClassifierTimer>();

        if (scoreModule == null) scoreModule = GetComponent<ClassifierScore>();
    }

    void OnEnable()
    {
        // Nos suscribimos a los eventos
        inputModule.OnSwipeDetected += HandlePlayerSwipe;
        timerModule.OnTimeOut += TimerModule_OnTimeOut;
    }

    private void TimerModule_OnTimeOut(object sender, System.EventArgs e)
    {
        HandleGameOver();
    }

    void OnDisable()
    {
        // Nos desuscribimos por seguridad para evitar fugas de memoria
        inputModule.OnSwipeDetected -= HandlePlayerSwipe;
        timerModule.OnTimeOut -= TimerModule_OnTimeOut;
    }

    void Start()
    {
        SpawnNewItem();
    }

    private void SpawnNewItem()
    {
        int randomCategoryIndex = UnityEngine.Random.Range(0, categories.Count);
        CategoryData selectedCategory = categories[randomCategoryIndex];

        if (selectedCategory.validSprites.Count == 0) return;

        Sprite selectedSprite = selectedCategory.validSprites[UnityEngine.Random.Range(0, selectedCategory.validSprites.Count)];

        centerItemRenderer.sprite = selectedSprite;
        ResizeSpriteToFit();

        currentCorrectDirection = selectedCategory.correctDirection;

        // Arrancamos todo
        timerModule.ResetAndStartTimer();
        inputModule.isInputActive = true;
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
        inputModule.isInputActive = false;
        OnClassifierGameOver?.Invoke(this, EventArgs.Empty); 
        Debug.Log("<color=red>¡FIN DEL JUEGO! El tiempo llegó a cero.</color>");
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
        SpawnNewItem();
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

    //GETTERS 
    public int GetCurrentScore()
    {
        return scoreModule.CurrentScore;
    }   
}