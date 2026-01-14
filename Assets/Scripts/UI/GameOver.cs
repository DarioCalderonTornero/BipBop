using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class GameOver : MonoBehaviour
{
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Image backGround;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI scoreText;   // ✅ NUEVO: solo número
    [SerializeField] private TextMeshProUGUI gameOverText;

    [Header("Localization")]
    [SerializeField] private LocalizedString coinsObtainedLocalized; // "Monedas obtenidas: {0}"

    [SerializeField] private Animator myanimator;

    [Header("Transition")]
    [SerializeField] private string mainMenuSceneName = "Menu";
    [SerializeField] private LocalizedString mainMenuTransitionLabel;

    private void Awake()
    {
        retryButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.GameScene);
        });

        mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    private void Start()
    {
        myanimator = GetComponent<Animator>();
        LogicaJuego.Instance.OnGameOver += LogicaPuntos_OnGameOver;
    }

    private void LogicaPuntos_OnGameOver(object sender, System.EventArgs e)
    {
        Debug.Log("Show");

        // ✅ SCORE: solo número de esta partida
        if (scoreText != null && MainGamePoints.Instance != null)
            scoreText.text = MainGamePoints.Instance.GetScore().ToString();

        // ✅ COINS: "Monedas obtenidas: X" (localizable)
        if (coinText != null && coinsObtainedLocalized != null && MainGamePoints.Instance != null)
        {
            int coinsEarned = MainGamePoints.Instance.GetCoinsEarned(); // si ya lo tienes (1 cada 3)
            // Si todavía NO tienes GetCoinsEarned(), usa temporalmente:
            // int coinsEarned = MainGamePoints.Instance.GetScore();

            coinText.text = coinsObtainedLocalized.GetLocalizedString(coinsEarned);
        }

        myanimator.SetBool("IsGameOver", true);
    }

    private void OnMainMenuClicked()
    {
        if (TransitionScript.Instance != null)
        {
            string label = mainMenuTransitionLabel.GetLocalizedString();
            TransitionScript.Instance.TransitionToScene(mainMenuSceneName, label);
        }
        else
        {
            SceneLoader.LoadScene(SceneLoader.Scene.Menu);
        }
    }

    private void OnDestroy()
    {
        if (LogicaJuego.Instance != null)
            LogicaJuego.Instance.OnGameOver -= LogicaPuntos_OnGameOver;
    }
}
