using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class GamePause : MonoBehaviour
{
    public event EventHandler OnPauseMenu;

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

    [Header("Localization")]
    [SerializeField] private LocalizedString soundVolumeLabel; 
    [SerializeField] private LocalizedString musicVolumeLabel; 

    bool cancelImage = true;

    [Header("Resume Countdown")]
    [SerializeField] private ResumeCountDownUI resumeCountdownUI;
    private bool isResuming;


    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

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
            if (isResuming) return;
            StartCoroutine(ResumeWithCountdown());
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
            UpdateSoundText(); //  en vez de texto hardcode
            bool isSoundMutedNow = SoundManager.Instance.GetSoundVolume() == 0;
            getCancelSoundVolumeImage.gameObject.SetActive(isSoundMutedNow);
            getSoundVolumeImage.gameObject.SetActive(!isSoundMutedNow);
        });

        musicChangeButton.onClick.AddListener(() =>
        {
            MusicManager.Instance.ChangeMusicVolume();
            UpdateMusicText(); //  en vez de texto hardcode
            bool isMusicMutedNow = MusicManager.Instance.GetMusicVolume() == 0;
            getCancelMusicVolumeImage.gameObject.SetActive(isMusicMutedNow);
            getMusicVolumeImage.gameObject.SetActive(!isMusicMutedNow);
        });

        soundCancelVolumeButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.GetCancelVolume();
            UpdateSoundText(); // 
            bool isMutedNow = SoundManager.Instance.GetSoundVolume() == 0;
            getCancelSoundVolumeImage.gameObject.SetActive(isMutedNow);
            getSoundVolumeImage.gameObject.SetActive(!isMutedNow);
        });

        musicCancelVolumeButton.onClick.AddListener(() =>
        {
            MusicManager.Instance.CancelMusicVolume();
            UpdateMusicText(); // 
            bool isMusicMutedNow = MusicManager.Instance.GetMusicVolume() == 0;
            getCancelMusicVolumeImage.gameObject.SetActive(isMusicMutedNow);
            getMusicVolumeImage.gameObject.SetActive(!isMusicMutedNow);
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

        UpdateSoundText();
        UpdateMusicText();

        // Sound
        bool isSoundMuted = SoundManager.Instance.GetSoundVolume() == 0;
        getCancelSoundVolumeImage.gameObject.SetActive(isSoundMuted);
        getSoundVolumeImage.gameObject.SetActive(!isSoundMuted);
        cancelImage = !isSoundMuted;

        // Music
        bool isMusicMuted = MusicManager.Instance.GetMusicVolume() == 0;
        getCancelMusicVolumeImage.gameObject.SetActive(isMusicMuted);
        getMusicVolumeImage.gameObject.SetActive(!isMusicMuted);
    }

    private void OnLocaleChanged(Locale locale)
    {
        // Si cambias el idioma dentro del juego, refrescamos los textos
        UpdateSoundText();
        UpdateMusicText();
    }

    private void UpdateSoundText()
    {
        if (soundChangeText == null) return;

        float vol = SoundManager.Instance.GetSoundVolume();
        soundChangeText.text = soundVolumeLabel.GetLocalizedString(vol);
    }

    private void UpdateMusicText()
    {
        if (musicChangeText == null) return;

        float vol = MusicManager.Instance.GetMusicVolume();
        musicChangeText.text = musicVolumeLabel.GetLocalizedString(vol);
    }

    private IEnumerator InvokeNormalAnim()
    {
        yield return new WaitForSecondsRealtime(1f);
        Debug.Log("FALSE");
        gamePauseAnimator.SetBool("IsSettingsClose", false);
        gamePauseAnimator.SetBool("IsSettingsOpen", false);
    }

    private IEnumerator ResumeWithCountdown()
    {
        isResuming = true;

        resumeGameButton.interactable = false;
        pauseGameButton.interactable = false;
        settingsButton.interactable = false;
        mainMenuButton.interactable = false;

        gamePauseAnimator.SetBool("IsGamePaused", false);
        closeGamePauseImage.gameObject.SetActive(false);

        Time.timeScale = 0f;

        bool done = false;
        resumeCountdownUI.Finished += OnFinished;
        resumeCountdownUI.Play();

        while (!done)
            yield return null;

        resumeCountdownUI.Finished -= OnFinished;

        Time.timeScale = 1f;

        pauseGameButton.gameObject.SetActive(true);

        resumeGameButton.interactable = true;
        pauseGameButton.interactable = true;
        settingsButton.interactable = true;
        mainMenuButton.interactable = true;

        isResuming = false;

        void OnFinished() => done = true;
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
