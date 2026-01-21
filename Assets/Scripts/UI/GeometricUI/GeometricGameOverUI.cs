using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;

public class GeometricGameOverUI : MonoBehaviour
{
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Image backGround;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI scoreText; 
    [SerializeField] private TextMeshProUGUI gameOverText;

    [Header("Localization")]
    [SerializeField] private LocalizedString coinsObtainedLocalized;

    [SerializeField] private Animator myanimator;

    private void Awake()
    {
        retryButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.GeometricScene);
        });

        mainMenuButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.Menu);
            Time.timeScale = 1.0f;
        });
    }

    void Start()
    {
        myanimator = GetComponent<Animator>();
        GeometricModeManager.Instance.OnGameOver += GeometricModeManager_OnGameOver;
    }

    private void GeometricModeManager_OnGameOver(object sender, System.EventArgs e)
    {
        Debug.Log("OnGeometricGameOver");

        int score = GeometricModeManager.Instance.GetScore();
        int coinsEarned = score / 3;

        // ✅ SCORE: solo número
        if (scoreText != null)
            scoreText.text = score.ToString();

        // ✅ COINS: localizable "Monedas obtenidas: X"
        if (coinText != null)
            coinText.text = coinsObtainedLocalized.GetLocalizedString(coinsEarned);

        myanimator.SetBool("IsGameOver", true);
    }

    private void OnDestroy()
    {
        if (GeometricModeManager.Instance != null)
            GeometricModeManager.Instance.OnGameOver -= GeometricModeManager_OnGameOver;
    }
}
