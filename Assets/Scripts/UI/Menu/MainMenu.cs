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

    // [SerializeField] private Toggle voiceInstructionsToggle;
    [SerializeField] private Toggle motionTasksToggle;

    public event EventHandler OnPlayButton;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Cargar configuraciones guardadas
        motionTasksToggle.isOn = PlayerPrefs.GetInt("MotionTasks", 1) == 1;

        // Suscribirse a los eventos de los toggles
        motionTasksToggle.onValueChanged.AddListener(OnMotionTasksToggleChanged);
    }

    private void OnMotionTasksToggleChanged(bool isOn)
    {
        PlayerPrefs.SetInt("MotionTasks", isOn ? 1 : 0);
    }
}
