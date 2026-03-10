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

    [Header("UI Containers")]
    [SerializeField] private GameObject visualContainer;

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

    [Header("Sound Icons")]
    [SerializeField] private Image soundIcon;

    [Header("Music Icons")]
    [SerializeField] private Image musicIcon;

    [Header("Icon Colors")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color disabledColor = new Color(0.65f, 0.65f, 0.65f, 1f);

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

    [SerializeField] private AudioClip buttonAudioclip;

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
        //Button sounds
        Button[] allButtons = GetComponentsInChildren<Button>();

        foreach (Button button in allButtons)
        {
            button.onClick.AddListener(() =>
            {
                SoundManager.Instance.PlaySound(buttonAudioclip, 1.0f);
            });
        }
        // PAUSE
        pauseGameButton.onClick.AddListener(() =>
        {
            if (visualContainer != null)
                visualContainer.SetActive(true);

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

        // SOUND CHANGE
        soundChangeButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.ChangeSoundVolume();
            UpdateSoundText();
            UpdateSoundIcon();
        });

        // MUSIC CHANGE
        musicChangeButton.onClick.AddListener(() =>
        {
            MusicManager.Instance.ChangeMusicVolume();
            UpdateMusicText();
            UpdateMusicIcon();
        });

        // SOUND CANCEL
        soundCancelVolumeButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.GetCancelVolume();
            UpdateSoundText();
            UpdateSoundIcon();
        });

        // MUSIC CANCEL
        musicCancelVolumeButton.onClick.AddListener(() =>
        {
            MusicManager.Instance.CancelMusicVolume();
            UpdateMusicText();
            UpdateMusicIcon();
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

        UpdateSoundIcon();
        UpdateMusicIcon();
    }

    private void StartResumeFlow()
    {
        isResuming = true;

        if (visualContainer != null)
            visualContainer.SetActive(false);

        resumeGameButton.interactable = false;
        pauseGameButton.interactable = false;
        settingsButton.interactable = false;
        mainMenuButton.interactable = false;

        resumeCountdownUI.Play(OnResumeCountdownFinished);

        Time.timeScale = 0f;
    }

    private void OnResumeCountdownFinished()
    {
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

    private void UpdateSoundIcon()
    {
        if (soundIcon == null) return;

        bool muted = SoundManager.Instance.GetSoundVolume() == 0;
        soundIcon.color = muted ? disabledColor : activeColor;
    }

    private void UpdateMusicIcon()
    {
        if (musicIcon == null) return;

        bool muted = MusicManager.Instance.GetMusicVolume() == 0;
        musicIcon.color = muted ? disabledColor : activeColor;
    }

    private IEnumerator InvokeNormalAnim()
    {
        yield return new WaitForSecondsRealtime(1f);
        gamePauseAnimator.SetBool("IsSettingsClose", false);
        gamePauseAnimator.SetBool("IsSettingsOpen", false);
    }
}