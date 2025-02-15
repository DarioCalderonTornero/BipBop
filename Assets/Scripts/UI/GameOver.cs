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

    [SerializeField] private Animator myanimator;

    private void Awake()
    {
        //Hide();
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
        myanimator = GetComponent<Animator>();
        LogicaPuntos.Instance.OnGameOver += LogicaPuntos_OnGameOver;
    }

    private void Update()
    {

    }

    private void LogicaPuntos_OnGameOver(object sender, System.EventArgs e)
    {
        Debug.Log("Show");
        myanimator.SetBool("IsGameOver", true);
    }
  
}
