using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LogicaPuntos : MonoBehaviour
{
    public static LogicaPuntos Instance { get; private set; }

    public TextMeshProUGUI instructionText; // Texto para mostrar la instrucción
    public TextMeshProUGUI timeText; // Texto para mostrar el tiempo restante
    public TextMeshProUGUI scoreText; // Texto para mostrar la puntuación
    public float startTime = 100f; // Tiempo inicial en segundos
    private float currentTime;
    private bool isGameActive = true;
    private int score = 0; // Puntuación inicial

    public event EventHandler OnGameOver;

    private bool isTaskCompleted = false; // Verifica si la tarea actual ya fue completada

    private string currentTask; // Tarea actual
    private string[] tasks = { "¡Toca la pantalla!", "¡Haz zoom!", "¡Agita el teléfono!", "¡Ponlo boca abajo!" };

    private void Awake()
    {
        Instance = this;
        instructionText.text = "";
        scoreText.text = "Puntos: 0"; // Inicializa el texto de la puntuación
    }

    void Start()
    {
        // Inicia el juego con una nueva tarea
        StartNewTask();
    }

    void Update()
    {
        if (!isGameActive)
            return;

        // Reduce el tiempo restante en función del tiempo real transcurrido
        currentTime -= Time.deltaTime;

        // Actualiza el texto del temporizador
        UpdateTimeText();

        // Si el tiempo se acaba, termina el juego
        if (currentTime <= 0f)
        {
            isGameActive = false;
            instructionText.text = "";
            OnGameOver?.Invoke(this, EventArgs.Empty);
        }
    }

    public void OnTaskAction(string action)
    {
        if (!isGameActive || isTaskCompleted)
            return;

        // Verifica si la acción corresponde a la tarea actual
        if (action == currentTask) // Solo acepta acciones que coincidan exactamente
        {
            isTaskCompleted = true; // Marca la tarea como completada
            StartNewTask();
            isTaskCompleted = false; // Resetea la bandera para la próxima tarea
        }
    }

    private void StartNewTask()
    {
        // Incrementa la puntuación
        score++;
        UpdateScoreText();

        // Genera una nueva tarea aleatoria
        currentTask = tasks[UnityEngine.Random.Range(0, tasks.Length)];
        instructionText.text = currentTask;

        // Reinicia el temporizador al tiempo inicial actual
        currentTime = startTime;

        // Reduce el tiempo inicial para la próxima tarea con un mínimo de 2 segundos
        startTime = Mathf.Max(2f, startTime - 0.1f);

        // Actualiza el texto del temporizador inmediatamente
        UpdateTimeText();
    }

    private void UpdateTimeText()
    {
        // Actualiza el texto en pantalla con el tiempo actual formateado
        timeText.text = "Tiempo: " + currentTime.ToString("F1") + "s";
    }

    private void UpdateScoreText()
    {
        // Actualiza el texto en pantalla con la puntuación actual
        scoreText.text = "Puntos: " + score;
    }

    public int GetScore()
    {
        return score;
    }
}
