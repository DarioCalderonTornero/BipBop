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

    [Header("Resume Countdown")]
    [SerializeField] private ReviveCountdownUI resumeCountdownUI;

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
        // PAUSE
        pauseGameButton.onClick.AddListener(() =>
        {
            gamePauseAnimator.SetBool("IsGamePaused", true);
            pauseGameButton.gameObject.SetActive(false);
            closeGamePauseImage.gameObject.SetActive(true);

            Time.timeScale = 0f;
        });

        // RESUME
        resumeGameButton.onClick.AddListener(() =>
        {
            if (isResuming) return;
            StartResumeFlow();
        });

        settingsButton.onClick.AddListener(() =>
        {
            gamePauseAnimator.SetBool("IsSettingsOpen", true);
        });

        mainMenuButton.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            SceneLoader.LoadScene(SceneLoader.Scene.Menu);
        });

        soundChangeButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.ChangeSoundVolume();
            UpdateSoundText();

            bool muted = SoundManager.Instance.GetSoundVolume() == 0;
            getCancelSoundVolumeImage.gameObject.SetActive(muted);
            getSoundVolumeImage.gameObject.SetActive(!muted);
        });

        musicChangeButton.onClick.AddListener(() =>
        {
            MusicManager.Instance.ChangeMusicVolume();
            UpdateMusicText();

            bool muted = MusicManager.Instance.GetMusicVolume() == 0;
            getCancelMusicVolumeImage.gameObject.SetActive(muted);
            getMusicVolumeImage.gameObject.SetActive(!muted);
        });

        soundCancelVolumeButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.GetCancelVolume();
            UpdateSoundText();

            bool muted = SoundManager.Instance.GetSoundVolume() == 0;
            getCancelSoundVolumeImage.gameObject.SetActive(muted);
            getSoundVolumeImage.gameObject.SetActive(!muted);
        });

        musicCancelVolumeButton.onClick.AddListener(() =>
        {
            MusicManager.Instance.CancelMusicVolume();
            UpdateMusicText();

            bool muted = MusicManager.Instance.GetMusicVolume() == 0;
            getCancelMusicVolumeImage.gameObject.SetActive(muted);
            getMusicVolumeImage.gameObject.SetActive(!muted);
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

        bool soundMuted = SoundManager.Instance.GetSoundVolume() == 0;
        getCancelSoundVolumeImage.gameObject.SetActive(soundMuted);
        getSoundVolumeImage.gameObject.SetActive(!soundMuted);

        bool musicMuted = MusicManager.Instance.GetMusicVolume() == 0;
        getCancelMusicVolumeImage.gameObject.SetActive(musicMuted);
        getMusicVolumeImage.gameObject.SetActive(!musicMuted);
    }

    private void StartResumeFlow()
    {
        isResuming = true;

        // Bloqueamos interacción
        resumeGameButton.interactable = false;
        pauseGameButton.interactable = false;
        settingsButton.interactable = false;
        mainMenuButton.interactable = false;


        Time.timeScale = 0f;

        resumeCountdownUI.Play(OnResumeCountdownFinished);
    }

    private void OnResumeCountdownFinished()
    {
        // AHORA sí cerramos el GamePause
        gamePauseAnimator.SetBool("IsGamePaused", false);

        Time.timeScale = 1f;

        pauseGameButton.gameObject.SetActive(true);

        resumeGameButton.interactable = true;
        pauseGameButton.interactable = true;
        settingsButton.interactable = true;
        mainMenuButton.interactable = true;

        isResuming = false;
    }

    private void OnLocaleChanged(Locale locale)
    {
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
        gamePauseAnimator.SetBool("IsSettingsClose", false);
        gamePauseAnimator.SetBool("IsSettingsOpen", false);
    }
}
