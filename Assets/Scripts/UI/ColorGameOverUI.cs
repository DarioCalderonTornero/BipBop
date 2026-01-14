using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ColorGameOverUI : MonoBehaviour
{

    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Image backGround;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI gameOverText;

    [Header("Localization")]
    [SerializeField] private LocalizedString coinsObtainedLocalized; // "Monedas obtenidas: {0}"

    [SerializeField] private Animator myanimator;

    [SerializeField] private ColorVideoGameOver videoGameOver;
    [SerializeField] private AdButtonFill adButtonFill;

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

    void Start()
    {
        myanimator = GetComponent<Animator>();
        ColorManager.Instance.OnGameOver += ColorManager_OnGameOver;
        adButtonFill.OnHideOffer += AdButtonFill_OnHideOffer;
    }

    private void AdButtonFill_OnHideOffer(object sender, System.EventArgs e)
    {
        ColorManager.Instance.SetDeathType(ColorManager.DeathType.GameOver);
    }

    private void ColorManager_OnGameOver(object sender, System.EventArgs e)
    {
        ShowGameOver();
    }

    private void ShowGameOver()
    {
        int score = ColorGamePuntos.Instance.GetScore();
        int coinsEarned = ColorGamePuntos.Instance.GetCoinsEarned();

        // Monedas obtenidas: X (localizable)
        coinText.text = coinsObtainedLocalized.GetLocalizedString(coinsEarned);

        // Score: solo el número
        scoreText.text = score.ToString();

        myanimator.SetBool("IsGameOver", true);
    }

    private void OnDestroy()
    {
        if (ColorManager.Instance != null)
            ColorManager.Instance.OnGameOver -= ColorManager_OnGameOver;
    }
}

