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
    [SerializeField] private Button soundCancelVolumeButton;
    [SerializeField] private Button musicCancelVolumeButton;
    [SerializeField] private Button closeSoundSettingsButton;

    [Header("Sound Images")]
    [SerializeField] private Image getCancelSoundVolumeImage;
    [SerializeField] private Image getSoundVolumeImage;

    [Header("Music Images")]
    [SerializeField] private Image getCancelMusicVolumeImage;
    [SerializeField] private Image getMusicVolumeImage;

    [Header("Sound Texts")]
    [SerializeField] private TextMeshProUGUI soundChangeText;
    [SerializeField] private TextMeshProUGUI musicChangeText;

    [SerializeField] private Button closeGamePauseImage;

    [SerializeField] private Animator gamePauseAnimator;

    bool cancelImage = true;

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
            bool isSoundMutedNow = SoundManager.Instance.GetSoundVolume() == 0;
            getCancelSoundVolumeImage.gameObject.SetActive(isSoundMutedNow);
            getSoundVolumeImage.gameObject.SetActive(!isSoundMutedNow);
        });

        /*
        musicChangeButton.onClick.AddListener(() =>
        {
            MusicManager.Instance.ChangeMusicVolume();
            musicChangeText.text = "Music Volume: " + MusicManager.Instance.GetMusicVolume();
            bool isMusicMutedNow = MusicManager.Instance.GetMusicVolume() == 0;
            getCancelMusicVolumeImage.gameObject.SetActive(isMusicMutedNow);
            getMusicVolumeImage.gameObject.SetActive(!isMusicMutedNow);
        });
        */

        soundCancelVolumeButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.GetCancelVolume();
            soundChangeText.text = "Sound Volume: " + SoundManager.Instance.GetSoundVolume();
            bool isMutedNow = SoundManager.Instance.GetSoundVolume() == 0;
            getCancelSoundVolumeImage.gameObject.SetActive(isMutedNow);
            getSoundVolumeImage.gameObject.SetActive(!isMutedNow);
        });

        musicCancelVolumeButton.onClick.AddListener(() =>
        {

        });

        closeSoundSettingsButton.onClick.AddListener(() =>
        {
            gamePauseAnimator.SetBool("IsSettingsClose", true);
            StartCoroutine(InvokeNormalAnim());
        });
    }

    private void Start()
    {
        closeGamePauseImage.gameObject.SetActive(false);
        soundChangeText.text = "Sound Volume: " + SoundManager.Instance.GetSoundVolume();
        bool isMuted = SoundManager.Instance.GetSoundVolume() == 0;
        getCancelSoundVolumeImage.gameObject.SetActive(isMuted);
        getSoundVolumeImage.gameObject.SetActive(!isMuted);
        cancelImage = !isMuted;
    }

    private IEnumerator InvokeNormalAnim()
    {
        yield return new WaitForSecondsRealtime(1f);
        Debug.Log("FALSE");
        gamePauseAnimator.SetBool("IsSettingsClose", false);
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
