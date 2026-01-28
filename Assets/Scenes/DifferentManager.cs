using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DifferentManager : MonoBehaviour
{
    public static DifferentManager Instance { get; private set; }

    public event Action OnRoundChanged;
    public event Action<int> OnScoreChanged;
    public event Action OnFinished; // cuando se acaba el tiempo o fallas (si activas failOnWrong)

    [Header("UI (Opcional)")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private Image timeBarImage;

    [Header("Timer")]
    [SerializeField] private bool useTimer = true;
    [SerializeField] private float startTime = 30f;

    [Header("Grid")]
    [SerializeField] private RectTransform gridRoot;       // Tiene GridLayoutGroup
    [SerializeField] private DifferentTile tilePrefab;     // Prefab (Button+Image+DifferentTile)
    [SerializeField] private int itemCount = 16;           // total tiles

    [Header("Gameplay")]
    [SerializeField] private bool failOnWrongClick = true;
    [SerializeField] private float timeBonusOnCorrect = 0.75f;

    public enum DifferenceType { Color, Rotation, Scale, Sprite }

    [Header("Patterns (Opcional)")]
    [SerializeField] private Sprite[] patternSprites;  // para tipo Sprite
    [SerializeField] private Color[] patternColors;    // para tipo Color

    [Header("Allowed Differences")]
    [SerializeField] private bool allowColor = true;
    [SerializeField] private bool allowRotation = true;
    [SerializeField] private bool allowScale = true;
    [SerializeField] private bool allowSprite = true;

    [Header("Tuning")]
    [SerializeField] private Vector2 rotationDeltaRange = new Vector2(12f, 60f);
    [SerializeField] private Vector2 oddScaleMultiplierRange = new Vector2(0.78f, 1.28f);
    [SerializeField] private float minScaleDeltaFromOne = 0.10f;

    private readonly List<DifferentTile> tiles = new List<DifferentTile>();
    private int oddIndex = -1;
    private DifferenceType currentType;

    private int score = 0;
    private float currentTime;
    private bool isRunning;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        score = 0;
        currentTime = startTime;
        isRunning = true;

        BuildGrid();
        UpdateUI();
        SetupRound();
    }

    private void Update()
    {
        if (!isRunning) return;

        if (useTimer)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0f)
            {
                currentTime = 0f;
                Finish();
                return;
            }
            UpdateUI();
        }
    }

    private void BuildGrid()
    {
        // Limpia
        for (int i = 0; i < tiles.Count; i++)
        {
            if (tiles[i] != null) Destroy(tiles[i].gameObject);
        }
        tiles.Clear();

        if (gridRoot == null || tilePrefab == null) return;

        for (int i = 0; i < itemCount; i++)
        {
            var t = Instantiate(tilePrefab, gridRoot);
            int captured = i;
            t.Bind(captured, OnTileClicked);
            tiles.Add(t);
        }
    }

    private void SetupRound()
    {
        if (!isRunning) return;
        if (tiles.Count == 0) return;

        currentType = PickAllowedType();
        oddIndex = UnityEngine.Random.Range(0, tiles.Count);

        // Base pattern (sprite/color siguen igual)
        Sprite baseSprite = (patternSprites != null && patternSprites.Length > 0)
            ? patternSprites[UnityEngine.Random.Range(0, patternSprites.Length)]
            : null;

        Color baseColor = (patternColors != null && patternColors.Length > 0)
            ? patternColors[UnityEngine.Random.Range(0, patternColors.Length)]
            : Color.white;

        // Base TRANSFORM = lo que tenga el prefab (guardado en cada tile)
        // (Asumo que todos comparten base, pero aunque no, cada uno aplica la suya)
        for (int i = 0; i < tiles.Count; i++)
            tiles[i].ApplyBase(baseSprite, baseColor);

        // Ahora hago *una* distinta (delta sobre su base)
        ApplyOdd(baseSprite, baseColor, oddIndex);

        OnRoundChanged?.Invoke();
    }

    private DifferenceType PickAllowedType()
    {
        var allowed = new List<DifferenceType>(4);

        if (allowColor && patternColors != null && patternColors.Length >= 2) allowed.Add(DifferenceType.Color);
        if (allowRotation) allowed.Add(DifferenceType.Rotation);
        if (allowScale) allowed.Add(DifferenceType.Scale);
        if (allowSprite && patternSprites != null && patternSprites.Length >= 2) allowed.Add(DifferenceType.Sprite);

        if (allowed.Count == 0) return DifferenceType.Rotation;
        return allowed[UnityEngine.Random.Range(0, allowed.Count)];
    }

    private void ApplyOdd(Sprite baseSprite, Color baseColor, int index)
    {
        var t = tiles[index];

        switch (currentType)
        {
            case DifferenceType.Color:
                {
                    Color odd = baseColor;
                    int guard = 0;
                    while (odd == baseColor && guard++ < 20)
                        odd = patternColors[UnityEngine.Random.Range(0, patternColors.Length)];
                    t.SetColor(odd);
                    break;
                }

            case DifferenceType.Sprite:
                {
                    Sprite odd = baseSprite;
                    int guard = 0;
                    while (odd == baseSprite && guard++ < 20)
                        odd = patternSprites[UnityEngine.Random.Range(0, patternSprites.Length)];
                    t.SetSprite(odd);
                    break;
                }

            case DifferenceType.Scale:
                {
                    // delta multiplicativo sobre la escala base del prefab
                    float m = 1f;
                    int guard = 0;
                    while (Mathf.Abs(m - 1f) < minScaleDeltaFromOne && guard++ < 30)
                        m = UnityEngine.Random.Range(oddScaleMultiplierRange.x, oddScaleMultiplierRange.y);

                    t.SetScale(t.GetBaseScale() * m);
                    break;
                }

            case DifferenceType.Rotation:
            default:
                {
                    // delta sobre la rotación base del prefab (Z)
                    float delta = UnityEngine.Random.Range(rotationDeltaRange.x, rotationDeltaRange.y);
                    if (UnityEngine.Random.value < 0.5f) delta = -delta;

                    float baseZ = t.GetBaseRotation().eulerAngles.z;
                    t.SetRotationZ(baseZ + delta);
                    break;
                }
        }
    }

    private void OnTileClicked(int clickedIndex)
    {
        if (!isRunning) return;

        if (clickedIndex == oddIndex)
        {
            score += 1;
            if (useTimer) currentTime = Mathf.Min(startTime, currentTime + timeBonusOnCorrect);

            UpdateUI();
            OnScoreChanged?.Invoke(score);

            SetupRound();
        }
        else
        {
            if (failOnWrongClick) Finish();
        }
    }

    private void Finish()
    {
        if (!isRunning) return;
        isRunning = false;
        OnFinished?.Invoke();
    }

    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = score.ToString();

        if (useTimer)
        {
            if (timeText != null) timeText.text = FormatSeconds(currentTime);
            if (timeBarImage != null) timeBarImage.fillAmount = Mathf.Clamp01(currentTime / startTime);
        }
        else
        {
            if (timeText != null) timeText.text = "";
            if (timeBarImage != null) timeBarImage.fillAmount = 1f;
        }
    }

    private static string FormatSeconds(float seconds)
    {
        int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
        int m = s / 60;
        int r = s % 60;
        return $"{m:00}:{r:00}";
    }

    // Helpers
    public int GetScore() => score;
    public bool IsRunning() => isRunning;
}
