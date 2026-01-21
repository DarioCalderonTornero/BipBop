using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class ColorGamePuntos : MonoBehaviour
{
    public static ColorGamePuntos Instance { get; private set; }
    public static event EventHandler OnColorAddScore;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Tooltip("Elementos que harán el efecto (sin meterlos en un padre).")]
    [SerializeField] private RectTransform[] scoreEffectTargets;

    public int score = 0;

    private bool isAnimating = false;
    private Vector3[] originalScales;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        score = 0;
        UpdateScoreText();

        // Guardar escalas originales
        if (scoreEffectTargets != null && scoreEffectTargets.Length > 0)
        {
            originalScales = new Vector3[scoreEffectTargets.Length];
            for (int i = 0; i < scoreEffectTargets.Length; i++)
            {
                if (scoreEffectTargets[i] != null)
                {
                    originalScales[i] = scoreEffectTargets[i].localScale;
                    scoreEffectTargets[i].localScale = originalScales[i];
                }
            }
        }
    }

    public int GetScore() => score;

    public int GetCoinsEarned() => score / 3;

    private void UpdateScoreText()
    {
        if (scoreText != null)
            scoreText.text = score.ToString();
    }

    public void AddScore()
    {
        OnColorAddScore?.Invoke(this, EventArgs.Empty);
        score++;

        PlayerLevelManager.Instance.AddXP(10);
        UpdateScoreText();

        PlayScoreEffect(); // ✅ Actívalo si quieres el pop
    }

    public void AddScoreRaw(int amount)
    {
        score += amount;
        UpdateScoreText();
    }

    public void ShowScore() => UpdateScoreText();

    public void SafeRecordIfNeeded()
    {
        int maxRecordColor = PlayerPrefs.GetInt("MaxRecordColor", 0);
        if (score > maxRecordColor)
        {
            PlayerPrefs.SetInt("MaxRecordColor", score);
            PlayerPrefs.Save();
        }
    }

    private void PlayScoreEffect()
    {
        if (!isAnimating && scoreEffectTargets != null && scoreEffectTargets.Length > 0)
            StartCoroutine(AnimateScoreEffect());
    }

    private IEnumerator AnimateScoreEffect()
    {
        isAnimating = true;

        float duration = 0.05f;
        float half = duration / 2f;
        float time;

        float mult = 1.2f;

        // Zoom in
        time = 0f;
        while (time < half)
        {
            float t = time / half;
            for (int i = 0; i < scoreEffectTargets.Length; i++)
            {
                var rt = scoreEffectTargets[i];
                if (rt == null) continue;

                Vector3 from = originalScales[i];
                Vector3 to = originalScales[i] * mult;
                rt.localScale = Vector3.Lerp(from, to, t);
            }

            time += Time.deltaTime;
            yield return null;
        }

        // Zoom out
        time = 0f;
        while (time < half)
        {
            float t = time / half;
            for (int i = 0; i < scoreEffectTargets.Length; i++)
            {
                var rt = scoreEffectTargets[i];
                if (rt == null) continue;

                Vector3 from = originalScales[i] * mult;
                Vector3 to = originalScales[i];
                rt.localScale = Vector3.Lerp(from, to, t);
            }

            time += Time.deltaTime;
            yield return null;
        }

        // Reset exacto
        for (int i = 0; i < scoreEffectTargets.Length; i++)
        {
            if (scoreEffectTargets[i] != null)
                scoreEffectTargets[i].localScale = originalScales[i];
        }

        isAnimating = false;
    }
}
