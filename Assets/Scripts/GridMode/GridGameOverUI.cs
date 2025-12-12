using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GridGameOverUI : MonoBehaviour
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
        Debug.Log("OnGeometricGameOver");
        coinText.text = "Points: " + GridGameManager.Instance.GetScore();
        myanimator.SetBool("IsGameOver", true);
    }

    private void OnDestroy()
    {
        GridGameManager.Instance.OnGameOver -= GridGameManager_OnGameOver;
    }
}
