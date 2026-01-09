using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ColorGamePuntos : MonoBehaviour
{
    public static ColorGamePuntos Instance { get; private set; }

    public static event EventHandler OnColorAddScore;

    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private RectTransform scoreEffectGroup;
    public int score = 0;
    private bool isAnimating = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        score = 0;
        UpdateScoreText();

        if (scoreEffectGroup != null)
            scoreEffectGroup.localScale = Vector3.one;
    }

    public int GetScore()
    {
        return score;
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
            scoreText.text = score.ToString();
    }

    public void AddScore()
    {
        OnColorAddScore?.Invoke(this, EventArgs.Empty);
        score++;
        CurrencyManager.Instance.AddCoins(1);
        PlayerLevelManager.Instance.AddXP(5);
        UpdateScoreText();
        // PlayScoreEffect();
    }

    public void AddScoreRaw(int amount)
    {
        score += amount;
        UpdateScoreText();
    }

    public void ShowScore()
    {
        UpdateScoreText();
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
        Vector3 zoomScale = new Vector3(1.2f, 1.2f, 1);

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
