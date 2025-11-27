using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DodgeGameOverUI : MonoBehaviour
{
    
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Image backGround;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI gameOverText;

    [SerializeField] private Animator myanimator;

    private void Awake()
    {
        //Hide();
        retryButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.DodgeScene);

        });

        mainMenuButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.Menu);
        });
    }

    void Start()
    {
        myanimator = GetComponent<Animator>();
        DodgeManager.Instance.OnGameOver += DodgeManager_OnGameOver;
    }

    private void DodgeManager_OnGameOver(object sender, System.EventArgs e)
    {
        Debug.Log("OnGeometricGameOver");
        coinText.text = "Coins: " + DodgeManager.Instance.GetScore();
        myanimator.SetBool("IsGameOver", true);
    }

    private void OnDestroy()
    {
        DodgeManager.Instance.OnGameOver -= DodgeManager_OnGameOver;
    }
}

