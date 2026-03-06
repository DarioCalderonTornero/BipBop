using System;
using UnityEngine;
using TMPro;

public class ColorGamePuntos : MonoBehaviour
{
    public static ColorGamePuntos Instance { get; private set; }
    public static event EventHandler OnColorAddScore;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Animación")]
    public Color scoreHighlightColor = Color.yellow; // <-- NUEVO: Color del destello

    public int score = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        score = 0;
        // Le pasamos "false" para que no haga la animación al arrancar
        UpdateScoreText(false);
    }

    public int GetScore() => score;

    public int GetCoinsEarned() => score / 3;

    public void AddScore()
    {
        OnColorAddScore?.Invoke(this, EventArgs.Empty);
        score++;

        PlayerLevelManager.Instance.AddXP(10);
        // Le pasamos "true" para que haga el Pop y cambie de color
        UpdateScoreText(true);
    }

    public void AddScoreRaw(int amount)
    {
        score += amount;
        UpdateScoreText(true);
    }

    public void ShowScore() => UpdateScoreText(false);

    public void SafeRecordIfNeeded()
    {
        int maxRecordColor = PlayerPrefs.GetInt("MaxRecordColor", 0);
        if (score > maxRecordColor)
        {
            PlayerPrefs.SetInt("MaxRecordColor", score);
            PlayerPrefs.Save();
        }
    }

    // <-- NUEVO: Método actualizado con LeanTween para tamaño y color
    private void UpdateScoreText(bool animate)
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();

            if (animate && score > 0)
            {
                LeanTween.cancel(scoreText.gameObject);

                // 1. Efecto de Escala (Pop)
                scoreText.transform.localScale = Vector3.one;
                LeanTween.scale(scoreText.gameObject, Vector3.one * 1.4f, 0.2f)
                    .setEase(LeanTweenType.easeOutBack)
                    .setLoopPingPong(1);

                // 2. Efecto de Color (Compatible con TextMeshPro)
                scoreText.color = scoreHighlightColor;

                LeanTween.value(scoreText.gameObject, scoreHighlightColor, Color.white, 0.4f)
                    .setEase(LeanTweenType.easeOutQuad)
                    .setOnUpdate((Color colorAnimado) =>
                    {
                        scoreText.color = colorAnimado;
                    });
            }
        }
    }
}