using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private Button retryButton;
    [SerializeField] private TextMeshProUGUI scoreText;

    private void Awake()
    {
        retryButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("SampleScene");
            Hide();
        });
    }

    private void Start()
    {
        LogicaJuego.Instance.OnGameOver += LogicaPuntos_OnGameOver;
        Hide();
    }

    private void LogicaPuntos_OnGameOver(object sender, System.EventArgs e)
    {
        // Muestra la puntuación final
        //scoreText.text = $"Score: {LogicaJuego.Instance.GetScore()}";
        Show();
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }
}
