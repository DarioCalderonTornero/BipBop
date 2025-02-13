using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOver : MonoBehaviour
{
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Image backGround;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI gameOverText;

    private void Awake()
    {
        Hide();
        retryButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("GameScene");
        });

        mainMenuButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("Menu");
        });
    }

    void Start()
    {
        LogicaPuntos.Instance.OnGameOver += LogicaPuntos_OnGameOver;
        
    }

    private void LogicaPuntos_OnGameOver(object sender, System.EventArgs e)
    {
        Show();
        Debug.Log("Show");
    }

    private void Show()
    {
        retryButton.gameObject.SetActive(true);
        mainMenuButton.gameObject.SetActive(true);
        gameOverText.gameObject.SetActive(true);
        backGround.gameObject.SetActive(true);
        coinText.gameObject.SetActive(true);
    }

    private void Hide()
    {
        retryButton.gameObject.SetActive(false);
        mainMenuButton.gameObject.SetActive(false);
        gameOverText.gameObject.SetActive(false);
        backGround.gameObject.SetActive(false);
        coinText.gameObject.SetActive(false);
    }
}
