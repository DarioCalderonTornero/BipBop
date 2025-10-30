using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ColorGamePuntos : MonoBehaviour
{
    public static ColorGamePuntos Instance { get; private set; }

    public event EventHandler OnColorAddScore;

    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private RectTransform scoreEffectGroup; // <- lo arrastras desde la UI
    private int score = 0;
    private bool isAnimating = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        scoreText.text = "Puntos: 0";
        if (scoreEffectGroup != null)
            scoreEffectGroup.localScale = Vector3.one; // aseguramos tamaño original
    }

    public int GetScore()
    {
        return score;
    }

    public void AddScore()
    {
        OnColorAddScore?.Invoke(this, EventArgs.Empty);
        score++;
        CurrencyManager.Instance.AddCoins(1);
        PlayerLevelManager.Instance.AddXP(5);
        ShowScore();
        PlayScoreEffect(); // <- dispara el zoom
    }

    public void ShowScore()
    {
        scoreText.text = "Puntos: " + score;
    }

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
        if (!isAnimating)
            StartCoroutine(AnimateScoreEffect());
    }

    private IEnumerator AnimateScoreEffect()
    {
        isAnimating = true;

        float duration = 0.05f;
        float halfDuration = duration / 2f;
        Vector3 originalScale = Vector3.one;
        Vector3 zoomScale = new Vector3(1.2f, 1.2f, 1); // pequeño zoom

        float time = 0;
        while (time < halfDuration)
        {
            scoreEffectGroup.localScale = Vector3.Lerp(originalScale, zoomScale, time / halfDuration);
            time += Time.deltaTime;
            yield return null;
        }

        time = 0;
        while (time < halfDuration)
        {
            scoreEffectGroup.localScale = Vector3.Lerp(zoomScale, originalScale, time / halfDuration);
            time += Time.deltaTime;
            yield return null;
        }

        scoreEffectGroup.localScale = originalScale;
        isAnimating = false;
    }
}

