using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class DifferentGameOverUI : MonoBehaviour
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
            SceneLoader.LoadScene(SceneLoader.Scene.DifferentScene);
        });

        mainMenuButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.Menu);
        });
    }

    void Start()
    {
        myanimator = GetComponent<Animator>();
        DifferentManager.Instance.OnDifferentGameOver += DifferentManager_OnDifferentGameOver;
    }

    private void DifferentManager_OnDifferentGameOver(object sender, System.EventArgs e)
    {
        int score = DifferentManager.Instance.GetScore();
        int coinsEarned = score / 3;

        if (scoreText != null)
            scoreText.text = score.ToString();

        if (coinText != null)
            coinText.text = coinsObtainedLocalized.GetLocalizedString(coinsEarned);

        myanimator.SetBool("IsGameOver", true);
    }

    private void OnDestroy()
    {
        if (DifferentManager.Instance != null)
        {
            DifferentManager.Instance.OnDifferentGameOver -= DifferentManager_OnDifferentGameOver;
        }
    }
}
