using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using UnityEngine.Localization;

public class SettingsUI : MonoBehaviour
{
    [Header("Images")]
    [SerializeField] private Image blackBackgroundImage;
    [SerializeField] private Image settingsBackgroundImage;
    [SerializeField] private Image idiomaBackgroundImage;
    [SerializeField] private Image vibrationBackgroundImage;
    [SerializeField] private Image soundBackgroundImage;
    [SerializeField] private Image musicBackgroundImage;
    [SerializeField] private Image creditsBackgroundImage;
    [SerializeField] private Image resetSettingsImage;
    [SerializeField] private Image generalNextLineImage;
    [SerializeField] private Image audioNextLineImage;
    [SerializeField] private Image extrasNextLineImage;

    [Header("Buttons")]
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button openSettingsButton;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private Button idiomaButton;

    [SerializeField] private Button vibrationButton;
    [SerializeField] private Button soundVolumeButton;
    [SerializeField] private Button musicVolumeButton;
    [SerializeField] private Button resetSettingsButton;

    [Header("General Texts")]
    [SerializeField] private TextMeshProUGUI settingsText;
    [SerializeField] private TextMeshProUGUI generalSettingsText;
    [SerializeField] private TextMeshProUGUI audioSettingsText;
    [SerializeField] private TextMeshProUGUI extraSettingsText;

    [SerializeField] private TextMeshProUGUI idiomaButtonText;

    [Header("Settings Texts")]
    [SerializeField] private TextMeshProUGUI idiomaText;
    [SerializeField] private TextMeshProUGUI vibrationText;
    [SerializeField] private TextMeshProUGUI soundText;
    [SerializeField] private TextMeshProUGUI musicText;
    [SerializeField] private TextMeshProUGUI creditsText;
    [SerializeField] private TextMeshProUGUI resetText;

    [Header("Sound Logic Images")]
    [SerializeField] private Image soundVolumeOnImage;
    [SerializeField] private Image soundVolumeOffImage;

    [SerializeField] private Image musicVolumeOnImage;
    [SerializeField] private Image musicVolumeOffImage;

    [Header("Sprite Images")]
    [SerializeField] private Image settingsImage;
    [SerializeField] private Image idiomaImage;
    [SerializeField] private Image vibrationImage;
    [SerializeField] private Image soundImage;
    [SerializeField] private Image musicImage;
    [SerializeField] private Image creditsImage;
    [SerializeField] private Image resetImage;


    [Header("Vibration State Images")]
    [SerializeField] private Image vibrationOnImage;
    [SerializeField] private Image vibrationOffImage;

    [SerializeField] private Animator settingsAnimator;

    private bool isVibrationImageOn = true;

    private void Awake()
    {
        openSettingsButton.onClick.AddListener(Show);
        closeSettingsButton.onClick.AddListener(() =>
        {
            settingsAnimator.SetBool("IsSettingsOpen", false);
            settingsAnimator.SetBool("IsSettingsClose", true);
            //Hide();
        });

        vibrationButton.onClick.AddListener(() =>
        {
            bool newState = !Haptics.enabled;
            Haptics.SetEnabled(newState);

            if (newState)
                Haptics.TryVibrate();

            RefreshVibrationUI();
        });

        
        soundVolumeButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.GetCancelVolume();
            bool isSoundMuted = SoundManager.Instance.GetSoundVolumeNormalized() == 0;
            soundVolumeOffImage.gameObject.SetActive(isSoundMuted);
            soundVolumeOnImage.gameObject.SetActive(!isSoundMuted);  
        });

        musicVolumeButton.onClick.AddListener(() =>
        {
            MusicManager.Instance.CancelMusicVolume();
            bool isMusicMuted = MusicManager.Instance.GetMusicVolumeNormalized() == 0; ;
            musicVolumeOffImage.gameObject.SetActive(isMusicMuted);
            musicVolumeOnImage.gameObject.SetActive(!isMusicMuted);
        });
        
    }

    private void Start()
    {
        Hide();
        RefreshVibrationUI();
        SetCancelVolumeImage();

        LocalizationSettings.SelectedLocaleChanged += LocalizationSettings_SelectedLocaleChanged;
        
        //Update sound in start
        bool isSoundMuted = SoundManager.Instance.GetSoundVolumeNormalized() == 0;
        soundVolumeOffImage.gameObject.SetActive(isSoundMuted);
        soundVolumeOnImage.gameObject.SetActive(!isSoundMuted);

        //Update music in start
        bool isMusicMuted = MusicManager.Instance.GetMusicVolumeNormalized() == 0; ;
        musicVolumeOffImage.gameObject.SetActive(isMusicMuted);
        musicVolumeOnImage.gameObject.SetActive(!isMusicMuted);
        
    }

    private void LocalizationSettings_SelectedLocaleChanged(Locale newLocale)
    {
        RefreshLenguage(newLocale);
    }

    private void RefreshLenguage(Locale locale)
    {
        string code = locale.Identifier.Code;

        if (code.StartsWith("es"))
        {
            idiomaButtonText.text = "ESP";
        }

        if (code.StartsWith("en"))
        {
            idiomaButtonText.text = "ENG";
        }
    }

    private void SetCancelVolumeImage()
    {
        isVibrationImageOn = !isVibrationImageOn;

        soundVolumeOnImage.gameObject.SetActive(!isVibrationImageOn);
        soundVolumeOffImage.gameObject.SetActive(isVibrationImageOn);
    }


    private void RefreshVibrationUI()
    {
        if (vibrationButton == null) return;

        Color on = new Color(1f, 0.75f, 0.2f, 1f);
        Color off = new Color(0.6f, 0.6f, 0.6f, 1f);

        bool enabled = Haptics.enabled;

        // Colores del botón
        var cb = vibrationButton.colors;
        Color baseCol = enabled ? on : off;
        cb.normalColor = baseCol;
        cb.highlightedColor = baseCol * 1.05f;
        cb.pressedColor = baseCol * 0.9f;
        cb.selectedColor = baseCol;
        cb.disabledColor = new Color(baseCol.r, baseCol.g, baseCol.b, 0.5f);
        cb.colorMultiplier = 1f;
        vibrationButton.colors = cb;

        if (vibrationButton.image != null)
            vibrationButton.image.color = baseCol;

        // Mostrar/ocultar imágenes según el estado
        if (vibrationOnImage != null)
            vibrationOnImage.gameObject.SetActive(enabled);

        if (vibrationOffImage != null)
            vibrationOffImage.gameObject.SetActive(!enabled);
    }

    private void Show()
    {
        //Animations
        settingsAnimator.SetBool("IsSettingsOpen", true);
        settingsAnimator.SetBool("IsSettingsClose", false);
        //StartCoroutine(OpenSettingsAnimFalse());

        //-----GENERAL SHOW/HIDE LOGIC------//

        //Texts Show
        settingsText.gameObject.SetActive(true);
        generalSettingsText.gameObject.SetActive(true);
        audioSettingsText.gameObject.SetActive(true);
        extraSettingsText.gameObject.SetActive(true);

        idiomaText.gameObject.SetActive(true);
        vibrationText.gameObject.SetActive(true);
        soundText.gameObject.SetActive(true);
        musicText.gameObject.SetActive(true);
        creditsText.gameObject.SetActive(true);
        resetText.gameObject.SetActive(true);

        //Images Show
        settingsBackgroundImage.gameObject.SetActive(true);
        blackBackgroundImage.gameObject.SetActive(true);

        idiomaBackgroundImage.gameObject.SetActive(true);
        vibrationBackgroundImage.gameObject.SetActive(true);
        soundBackgroundImage.gameObject.SetActive(true);    
        musicBackgroundImage.gameObject.SetActive(true);
        creditsBackgroundImage.gameObject.SetActive(true);
        resetSettingsImage.gameObject.SetActive(true); 
        audioNextLineImage.gameObject.SetActive(true);
        extrasNextLineImage.gameObject.SetActive(true);
        generalNextLineImage.gameObject.SetActive(true);

        //Images sprites Show
        settingsImage.gameObject.SetActive(true);
        idiomaImage.gameObject.SetActive(true); 
        vibrationImage.gameObject.SetActive(true);
        soundImage.gameObject.SetActive(true);  
        musicImage.gameObject.SetActive(true);  
        creditsImage.gameObject.SetActive(true);
        resetImage.gameObject.SetActive(true);  

        //Buttons Show
        creditsButton.gameObject.SetActive(true);
        closeSettingsButton.gameObject.SetActive(true);
        idiomaButton.gameObject.SetActive(true);    
        vibrationButton.gameObject.SetActive(true);
        soundVolumeButton.gameObject.SetActive(true);
        musicVolumeButton.gameObject.SetActive(true);  
        resetSettingsButton.gameObject.SetActive(true);
    }

    public void Hide()
    {
        //-----GENERAL SHOW/HIDE LOGIC------//

        //Texts Hide
        settingsText.gameObject.SetActive(false);
        generalSettingsText.gameObject.SetActive(false);
        audioSettingsText.gameObject.SetActive(false);
        extraSettingsText.gameObject.SetActive(false);

        idiomaText.gameObject.SetActive(false);
        vibrationText.gameObject.SetActive(false);
        soundText.gameObject.SetActive(false);
        musicText.gameObject.SetActive(false);
        creditsText.gameObject.SetActive(false);
        resetText.gameObject.SetActive(false);

        //Images Hide
        settingsBackgroundImage.gameObject.SetActive(false);
        blackBackgroundImage.gameObject.SetActive(false);

        idiomaBackgroundImage.gameObject.SetActive(false);
        vibrationBackgroundImage.gameObject.SetActive(false);
        soundBackgroundImage.gameObject.SetActive(false);
        musicBackgroundImage.gameObject.SetActive(false);
        creditsBackgroundImage.gameObject.SetActive(false);
        resetSettingsImage.gameObject.SetActive(false);
        audioNextLineImage.gameObject.SetActive(false);
        extrasNextLineImage.gameObject.SetActive(false);
        generalNextLineImage.gameObject.SetActive(false);

        //Images sprites Show
        settingsImage.gameObject.SetActive(false);
        idiomaImage.gameObject.SetActive(false);
        vibrationImage.gameObject.SetActive(false);
        soundImage.gameObject.SetActive(false);
        musicImage.gameObject.SetActive(false);
        creditsImage.gameObject.SetActive(false);
        resetImage.gameObject.SetActive(false);

        //Buttons Hide
        creditsButton.gameObject.SetActive(false);
        closeSettingsButton.gameObject.SetActive(false);
        idiomaButton.gameObject.SetActive(false);
        vibrationButton.gameObject.SetActive(false);
        soundVolumeButton.gameObject.SetActive(false);
        musicVolumeButton.gameObject.SetActive(false);
        resetSettingsButton.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        LocalizationSettings.SelectedLocaleChanged += LocalizationSettings_SelectedLocaleChanged;
    }
}
