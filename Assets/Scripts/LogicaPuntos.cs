using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LogicaPuntos : MonoBehaviour
{
    public static LogicaPuntos Instance { get; private set; }

    public TextMeshProUGUI instructionText; // Texto para mostrar la instrucción
    public TextMeshProUGUI scoreText; // Texto para mostrar la puntuación
    public Slider timeSlider; // Slider para mostrar el tiempo restante
    public float startTime = 100f; // Tiempo inicial en segundos

    private float currentTime;
    private bool isGameActive = true;
    private int score = 0; // Puntuación inicial

    public event EventHandler OnGameOver;

    private bool isTaskCompleted = false; // Verifica si la tarea actual ya fue completada
    private string currentTask; // Tarea actual
    private string lastTask;    // Última tarea realizada
    private string[] tasks = {
        "¡Toca la pantalla!",
        "¡Haz zoom hacia dentro!",
        "¡Haz zoom hacia fuera!",
        "¡Agita el teléfono!",
        "¡Ponlo boca abajo!"
    };

    private void Awake()
    {
        Instance = this;
        instructionText.text = "";
        scoreText.text = "Puntos: 0"; // Inicializa el texto de la puntuación
    }

    void Start()
    {
        // Configura el slider
        timeSlider.maxValue = startTime;
        timeSlider.value = startTime;

        // Inicia el juego con una nueva tarea
        StartNewTask();
    }

    void Update()
    {
        if (!isGameActive)
            return;

        // Reduce el tiempo restante en función del tiempo real transcurrido
        currentTime -= Time.deltaTime;

        // Actualiza el slider con el tiempo restante
        timeSlider.value = currentTime;

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
        score++;
        UpdateScoreText();

        // Filtrar tareas según las configuraciones
        List<string> availableTasks = new List<string>(tasks);

        // Si las tareas de movimiento están deshabilitadas
        if (PlayerPrefs.GetInt("MotionTasks", 1) == 0)
        {
            availableTasks.Remove("¡Agita el teléfono!");
            availableTasks.Remove("¡Ponlo boca abajo!");
        }

        // Seleccionar una nueva tarea diferente
        string newTask;
        do
        {
            newTask = availableTasks[UnityEngine.Random.Range(0, availableTasks.Count)];
        } while (newTask == currentTask);

        currentTask = newTask;
        instructionText.text = currentTask;

        currentTime = startTime;
        startTime = Mathf.Max(2f, startTime - 0.1f);

        // Actualiza el slider con el nuevo tiempo
        timeSlider.maxValue = startTime;
        timeSlider.value = currentTime;
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

    public bool IsGameActive()
    {
        return isGameActive;
    }
}
