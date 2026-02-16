using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;

public class ClassifierGameOver : MonoBehaviour
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
            SceneLoader.LoadScene(SceneLoader.Scene.ClassificateScene);
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
        ClassifierManager.Instance.OnClassifierGameOver += ClassifierManager_OnClassifierGameOver;
    }

    private void ClassifierManager_OnClassifierGameOver(object sender, System.EventArgs e)
    {
        Debug.Log("OnGeometricGameOver");

        gameObject.SetActive(true);

        int score = ClassifierManager.Instance.GetCurrentScore();   
        int coinsEarned = score / 3;

        if (scoreText != null)
            scoreText.text = score.ToString();

        if (coinText != null)
            //coinText.text = coinsObtainedLocalized.GetLocalizedString(coinsEarned);

        myanimator.SetBool("IsGameOver", true);
        Debug.Log("Classifier Game Over True");
    }

    private void OnDestroy()
    {
        if (ClassifierManager.Instance != null)
            ClassifierManager.Instance.OnClassifierGameOver -= ClassifierManager_OnClassifierGameOver;
    }
}
