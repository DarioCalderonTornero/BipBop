using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Toggle voiceInstructionsToggle;
    [SerializeField] private Toggle motionTasksToggle;
    [SerializeField] private TextMeshProUGUI coinText; // Texto que mostrará las monedas acumuladas

    private void Awake()
    {
        playButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("GameScene");
        });
    }

    private void Start()
    {
        // Cargar configuraciones guardadas
        voiceInstructionsToggle.isOn = PlayerPrefs.GetInt("VoiceInstructions", 1) == 1;
        motionTasksToggle.isOn = PlayerPrefs.GetInt("MotionTasks", 1) == 1;

        // Suscribirse a los eventos de los toggles
        voiceInstructionsToggle.onValueChanged.AddListener(OnVoiceInstructionsToggleChanged);
        motionTasksToggle.onValueChanged.AddListener(OnMotionTasksToggleChanged);

        // Actualizar el texto de monedas
        UpdateCoinText();
    }

    private void OnVoiceInstructionsToggleChanged(bool isOn)
    {
        PlayerPrefs.SetInt("VoiceInstructions", isOn ? 1 : 0);
    }

    private void OnMotionTasksToggleChanged(bool isOn)
    {
        PlayerPrefs.SetInt("MotionTasks", isOn ? 1 : 0);
    }

    private void UpdateCoinText()
    {
        // Recupera el total de monedas guardadas (por defecto 0)
        int totalCoins = PlayerPrefs.GetInt("CoinCount", 0);
        coinText.text = "Monedas: " + totalCoins;
    }

}
