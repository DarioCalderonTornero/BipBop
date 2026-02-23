using UnityEngine;
using TMPro;

public class ClassifierScore : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("El texto que muestra los puntos MIENTRAS juegas (opcional)")]
    [SerializeField] private TextMeshProUGUI inGameScoreText;

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
        UpdateScoreUI();
    }

    public void AddPoint(int score)
    {
        CurrentScore += score;
        UpdateScoreUI();
    }

    public int GetScore() => CurrentScore;

    public int GetCoinsEarned() => CurrentScore / 3;

    public void ShowScore() => UpdateScoreUI();

    public void SafeRecordIfNeeded()
    {
        int maxRecord = PlayerPrefs.GetInt("MaxRecordClassifier", 0);
        if (CurrentScore > maxRecord)
        {
            PlayerPrefs.SetInt("MaxRecordClassifier", CurrentScore);
            PlayerPrefs.Save();
        }
    }


    private void UpdateScoreUI()
    {
        if (inGameScoreText != null)
        {
            inGameScoreText.text = CurrentScore.ToString();
        }
    }
}