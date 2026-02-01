using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DifferentManager : MonoBehaviour
{
    public static DifferentManager Instance { get; private set; }

    public event EventHandler OnDifferentGameOver;

    public event Action OnRoundChanged;
    public event Action<int> OnScoreChanged;

    [Header("UI (Opcional)")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Image timeBarImage;
    [SerializeField] private TextMeshProUGUI instructionText;

    [Header("Timer")]
    [SerializeField] private bool useTimer = true;
    [SerializeField] private float startTime = 30f;

    [Header("Grid")]
    [SerializeField] private RectTransform gridRoot;
    [SerializeField] private DifferentTile tilePrefab;
    [SerializeField] private int itemCount = 16;

    [Header("Gameplay")]
    [SerializeField] private bool failOnWrongClick = true;
    [SerializeField] private float timeBonusOnCorrect = 0.75f;

    public enum DifferenceType
    {
        TintColor,       // más oscuro + hue shift
        Rotation,        // delta Z random (max-min)
        Scale,           // multiplicador (max-min)
        MirrorX,         // mirando al lado contrario (Y 180º)
        UpsideDown,      // boca abajo (Z +180)
        Sprite           // opcional (si quieres mantenerlo)
    }

    [Header("Patterns (Opcional)")]
    [SerializeField] private Sprite[] patternSprites;
    [SerializeField] private Color[] patternColors;

    [Header("Odd Tint Colors (para TintColor)")]
    [SerializeField] private Color[] oddTintColors;

    [Header("Allowed Differences")]
    [SerializeField] private bool allowColor = true;
    [SerializeField] private bool allowRotation = true;
    [SerializeField] private bool allowScale = true;
    [SerializeField] private bool allowSprite = true;

    [Header("Tuning")]
    [SerializeField] private Vector2 rotationDeltaRange = new Vector2(12f, 60f);
    [SerializeField] private Vector2 oddScaleMultiplierRange = new Vector2(0.78f, 1.28f);
    [SerializeField] private float minScaleDeltaFromOne = 0.10f;

    [Header("Different moves")]
    [SerializeField] private float oddSwapInterval = 1.5f;
    [SerializeField] private float oddSwapAnimDuration = 0.12f; // suave pero rápida

    private readonly List<DifferentTile> tiles = new List<DifferentTile>();
    private int oddIndex = -1;
    private DifferenceType currentType;

    private int score = 0;
    private float currentTime;
    private bool isRunning;

    // estado del patrón actual (para re-aplicar odd al cambiar de índice)
    private Sprite currentBaseSprite;
    private Color currentBaseColor;

    private Color currentOddColor;
    private Sprite currentOddSprite;
    private float currentOddRotDelta;
    private float currentOddScaleMul;

    private Coroutine oddSwapRoutine;

    private Color currentOddTintColor; // para TintColor

    [SerializeField] private int scoreToEnableOddSwap = 50;

    [Header("Round Transition FX")]
    [SerializeField] private bool playRoundTransitionOnCorrect = true;
    [SerializeField] private float roundTransitionDuration = 0.16f;
    [SerializeField] private float roundTransitionMinScaleMul = 0.08f; // hacia dentro

    private bool isTransitioning;
    private Coroutine transitionRoutine;

    public enum RoundMode
    {
        FindDifferent,
        FindSingleton
    }

    [Header("Modes")]
    [SerializeField] private bool allowSingletonMode = true;
    [SerializeField, Range(0f, 1f)] private float singletonModeChance = 0.20f;

    private RoundMode currentMode = RoundMode.FindDifferent;

    [Header("Economy / Meta")]
    [SerializeField] private string playFabStatName = "DifferentScore";
    [SerializeField] private string recordPlayerPrefsKey = "MaxRecordDifferent";
    [SerializeField] private int xpPerPoint = 10;
    [SerializeField] private int coinsDivisor = 3; // monedas = score / 3 (como otros)

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
        isRunning = false; 

        BuildGrid();
        UpdateUI();
        SetupRound();
    }

    public void ResumeGameplay()
    {
        if (hasEnded) return;
        isRunning = true;
    }

    public void PauseGameplay()
    {
        isRunning = false;
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
        if (hasEnded) return;
        if (tiles.Count == 0) return;

        bool doSingleton = allowSingletonMode
                           && patternSprites != null
                           && patternSprites.Length >= 3
                           && UnityEngine.Random.value < singletonModeChance;

        if (doSingleton)
        {
            currentMode = RoundMode.FindSingleton;
            SetupSingletonRound();
            OnRoundChanged?.Invoke();
            return;
        }

        currentMode = RoundMode.FindDifferent;

        if (instructionText != null)
            instructionText.text = "¡Toca el diferente!";

        currentType = PickAllowedType();

        // Base pattern
        currentBaseSprite = (patternSprites != null && patternSprites.Length > 0)
            ? patternSprites[UnityEngine.Random.Range(0, patternSprites.Length)]
            : null;

        currentBaseColor = (patternColors != null && patternColors.Length > 0)
            ? patternColors[UnityEngine.Random.Range(0, patternColors.Length)]
            : Color.white;

        // Aplica base a todos
        for (int i = 0; i < tiles.Count; i++)
            tiles[i].ApplyBase(currentBaseSprite, currentBaseColor);

        oddIndex = UnityEngine.Random.Range(0, tiles.Count);
        PrepareOddDelta();
        ApplyOddInstant(oddIndex);

        RestartOddSwapRoutine(); // seguirá respetando score>=50
        OnRoundChanged?.Invoke();
    }

    private void SetupSingletonRound()
    {
        if (instructionText != null)
            instructionText.text = "Busca el sprite solitario";

        // Color base (si quieres, o siempre blanco)
        currentBaseColor = (patternColors != null && patternColors.Length > 0)
            ? patternColors[UnityEngine.Random.Range(0, patternColors.Length)]
            : Color.white;

        // Elegir sprite único
        Sprite unique = patternSprites[UnityEngine.Random.Range(0, patternSprites.Length)];

        // Elegir dónde va el único
        oddIndex = UnityEngine.Random.Range(0, tiles.Count);

        // Generar lista de sprites para todos los tiles cumpliendo:
        // - oddIndex: unique (1 vez)
        // - el resto: cada sprite aparece al menos 2 veces
        Sprite[] assigned = GenerateSingletonDistribution(unique, tiles.Count);

        // Aplicar
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].ApplyBase(assigned[i], currentBaseColor);
        }

        // En singleton no tiene sentido el swap (a menos que regeneres distribución)
        if (oddSwapRoutine != null)
        {
            StopCoroutine(oddSwapRoutine);
            oddSwapRoutine = null;
        }
    }

    private Sprite[] GenerateSingletonDistribution(Sprite unique, int count)
    {
        Sprite[] result = new Sprite[count];

        // Colocamos el único
        result[oddIndex] = unique;

        // índices libres
        List<int> free = new List<int>(count - 1);
        for (int i = 0; i < count; i++)
            if (i != oddIndex) free.Add(i);

        // Escoger cuántos sprites “repetidos” tendremos
        int repeatedTypes = Mathf.Clamp(UnityEngine.Random.Range(3, 7), 3, Mathf.Min(7, free.Count / 2));

        // pool sin el unique
        List<Sprite> pool = new List<Sprite>(patternSprites.Length);
        for (int i = 0; i < patternSprites.Length; i++)
            if (patternSprites[i] != null && patternSprites[i] != unique)
                pool.Add(patternSprites[i]);

        // Barajar pool
        for (int i = 0; i < pool.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        repeatedTypes = Mathf.Min(repeatedTypes, pool.Count);

        // Cada tipo al menos 2
        List<Sprite> chosen = pool.GetRange(0, repeatedTypes);

        // Primero metemos 2 de cada
        List<Sprite> bag = new List<Sprite>(free.Count);
        for (int i = 0; i < chosen.Count; i++)
        {
            bag.Add(chosen[i]);
            bag.Add(chosen[i]);
        }

        // Rellenar el resto con repeticiones de esos mismos
        while (bag.Count < free.Count)
            bag.Add(chosen[UnityEngine.Random.Range(0, chosen.Count)]);

        // Barajar bag
        for (int i = 0; i < bag.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, bag.Count);
            (bag[i], bag[j]) = (bag[j], bag[i]);
        }

        // Asignar
        for (int k = 0; k < free.Count; k++)
            result[free[k]] = bag[k];

        return result;
    }

    private void RestartOddSwapRoutine()
    {
        // En singleton no movemos el objetivo (si quieres moverlo, habría que regenerar distribución)
        if (currentMode == RoundMode.FindSingleton)
        {
            if (oddSwapRoutine != null)
            {
                StopCoroutine(oddSwapRoutine);
                oddSwapRoutine = null;
            }
            return;
        }

        if (score < scoreToEnableOddSwap)
        {
            if (oddSwapRoutine != null)
            {
                StopCoroutine(oddSwapRoutine);
                oddSwapRoutine = null;
            }
            return;
        }

        if (oddSwapRoutine != null)
            StopCoroutine(oddSwapRoutine);

        oddSwapRoutine = StartCoroutine(OddSwapLoop());
    }

    private IEnumerator OddSwapLoop()
    {
        while (isRunning)
        {
            yield return new WaitForSeconds(oddSwapInterval);

            if (!isRunning) yield break;
            if (tiles.Count <= 1) continue;

            int newIndex = oddIndex;
            int guard = 0;
            while (newIndex == oddIndex && guard++ < 20)
                newIndex = UnityEngine.Random.Range(0, tiles.Count);

            SwapOddTo(newIndex);
        }
    }

    private void SwapOddTo(int newIndex)
    {
        if (newIndex == oddIndex) return;

        int oldIndex = oddIndex;
        oddIndex = newIndex;

        // 1) El viejo odd vuelve a base, animado
        AnimateToBase(oldIndex);

        // 2) El nuevo índice se convierte en odd, animado (con MISMA diferencia)
        AnimateToOdd(newIndex);
    }

    private DifferenceType PickAllowedType()
    {
        var allowed = new List<DifferenceType>(6);

        // Tint siempre es posible aunque no tengas patternColors
        if (allowColor) allowed.Add(DifferenceType.TintColor);

        if (allowRotation) allowed.Add(DifferenceType.Rotation);
        if (allowScale) allowed.Add(DifferenceType.Scale);

        // Reutilizo flags existentes para no crear nuevos, si quieres lo separamos luego:
        if (allowRotation) allowed.Add(DifferenceType.MirrorX);
        if (allowRotation) allowed.Add(DifferenceType.UpsideDown);

        if (allowSprite && patternSprites != null && patternSprites.Length >= 2)
            allowed.Add(DifferenceType.Sprite);

        return allowed.Count == 0 ? DifferenceType.Rotation : allowed[UnityEngine.Random.Range(0, allowed.Count)];
    }

    private void PrepareOddDelta()
    {
        switch (currentType)
        {
            case DifferenceType.TintColor:
                {
                    if (oddTintColors != null && oddTintColors.Length > 0)
                    {
                        Color picked = currentBaseColor;
                        int guard = 0;
                        while (picked == currentBaseColor && guard++ < 20)
                            picked = oddTintColors[UnityEngine.Random.Range(0, oddTintColors.Length)];

                        currentOddTintColor = picked;
                    }
                    else
                    {
                        currentOddTintColor = GenerateTintedColor(currentBaseColor);
                    }
                    break;
                }

            case DifferenceType.Sprite:
                {
                    Sprite odd = currentBaseSprite;
                    int guard = 0;
                    while (odd == currentBaseSprite && guard++ < 20)
                        odd = patternSprites[UnityEngine.Random.Range(0, patternSprites.Length)];
                    currentOddSprite = odd;
                    break;
                }

            case DifferenceType.Scale:
                {
                    float m = 1f;
                    int guard = 0;
                    while (Mathf.Abs(m - 1f) < minScaleDeltaFromOne && guard++ < 30)
                        m = UnityEngine.Random.Range(oddScaleMultiplierRange.x, oddScaleMultiplierRange.y);
                    currentOddScaleMul = m;
                    break;
                }

            case DifferenceType.UpsideDown:
                // No hace falta delta, es 180 fijo
                break;

            case DifferenceType.MirrorX:
                // No hace falta delta, es Y 180 fijo
                break;

            case DifferenceType.Rotation:
            default:
                {
                    float delta = UnityEngine.Random.Range(rotationDeltaRange.x, rotationDeltaRange.y);
                    if (UnityEngine.Random.value < 0.5f) delta = -delta;
                    currentOddRotDelta = delta;
                    break;
                }
        }
    }

    private Color GenerateTintedColor(Color baseColor)
    {
        Color.RGBToHSV(baseColor, out float h, out float s, out float v);

        // oscurecer un poco
        v = Mathf.Clamp01(v - UnityEngine.Random.Range(0.10f, 0.20f));

        // hue shift: rojizo / verdoso / azulado
        float[] shifts = { -0.06f, -0.03f, 0.03f, 0.06f };
        h = Mathf.Repeat(h + shifts[UnityEngine.Random.Range(0, shifts.Length)], 1f);

        // un pelín más saturado para que se note
        s = Mathf.Clamp01(s + UnityEngine.Random.Range(0.05f, 0.15f));

        return Color.HSVToRGB(h, s, v);
    }

    private void ApplyOddInstant(int index)
    {
        var t = tiles[index];

        switch (currentType)
        {
            case DifferenceType.TintColor:
                t.SetColor(currentOddTintColor);
                break;

            case DifferenceType.Sprite:
                t.SetSprite(currentOddSprite);
                break;

            case DifferenceType.Scale:
                t.SetScale(t.GetBaseScale() * currentOddScaleMul);
                break;

            case DifferenceType.MirrorX:
                {
                    Vector3 s = t.GetBaseScale();
                    s.x = -Mathf.Abs(s.x);   // fuerza flip horizontal
                    t.SetScale(s);
                    break;
                }

            case DifferenceType.UpsideDown:
                // Z + 180 (respeta la rotación base del prefab)
                t.SetRotation(t.GetBaseRotation() * Quaternion.Euler(0f, 0f, 180f));
                break;

            case DifferenceType.Rotation:
            default:
                {
                    float baseZ = t.GetBaseRotation().eulerAngles.z;
                    t.SetRotationZ(baseZ + currentOddRotDelta);
                    break;
                }
        }
    }

    private void AnimateToBase(int index)
    {
        if (index < 0 || index >= tiles.Count) return;

        var t = tiles[index];

        // SPRITE (solo animamos si el tipo actual es Sprite)
        if (currentType == DifferenceType.Sprite)
            t.AnimateToSprite(currentBaseSprite, oddSwapAnimDuration);
        else
            t.SetSprite(currentBaseSprite);

        // COLOR (solo animamos si el tipo actual es TintColor)
        if (currentType == DifferenceType.TintColor)
            t.AnimateToColor(currentBaseColor, oddSwapAnimDuration);
        else
            t.SetColor(currentBaseColor);

        // SCALE (solo animamos si el tipo actual es Scale)
        if (currentType == DifferenceType.Scale)
            t.AnimateToScale(t.GetBaseScale(), oddSwapAnimDuration);
        else
            t.SetScale(t.GetBaseScale());

        // ROTACIÓN
        if (currentType == DifferenceType.Rotation)
        {
            float baseZ = t.GetBaseRotation().eulerAngles.z;
            t.AnimateToRotationZ(baseZ, oddSwapAnimDuration);
        }
        else if (currentType == DifferenceType.MirrorX)
        {
            t.AnimateToScale(t.GetBaseScale(), oddSwapAnimDuration);
        }
        else
        {
            t.SetRotation(t.GetBaseRotation());
        }
    }

    private void AnimateToOdd(int index)
    {
        if (index < 0 || index >= tiles.Count) return;

        var t = tiles[index];

        switch (currentType)
        {
            case DifferenceType.TintColor:
                t.SetColor(currentBaseColor);
                t.AnimateToColor(currentOddTintColor, oddSwapAnimDuration);
                break;

            case DifferenceType.Sprite:
                t.SetSprite(currentBaseSprite);
                t.AnimateToSprite(currentOddSprite, oddSwapAnimDuration);
                break;

            case DifferenceType.Scale:
                t.SetScale(t.GetBaseScale());
                t.AnimateToScale(t.GetBaseScale() * currentOddScaleMul, oddSwapAnimDuration);
                break;

            case DifferenceType.MirrorX:
                {
                    // partir de base
                    t.SetScale(t.GetBaseScale());

                    // animar al flip
                    Vector3 target = t.GetBaseScale();
                    target.x = -Mathf.Abs(target.x);
                    t.AnimateToScale(target, oddSwapAnimDuration);
                    break;
                }

            case DifferenceType.UpsideDown:
                t.SetRotation(t.GetBaseRotation());
                t.AnimateToRotation(t.GetBaseRotation() * Quaternion.Euler(0f, 0f, 180f), oddSwapAnimDuration);
                break;

            case DifferenceType.Rotation:
            default:
                {
                    float baseZ = t.GetBaseRotation().eulerAngles.z;
                    t.SetRotationZ(baseZ);
                    t.AnimateToRotationZ(baseZ + currentOddRotDelta, oddSwapAnimDuration);
                    break;
                }
        }
    }

    private void OnTileClicked(int clickedIndex)
    {
        if (!isRunning) return;
        if (isTransitioning) return; // evita clicks durante el efecto

        if (clickedIndex == oddIndex)
        {
            // POP del correcto
            tiles[clickedIndex].Pop(0.10f, 1.20f);

            score += 1;

            if (score == scoreToEnableOddSwap)
                RestartOddSwapRoutine();

            if (useTimer)
                currentTime = Mathf.Min(startTime, currentTime + timeBonusOnCorrect);

            UpdateUI();
            OnScoreChanged?.Invoke(score);

            // En vez de SetupRound directo, hacemos la transición
            if (playRoundTransitionOnCorrect)
            {
                if (transitionRoutine != null) StopCoroutine(transitionRoutine);
                transitionRoutine = StartCoroutine(RoundTransitionRoutine());
            }
            else
            {
                SetupRound();
            }
        }
        else
        {
            if (failOnWrongClick)
                Finish();
        }
    }

    private IEnumerator RoundTransitionRoutine()
    {
        isTransitioning = true;

        // Guardar escala actual del SPRITE (no del tile)
        Vector3[] startSpriteScales = new Vector3[tiles.Count];
        for (int i = 0; i < tiles.Count; i++)
        {
            RectTransform imgRt = tiles[i].GetImageRect();
            startSpriteScales[i] = imgRt != null ? imgRt.localScale : Vector3.one;
        }

        float half = Mathf.Max(0.01f, roundTransitionDuration * 0.5f);

        // 1) shrink sprites
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float u = Smooth01(t / half);
            float m = Mathf.Lerp(1f, roundTransitionMinScaleMul, u);

            for (int i = 0; i < tiles.Count; i++)
            {
                RectTransform imgRt = tiles[i].GetImageRect();
                if (imgRt == null) continue;
                imgRt.localScale = startSpriteScales[i] * m;
            }

            yield return null;
        }

        // fijar mínimo
        for (int i = 0; i < tiles.Count; i++)
        {
            RectTransform imgRt = tiles[i].GetImageRect();
            if (imgRt == null) continue;
            imgRt.localScale = startSpriteScales[i] * roundTransitionMinScaleMul;
        }

        // 2) cambiar patrón aquí
        SetupRound();

        // Capturar escalas del nuevo patrón (ya correctas) y arrancar desde pequeño
        Vector3[] targetSpriteScales = new Vector3[tiles.Count];
        for (int i = 0; i < tiles.Count; i++)
        {
            RectTransform imgRt = tiles[i].GetImageRect();
            if (imgRt == null)
            {
                targetSpriteScales[i] = Vector3.one;
                continue;
            }

            targetSpriteScales[i] = imgRt.localScale; // debería ser baseScale o baseScale con odd
            imgRt.localScale = targetSpriteScales[i] * roundTransitionMinScaleMul;
        }

        // 3) expand sprites
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float u = Smooth01(t / half);
            float m = Mathf.Lerp(roundTransitionMinScaleMul, 1f, u);

            for (int i = 0; i < tiles.Count; i++)
            {
                RectTransform imgRt = tiles[i].GetImageRect();
                if (imgRt == null) continue;
                imgRt.localScale = targetSpriteScales[i] * m;
            }

            yield return null;
        }

        // fijar final exacto
        for (int i = 0; i < tiles.Count; i++)
        {
            RectTransform imgRt = tiles[i].GetImageRect();
            if (imgRt == null) continue;
            imgRt.localScale = targetSpriteScales[i];
        }

        isTransitioning = false;
        transitionRoutine = null;
    }

    private float Smooth01(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    private bool hasEnded = false;

    private void Finish()
    {
        if (!isRunning || hasEnded) return;

        isRunning = false;
        hasEnded = true;

        if (oddSwapRoutine != null)
        {
            StopCoroutine(oddSwapRoutine);
            oddSwapRoutine = null;
        }
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        EndGame();
    }

    private void EndGame()
    {
        OnDifferentGameOver?.Invoke(this, EventArgs.Empty);
        // 1) Récord local
        SaveRecordIfNeeded();

        // 2) Subir PlayFab
        if (PlayFabLoginManager.Instance != null &&
            PlayFabLoginManager.Instance.IsLoggedIn &&
            PlayFabScoreManager.Instance != null)
        {
            PlayFabScoreManager.Instance.SubmitScore(playFabStatName, score);
        }

        // 3) Monedas
        int coinsEarned = Mathf.Max(0, score / Mathf.Max(1, coinsDivisor));

        CoinsRewardUI rewardUI = FindObjectOfType<CoinsRewardUI>(true);
        if (rewardUI != null)
            rewardUI.ShowReward(coinsEarned);
        else if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.AddCoins(coinsEarned);

        // 4) XP
        if (PlayerLevelManager.Instance != null)
            PlayerLevelManager.Instance.AddXP(score * Mathf.Max(0, xpPerPoint));

        // 5) Misiones diarias (mínimo igual que otros minijuegos)
        if (DailyMissionManager.Instance != null)
        {
            DailyMissionManager.Instance.AddProgress("juega_1_partida", 1);
            DailyMissionManager.Instance.AddProgress("juega_3_partidas", 1);
            DailyMissionManager.Instance.AddProgress("juega_8_partidas", 1);
            DailyMissionManager.Instance.AddProgress("juega_10_partidas", 1);

            // específicas (si quieres)
            // DailyMissionManager.Instance.AddProgress("juega_3_partidas_diferente", 1);

            if (score >= 10) DailyMissionManager.Instance.AddProgress("consigue_10_puntos_diferente", 1);
            if (score >= 50) DailyMissionManager.Instance.AddProgress("consigue_50_puntos_diferente", 1);
        }

#if UNITY_ANDROID || UNITY_IOS
        Haptics.TryVibrate();
#endif
    }

    private void SaveRecordIfNeeded()
    {
        int currentRecord = PlayerPrefs.GetInt(recordPlayerPrefsKey, 0);
        if (score > currentRecord)
        {
            PlayerPrefs.SetInt(recordPlayerPrefsKey, score);
            PlayerPrefs.Save();
        }
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = score.ToString();

        if (timeBarImage != null)
        {
            if (useTimer)
                timeBarImage.fillAmount = Mathf.Clamp01(currentTime / startTime);
            else
                timeBarImage.fillAmount = 1f;
        }
    }

    private static string FormatSeconds(float seconds)
    {
        int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
        int m = s / 60;
        int r = s % 60;
        return $"{m:00}:{r:00}";
    }

    public int GetScore() => score;
    public bool IsRunning() => isRunning;
}
