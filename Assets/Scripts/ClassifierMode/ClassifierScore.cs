using UnityEngine;
using TMPro;

public class ClassifierScore : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("El texto que muestra los puntos MIENTRAS juegas (opcional)")]
    [SerializeField] private TextMeshProUGUI inGameScoreText;

    // Propiedad pública para que el Game Over pueda leer los puntos
    public int CurrentScore { get; private set; }

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

    private void UpdateScoreUI()
    {
        if (inGameScoreText != null)
        {
            inGameScoreText.text = CurrentScore.ToString();
        }
    }
}