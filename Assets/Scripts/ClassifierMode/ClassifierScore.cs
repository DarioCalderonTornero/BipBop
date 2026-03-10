using UnityEngine;
using TMPro;

public class ClassifierScore : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("El texto que muestra los puntos MIENTRAS juegas (opcional)")]
    [SerializeField] private TextMeshProUGUI inGameScoreText;

    [Header("Animación")]
    public Color scoreHighlightColor = Color.yellow; // <-- NUEVO: Color del destello

    public static ClassifierScore Instance { get; private set; }

    // Propiedad pública para que el Game Over pueda leer los puntos
    public int CurrentScore { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        CurrentScore = 0;
        // Le pasamos "false" para que no haga la animación al arrancar el juego
        UpdateScoreUI(false);
    }

    public void AddPoint(int score)
    {
        CurrentScore += score;
        PlayerLevelManager.Instance.AddXP(5);
        Haptics.TryVibrate();
        // Le pasamos "true" para que haga el POP al sumar puntos
        UpdateScoreUI(true);
    }

    public int GetScore() => CurrentScore;

    public int GetCoinsEarned() => CurrentScore / 3;

    public void ShowScore() => UpdateScoreUI(false);

    public void SafeRecordIfNeeded()
    {
        int maxRecord = PlayerPrefs.GetInt("MaxRecordClassifier", 0);
        if (CurrentScore > maxRecord)
        {
            PlayerPrefs.SetInt("MaxRecordClassifier", CurrentScore);
            PlayerPrefs.Save();
        }
    }

    // <-- NUEVO: Añadimos un parámetro bool para decidir si animamos o no
    private void UpdateScoreUI(bool animate)
    {
        if (inGameScoreText != null)
        {
            inGameScoreText.text = CurrentScore.ToString();

            // --- Efecto "Pop" y destello de color ---
            if (animate && CurrentScore > 0)
            {
                LeanTween.cancel(inGameScoreText.gameObject);

                // 1. Efecto de Escala (Pop)
                inGameScoreText.transform.localScale = Vector3.one;
                LeanTween.scale(inGameScoreText.gameObject, Vector3.one * 1.4f, 0.2f)
                    .setEase(LeanTweenType.easeOutBack)
                    .setLoopPingPong(1);

                // 2. Efecto de Color (Compatible con TextMeshPro)
                inGameScoreText.color = scoreHighlightColor;

                LeanTween.value(inGameScoreText.gameObject, scoreHighlightColor, Color.white, 0.4f)
                    .setEase(LeanTweenType.easeOutQuad)
                    .setOnUpdate((Color colorAnimado) =>
                    {
                        inGameScoreText.color = colorAnimado;
                    });
            }
        }
    }
}