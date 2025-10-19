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
        openSettingsButton.onClick.AddListener(()=>
        {
           Show();
        });

        closeSettingsButton.onClick.AddListener(()=>
        {
            Hide();
        });

        vibrationButton.onClick.AddListener(()=>
        {

        });

        volumeButton.onClick.AddListener(()=>
        {

        });
    }

    private void Start()
    {
        Hide();
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
