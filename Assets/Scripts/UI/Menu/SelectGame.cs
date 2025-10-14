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
    [SerializeField] private Button bipbopButton;
    [SerializeField] private Button colorButton;
    [SerializeField] private Button geometricButton;
    [SerializeField] private Button dodgeButton;
    [SerializeField] private Button gridButton;

    [Header("Play GameMode Buttons")]
    [SerializeField] private Button playBipbopButton;
    [SerializeField] private Button playColorButton;
    [SerializeField] private Button playGeometricButton;
    [SerializeField] private Button playEsquivarButton;
    [SerializeField] private Button playMemoryButton;

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

        bipbopButton.onClick.AddListener(() =>
        {
            playBipbopButton.gameObject.SetActive(true);
            colorButton.gameObject.SetActive(false);
            geometricButton.gameObject.SetActive(false);
            dodgeButton.gameObject.SetActive(false);
            gridButton.gameObject.SetActive(false);
        });

        colorButton.onClick.AddListener(() =>
        {
            playBipbopButton.gameObject.SetActive(false);
            colorButton.gameObject.SetActive(true);
            geometricButton.gameObject.SetActive(false);
            dodgeButton.gameObject.SetActive(false);
            gridButton.gameObject.SetActive(false);
        });

        geometricButton.onClick.AddListener(() =>
        {
            playBipbopButton.gameObject.SetActive(false);
            colorButton.gameObject.SetActive(false);
            geometricButton.gameObject.SetActive(true);
            dodgeButton.gameObject.SetActive(false);
            gridButton.gameObject.SetActive(false);
        });

        dodgeButton.onClick.AddListener(() =>
        {
            playBipbopButton.gameObject.SetActive(false);
            colorButton.gameObject.SetActive(false);
            geometricButton.gameObject.SetActive(false);
            dodgeButton.gameObject.SetActive(true);
            gridButton.gameObject.SetActive(false);
        });

        gridButton.onClick.AddListener(() =>
        {
            playBipbopButton.gameObject.SetActive(false);
            colorButton.gameObject.SetActive(false);
            geometricButton.gameObject.SetActive(false);
            dodgeButton.gameObject.SetActive(false);
            gridButton.gameObject.SetActive(true);
        });


        // --- Play Buttons ---

        playBipbopButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.GameScene);
        });

        playColorButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.ColorScene);
        });

        playGeometricButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.GeometricScene);
        });

        playEsquivarButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.DodgeScene);
        });

        playMemoryButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.GridScene);
        });
    }

    private void Start()
    {
        Hide();
        playBipbopButton.gameObject.SetActive(true);
        colorButton.gameObject.SetActive(false);
        geometricButton.gameObject.SetActive(false);
        dodgeButton.gameObject.SetActive(false);
        gridButton.gameObject.SetActive(false);
    }

    private void Hide()
    {
        backgroundImage.gameObject.SetActive(false);
        bipbopImage.gameObject.SetActive(false);
        colorImage.gameObject.SetActive(false);
        geometricImage.gameObject.SetActive(false);
        dodgeImage.gameObject.SetActive(false);
        gridImage.gameObject.SetActive(false);

        bipbopButton.gameObject.SetActive(false);
        colorButton.gameObject.SetActive(false);
        geometricButton.gameObject.SetActive(false);
        dodgeButton.gameObject.SetActive(false);
        gridButton.gameObject.SetActive(false);
    }

    private void Show()
    {
        backgroundImage.gameObject.SetActive(true);
        bipbopImage.gameObject.SetActive(true);
        colorImage.gameObject.SetActive(true);
        geometricImage.gameObject.SetActive(true);
        dodgeImage.gameObject.SetActive(true);
        gridImage.gameObject.SetActive(true);

        bipbopButton.gameObject.SetActive(true);
        colorButton.gameObject.SetActive(true);
        geometricButton.gameObject.SetActive(true);
        dodgeButton.gameObject.SetActive(true);
        gridButton.gameObject.SetActive(true);
    }
}
