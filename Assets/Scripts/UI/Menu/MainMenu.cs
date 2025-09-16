using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public static MainMenu Instance { get; private set; }

    [SerializeField] private Button playButton;
    // [SerializeField] private Toggle voiceInstructionsToggle;
    [SerializeField] private Toggle motionTasksToggle;

    public event EventHandler OnPlayButton;

    private void Awake()
    {
        Instance = this;

        playButton.onClick.AddListener(() =>
        {
            OnPlayButton?.Invoke(this, EventArgs.Empty);
            SceneManager.LoadScene("GameScene");
        });
    }

    private void Start()
    {
        // Cargar configuraciones guardadas
        // voiceInstructionsToggle.isOn = PlayerPrefs.GetInt("VoiceInstructions", 1) == 1;
        motionTasksToggle.isOn = PlayerPrefs.GetInt("MotionTasks", 1) == 1;

        // Suscribirse a los eventos de los toggles
        // voiceInstructionsToggle.onValueChanged.AddListener(OnVoiceInstructionsToggleChanged);
        motionTasksToggle.onValueChanged.AddListener(OnMotionTasksToggleChanged);

        // Asignar el texto de monedas al CurrencyManager
        CurrencyManager.Instance.AssignUIByName("CoinText"); // nombre del TMP en tu Canvas
    }

    private void OnVoiceInstructionsToggleChanged(bool isOn)
    {
        PlayerPrefs.SetInt("VoiceInstructions", isOn ? 1 : 0);
    }

    private void OnMotionTasksToggleChanged(bool isOn)
    {
        PlayerPrefs.SetInt("MotionTasks", isOn ? 1 : 0);
    }
}
