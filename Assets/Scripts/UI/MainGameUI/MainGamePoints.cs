using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class MainGamePoints : MonoBehaviour
{
    public static MainGamePoints Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Localization")]
    public LocalizedString scoreLabel;      // Smart string: "Puntos: {0}" / "Points: {0}"

    private int score = -1;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Texto inicial localizado
        score = 0;
        UpdateLocalizedScore();
    }

    public int GetScore()
    {
        return score;
    }

    public int AddScore()
    {
        score++;
        UpdateLocalizedScore();
        return score;
    }

    public void ShowScore()
    {
        UpdateLocalizedScore();
    }

    private void UpdateLocalizedScore()
    {
        scoreText.text = scoreLabel.GetLocalizedString(score);
    }

    public void SafeRecordIfNeeded()
    {
        int currentRecord = PlayerPrefs.GetInt("MaxRecord", 0);

        if (score > currentRecord)
        {
            PlayerPrefs.SetInt("MaxRecord", score);
            PlayerPrefs.Save();
        }
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale locale)
    {
        // Al cambiar idioma, actualizar la línea de puntuación
        UpdateLocalizedScore();
    }
}
