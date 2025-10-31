using System;
using UnityEngine;
using UnityEngine.UI;

public class GamePause : MonoBehaviour
{
    public event EventHandler OnPauseMenu;

    private bool isGamePaused = false;

    [SerializeField] private Button pauseGameButton;
    [SerializeField] private Button resumeGameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;

    [SerializeField] private Animator gamePauseAnimator;

    private void Awake()
    {
        pauseGameButton.onClick.AddListener(() =>
        {
            gamePauseAnimator.SetBool("IsGamePaused", true);
            pauseGameButton.gameObject.SetActive(false);    
        });

        resumeGameButton.onClick.AddListener(() =>
        {
            gamePauseAnimator.SetBool("IsGamePaused", false);
            pauseGameButton.gameObject.SetActive(true);
        });

        settingsButton.onClick.AddListener(() =>
        {
            gamePauseAnimator.SetBool("IsSettingsOpen", true);
            Debug.Log("Settigs Button");
        });

        mainMenuButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.Menu);
            Time.timeScale = 1.0f;  
        });
    }

    public void StopTime()
    {
        Time.timeScale = 0f;
    }

    public void ResumeTime()
    {
        Time.timeScale = 1f;
    }
}
