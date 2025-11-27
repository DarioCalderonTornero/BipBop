using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GeometricGameOverUI : MonoBehaviour
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
            SceneLoader.LoadScene(SceneLoader.Scene.GeometricScene);

        });

        mainMenuButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.Menu);
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
        coinText.text = "Coins: " + GeometricModeManager.Instance.GetScore();
        myanimator.SetBool("IsGameOver", true);
    }

    private void OnDestroy()
    {
        GeometricModeManager.Instance.OnGameOver -= GeometricModeManager_OnGameOver;
    }
}
