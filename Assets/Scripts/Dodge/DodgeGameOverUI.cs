// DodgeGameOverUI.cs
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class DodgeGameOverUI : MonoBehaviour
{
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI coinText;   // "Monedas obtenidas: {0}"
    [SerializeField] private TextMeshProUGUI scoreText;  // SOLO NÚMERO
    [SerializeField] private TextMeshProUGUI gameOverText;

    [SerializeField] private Animator myanimator;

    [Header("Localization")]
    [Tooltip("Smart String con {0}. Ej: 'Monedas obtenidas: {0}' / 'Coins earned: {0}'")]
    [SerializeField] private LocalizedString coinsTextTemplate;

    private int lastScore = 0;
    private int lastCoins = 0;

    private void Awake()
    {
        retryButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.DodgeScene);
        });

        mainMenuButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.Menu);
        });
    }

    private void Start()
    {
        myanimator = GetComponent<Animator>();

        if (DodgeManager.Instance != null)
            DodgeManager.Instance.OnGameOver += DodgeManager_OnGameOver;
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private async void DodgeManager_OnGameOver(object sender, System.EventArgs e)
    {
        if (DodgeManager.Instance == null) return;

        lastScore = DodgeManager.Instance.GetScore();
        lastCoins = lastScore / 3;

        if (scoreText != null)
            scoreText.text = lastScore.ToString();

        await RefreshCoinsText();

        if (myanimator != null)
            myanimator.SetBool("IsGameOver", true);
    }

    private async void OnLocaleChanged(Locale _)
    {
        if (myanimator != null && myanimator.GetBool("IsGameOver"))
            await RefreshCoinsText();
    }

    private async Task RefreshCoinsText()
    {
        if (coinText == null) return;

        if (coinsTextTemplate.IsEmpty)
        {
            coinText.text = "Coins: " + lastCoins;
            return;
        }

        coinsTextTemplate.Arguments = new object[] { lastCoins };

        AsyncOperationHandle<string> handle = coinsTextTemplate.GetLocalizedStringAsync();
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
            coinText.text = handle.Result;
    }

    private void OnDestroy()
    {
        if (DodgeManager.Instance != null)
            DodgeManager.Instance.OnGameOver -= DodgeManager_OnGameOver;
    }
}
