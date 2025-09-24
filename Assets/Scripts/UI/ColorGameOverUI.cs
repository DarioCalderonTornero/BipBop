using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ColorGameOverUI : MonoBehaviour
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
            SceneManager.LoadScene("ColorScene");
        });

        mainMenuButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("Menu");
        });
    }

    void Start()
    {
        myanimator = GetComponent<Animator>();
        ColorManager.Instance.OnGameOver += ColorManager_OnGameOver;
    }

    private void ColorManager_OnGameOver(object sender, System.EventArgs e)
    {
        myanimator.SetBool("IsGameOver", true);
    }
}

