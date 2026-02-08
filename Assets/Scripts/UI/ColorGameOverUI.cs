using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class ColorGameOverUI : MonoBehaviour
{
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;

    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Localization")]
    [SerializeField] private LocalizedString coinsObtainedLocalized; // "Monedas obtenidas: {0}"

    [SerializeField] private Animator myanimator;

    private void Awake()
    {
        retryButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.ColorScene);
        });

        mainMenuButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.Menu);
        });
    }

    private void Start()
    {
        myanimator = GetComponent<Animator>();
        ColorManager.Instance.OnGameOver += ColorManager_OnGameOver;
    }

    private void ColorManager_OnGameOver(object sender, EventArgs e)
    {
        ShowGameOver();
    }

    private void ShowGameOver()
    {
        int score = ColorGamePuntos.Instance.GetScore();
        int coinsEarned = ColorGamePuntos.Instance.GetCoinsEarned();

        coinText.text = coinsObtainedLocalized.GetLocalizedString(coinsEarned);
        scoreText.text = score.ToString();

        myanimator.SetBool("IsGameOver", true);
    }

    private void OnDestroy()
    {
        if (ColorManager.Instance != null)
            ColorManager.Instance.OnGameOver -= ColorManager_OnGameOver;
    }
}
