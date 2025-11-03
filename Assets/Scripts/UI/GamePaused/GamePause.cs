using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GamePause : MonoBehaviour
{
    public event EventHandler OnPauseMenu;

    private bool isGamePaused = false;

    [Header("Main Buttons")]
    [SerializeField] private Button pauseGameButton;
    [SerializeField] private Button resumeGameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Sound Buttons")]
    [SerializeField] private Button soundChangeButton;
    [SerializeField] private Button musicChangeButton;
    [SerializeField] private Button cancelVolumeButton;
    [SerializeField] private Button closeSoundSettingsButton;

    [Header("Sound Texts")]
    [SerializeField] private TextMeshProUGUI soundChangeText;
    [SerializeField] private TextMeshProUGUI musicChangeText;

    [SerializeField] private Button closeGamePauseImage;

    [SerializeField] private Animator gamePauseAnimator;

    private void Awake()
    {
        pauseGameButton.onClick.AddListener(() =>
        {
            gamePauseAnimator.SetBool("IsGamePaused", true);
            pauseGameButton.gameObject.SetActive(false);
            closeGamePauseImage.gameObject.SetActive(true);
        });

        resumeGameButton.onClick.AddListener(() =>
        {
            gamePauseAnimator.SetBool("IsGamePaused", false);
            pauseGameButton.gameObject.SetActive(true);
        });

        settingsButton.onClick.AddListener(() =>
        {
            gamePauseAnimator.SetBool("IsSettingsOpen", true);
            //StartCoroutine(Anim());
            Debug.Log("Settigs Button");
        });

        mainMenuButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.Menu);
            Time.timeScale = 1.0f;  
        });

        soundChangeButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.ChangeSoundVolume();
            soundChangeText.text = "Sound Volume: " + SoundManager.Instance.GetSoundVolume();
        });

        musicChangeButton.onClick.AddListener(() =>
        {
            //SoundManager.Instance.ChangeSoundVolume();
            //musicChangeText.text = "Sound Volume: " + SoundManager.Instance.GetSoundVolume();
        });

        cancelVolumeButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.GetCancelVolume();
        });

        closeSoundSettingsButton.onClick.AddListener(() =>
        {
            gamePauseAnimator.SetBool("IsSettingsClose", true);
        });
    }

    private void Start()
    {
        closeGamePauseImage.gameObject.SetActive(false);
        soundChangeText.text = "Sound Volume: " + SoundManager.Instance.GetSoundVolume();
    }

    private IEnumerator Anim()
    {
        yield return new WaitForSecondsRealtime(1f);
        Debug.Log("FALSE");
        gamePauseAnimator.SetBool("IsSettingsOpen", false);
    }

    public void StopTime()
    {
        Time.timeScale = 0f;
    }

    public void ResumeTime()
    {
        Time.timeScale = 1f;
    }

    public void CloseGamePause()
    {
        Debug.Log("CloseGamePause");
    }
}
