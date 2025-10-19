using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;

    [Header("Buttons")]
    [SerializeField] private Button mainSettingsButton;
    [SerializeField] private Button challengesButton;
    [SerializeField] private Button creditsButton;

    [SerializeField] private Button openSettingsButton;
    [SerializeField] private Button closeSettingsButton;

    [SerializeField] private Button vibrationButton;
    [SerializeField] private Button volumeButton;

    [Header("Images")]
    [SerializeField] private Image mainSettingsImage;
    [SerializeField] private Image challengesImage;

    private void Awake()
    {
        openSettingsButton.onClick.AddListener(Show);

        closeSettingsButton.onClick.AddListener(Hide);
       

        vibrationButton.onClick.AddListener(()=>
        {
            Haptics.SetEnabled(!Haptics.enabled);
            RefreshVibrationUI();
        });

        volumeButton.onClick.AddListener(()=>
        {

        });
    }

    

    private void Start()
    {
        Hide();
        RefreshVibrationUI();
    }

    private void RefreshVibrationUI()
    {
        if (vibrationButton == null) return;

        Color on = new Color(1f, 0.75f, 0.2f, 1f);  
        Color off = new Color(0.6f, 0.6f, 0.6f, 1f); 

        var cb = vibrationButton.colors;
        bool enabled = Haptics.enabled; 

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
    }


    private void Show()
    {
        backgroundImage.gameObject.SetActive(true);

        mainSettingsButton.gameObject.SetActive(true);
        challengesButton.gameObject.SetActive(true);
        creditsButton.gameObject.SetActive(true);
        closeSettingsButton.gameObject.SetActive(true);

        vibrationButton.gameObject.SetActive(true);
        volumeButton.gameObject.SetActive(true);

        mainSettingsImage.gameObject.SetActive(true);
        challengesImage.gameObject.SetActive(true);
    }

    private void Hide()
    {
        backgroundImage.gameObject.SetActive(false);

        mainSettingsButton.gameObject.SetActive(false);
        challengesButton.gameObject.SetActive(false);
        creditsButton.gameObject.SetActive(false);
        closeSettingsButton.gameObject.SetActive(false);

        vibrationButton.gameObject.SetActive(false);
        volumeButton.gameObject.SetActive(false);

        mainSettingsImage.gameObject.SetActive(false);
        challengesImage.gameObject.SetActive(false);
    }
}
