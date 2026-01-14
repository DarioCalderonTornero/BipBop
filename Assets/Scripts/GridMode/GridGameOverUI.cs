using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;

public class GridGameOverUI : MonoBehaviour
{
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Image backGround;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI scoreText; // ✅ NUEVO: solo número
    [SerializeField] private TextMeshProUGUI gameOverText;

    [Header("Localization")]
    [SerializeField] private LocalizedString coinsObtainedLocalized; // "Monedas obtenidas: {0}"

    [SerializeField] private Animator myanimator;

    private void Awake()
    {
        retryButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.GridScene);
        });

        mainMenuButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.Menu);
        });
    }

    void Start()
    {
        myanimator = GetComponent<Animator>();
        GridGameManager.Instance.OnGameOver += GridGameManager_OnGameOver;
    }

    private void GridGameManager_OnGameOver(object sender, System.EventArgs e)
    {

        int score = GridGameManager.Instance.GetScore();
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
        if (GridGameManager.Instance != null)
            GridGameManager.Instance.OnGameOver -= GridGameManager_OnGameOver;
    }
}
