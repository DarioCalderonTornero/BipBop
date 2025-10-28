using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectGame : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Button openGameModeButton;
    [SerializeField] private Button closeGameModeButton;

    [Header("GameMode Images")]
    [SerializeField] private Image bipbopImage;
    [SerializeField] private Image colorImage;
    [SerializeField] private Image geometricImage;
    [SerializeField] private Image dodgeImage;
    [SerializeField] private Image gridImage;

    [Header("GameMode Buttons")]
    [SerializeField] private Button gameModeBipbopButton;
    [SerializeField] private Button gameModeColorButton;
    [SerializeField] private Button gameModeGeometricButton;
    [SerializeField] private Button gameModeDodgeButton;
    [SerializeField] private Button gameModeGridButton;

    [Header("Play GameMode Buttons")]
    [SerializeField] private Button playBipbopButton;
    [SerializeField] private Button playColorButton;
    [SerializeField] private Button playGeometricButton;
    [SerializeField] private Button playDodgeButton;
    [SerializeField] private Button playGridButton;

    private void Awake()
    {
        openGameModeButton.onClick.AddListener(() =>
        {
            Show();
        });

        closeGameModeButton.onClick.AddListener(() =>
        {
            Hide();
        });

        // --- GameMode Buttons --- 

        gameModeBipbopButton.onClick.AddListener(() =>
        {
            ShowBipBopButton();
            Hide();
        });

        gameModeColorButton.onClick.AddListener(() =>
        {
            ShowColorButton();
            Hide();
        });

        gameModeGeometricButton.onClick.AddListener(() =>
        {
            ShowGeometricButton();
            Hide();
        });

        gameModeDodgeButton.onClick.AddListener(() =>
        {
            ShowDodgeButton();
            Hide();
        });

        gameModeGridButton.onClick.AddListener(() =>
        {
            ShowGridButton();
            Hide();
        });


        // --- Play Buttons ---

        playBipbopButton.onClick.AddListener(() =>
        {
            //SceneLoader.LoadScene(SceneLoader.Scene.GameScene);
        });

        playColorButton.onClick.AddListener(() =>
        {
            //SceneLoader.LoadScene(SceneLoader.Scene.ColorScene);
        });

        playGeometricButton.onClick.AddListener(() =>
        {
            //SceneLoader.LoadScene(SceneLoader.Scene.GeometricScene);
        });

        playDodgeButton.onClick.AddListener(() =>
        {
            //SceneLoader.LoadScene(SceneLoader.Scene.DodgeScene);
        });

        playGridButton.onClick.AddListener(() =>
        {
            //SceneLoader.LoadScene(SceneLoader.Scene.GridScene);
        });
    }

    private void Start()
    {
        Hide();
        ShowBipBopButton();
    }

    private void Hide()
    {
        backgroundImage.gameObject.SetActive(false);
        bipbopImage.gameObject.SetActive(false);
        colorImage.gameObject.SetActive(false);
        geometricImage.gameObject.SetActive(false);
        dodgeImage.gameObject.SetActive(false);
        gridImage.gameObject.SetActive(false);

        closeGameModeButton.gameObject.SetActive(false);
        //openGameModeButton.gameObject.SetActive(true);

        HideGameModeButtons();
    }

    private void Show()
    {
        backgroundImage.gameObject.SetActive(true);
        bipbopImage.gameObject.SetActive(true);
        colorImage.gameObject.SetActive(true);
        geometricImage.gameObject.SetActive(true);
        dodgeImage.gameObject.SetActive(true);
        gridImage.gameObject.SetActive(true);

        //openGameModeButton.gameObject.SetActive(false);
        closeGameModeButton.gameObject.SetActive(true);

        ShowGameModeButtons();
    }

    private void HideGameModeButtons()
    {
        gameModeBipbopButton.gameObject.SetActive(false);
        gameModeColorButton.gameObject.SetActive(false);
        gameModeGeometricButton.gameObject.SetActive(false);
        gameModeDodgeButton.gameObject.SetActive(false);
        gameModeGridButton.gameObject.SetActive(false);
    }

    private void ShowGameModeButtons()
    {
        gameModeBipbopButton.gameObject.SetActive(true);
        gameModeColorButton.gameObject.SetActive(true);
        gameModeGeometricButton.gameObject.SetActive(true);
        gameModeDodgeButton.gameObject.SetActive(true);
        gameModeGridButton.gameObject.SetActive(true);
    }

    private void ShowBipBopButton()
    {
        playBipbopButton.gameObject.SetActive(true);
        playColorButton.gameObject.SetActive(false);
        playGeometricButton.gameObject.SetActive(false);
        playDodgeButton.gameObject.SetActive(false);
        playGridButton.gameObject.SetActive(false);
    }

    private void ShowColorButton()
    {
        playBipbopButton.gameObject.SetActive(false);
        playColorButton.gameObject.SetActive(true);
        playGeometricButton.gameObject.SetActive(false);
        playDodgeButton.gameObject.SetActive(false);
        playGridButton.gameObject.SetActive(false);
    }
    private void ShowGeometricButton()
    {
        playBipbopButton.gameObject.SetActive(false);
        playColorButton.gameObject.SetActive(false);
        playGeometricButton.gameObject.SetActive(true);
        playDodgeButton.gameObject.SetActive(false);
        playGridButton.gameObject.SetActive(false);
    }
    private void ShowDodgeButton()
    {
        playBipbopButton.gameObject.SetActive(false);
        playColorButton.gameObject.SetActive(false);
        playGeometricButton.gameObject.SetActive(false);
        playDodgeButton.gameObject.SetActive(true);
        playGridButton.gameObject.SetActive(false);
    }
    private void ShowGridButton()
    {
        playBipbopButton.gameObject.SetActive(false);
        playColorButton.gameObject.SetActive(false);
        playGeometricButton.gameObject.SetActive(false);
        playDodgeButton.gameObject.SetActive(false);
        playGridButton.gameObject.SetActive(true);
    }
    
}
