using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;

    [Header("Buttons")]
    [SerializeField] private Button creditsButton;

    [SerializeField] private Button openSettingsButton;
    [SerializeField] private Button closeSettingsButton;

    [SerializeField] private Button vibrationButton;
    [SerializeField] private Button soundVolumeButton;
    [SerializeField] private Button musicVolumeButton;

    [Header("Images")]
    [SerializeField] private Image blackBackgroundImage;

    [SerializeField] private Image soundVolumeOnImage;
    [SerializeField] private Image soundVolumeOffImage;

    [SerializeField] private Image musicVolumeOnImage;
    [SerializeField] private Image musicVolumeOffImage;

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

        
        //Update sound in start
        bool isSoundMuted = SoundManager.Instance.GetSoundVolumeNormalized() == 0;
        soundVolumeOffImage.gameObject.SetActive(isSoundMuted);
        soundVolumeOnImage.gameObject.SetActive(!isSoundMuted);

        //Update music in start
        bool isMusicMuted = MusicManager.Instance.GetMusicVolumeNormalized() == 0; ;
        musicVolumeOffImage.gameObject.SetActive(isMusicMuted);
        musicVolumeOnImage.gameObject.SetActive(!isMusicMuted);
        
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
        settingsAnimator.SetBool("IsSettingsOpen", true);
        settingsAnimator.SetBool("IsSettingsClose", false);
        //StartCoroutine(OpenSettingsAnimFalse());

        backgroundImage.gameObject.SetActive(true);
        blackBackgroundImage.gameObject.SetActive(true);

        creditsButton.gameObject.SetActive(true);
        closeSettingsButton.gameObject.SetActive(true);

        vibrationButton.gameObject.SetActive(true);
        soundVolumeButton.gameObject.SetActive(true);

    }

    public void Hide()
    {
        backgroundImage.gameObject.SetActive(false);
        blackBackgroundImage.gameObject.SetActive(false);

        creditsButton.gameObject.SetActive(false);
        closeSettingsButton.gameObject.SetActive(false);

        vibrationButton.gameObject.SetActive(false);
        soundVolumeButton.gameObject.SetActive(false);

    }
}
