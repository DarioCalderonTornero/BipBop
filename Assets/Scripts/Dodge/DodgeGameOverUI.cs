using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;

public class DodgeGameOverUI : MonoBehaviour
{
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Image backGround;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI gameOverText;

    [Header("Localization")]
    [Tooltip("Smart String con {0}. Ej: 'Coins: {0}'")]
    [SerializeField] private LocalizedString coinsTextTemplate;

    [SerializeField] private Animator myanimator;

    private int lastScore = 0;

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

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    void Start()
    {
        myanimator = GetComponent<Animator>();
        DodgeManager.Instance.OnGameOver += DodgeManager_OnGameOver;
    }

    private async void DodgeManager_OnGameOver(object sender, System.EventArgs e)
    {
        Debug.Log("OnDodgeGameOver");

        lastScore = DodgeManager.Instance.GetScore();
        await RefreshCoinsText();

        myanimator.SetBool("IsGameOver", true);
    }

    private async void OnLocaleChanged(Locale _)
    {
        // Si el idioma cambia mientras está en Game Over
        if (myanimator.GetBool("IsGameOver"))
        {
            await RefreshCoinsText();
        }
    }

    private async Task RefreshCoinsText()
    {
        if (coinText == null) return;

        // Fallback por si no asignas la LocalizedString
        if (coinsTextTemplate.IsEmpty)
        {
            coinText.text = "Coins: " + lastScore;
            return;
        }

        coinsTextTemplate.Arguments = new object[] { lastScore };

        AsyncOperationHandle<string> handle = coinsTextTemplate.GetLocalizedStringAsync();
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
            coinText.text = handle.Result;
    }

    private void OnDestroy()
    {
        DodgeManager.Instance.OnGameOver -= DodgeManager_OnGameOver;
    }
}
