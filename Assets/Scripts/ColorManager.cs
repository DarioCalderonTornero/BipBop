using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ColorManager : MonoBehaviour
{
    public static ColorManager Instance { get; private set; }

    [Header("UI Elements")]
    public TextMeshProUGUI colorWordText;  // Muestra el nombre del color que se debe seleccionar
    public TextMeshProUGUI scoreText;        // Puntuación actual
    public Slider timeSlider;                // Barra de tiempo
    public float startTime = 60f;            // Tiempo inicial

    [Header("Candidate Buttons")]
    public List<Button> candidateButtons;    // Debe tener 6 botones, cada uno mostrando un color

    [Header("Color Data")]
    // 6 colores y sus nombres
    public string[] colorNames = { "Rojo", "Azul", "Verde", "Amarillo", "Morado", "Naranja", "Marrón", "Negro", "Blanco" };
    public Color[] colorValues = { Color.red, Color.blue, Color.green, Color.yellow, new Color(0.5f, 0f, 0.5f), new Color(1f, 0.5f, 0f),
                                new Color(0.6f, 0.3f, 0.1f), Color.black, Color.white };

    private float currentTime;
    private int score = 0;
    private int correctIndex; // Índice del color correcto en nuestros arrays

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        currentTime = startTime;
        timeSlider.maxValue = startTime;
        timeSlider.value = startTime;
        UpdateScoreText();
        SetupRound();
    }

    private void Update()
    {
        currentTime -= Time.deltaTime;
        timeSlider.value = currentTime;
        timeSlider.maxValue = startTime;
        if (currentTime <= 0f)
        {
            EndGame();
        }
    }

    // Configura una nueva ronda
    private void SetupRound()
    {
        // Determinar cuántos colores están disponibles según la puntuación
        int availableCount = 6; // siempre disponibles: índices 0-5
        if (score >= 10) availableCount = 7; // añade el marrón (índice 6)
        if (score >= 20) availableCount = 8; // añade el negro (índice 7)
        if (score >= 30) availableCount = 9; // añade el blanco (índice 8)

        // 1. Elegir aleatoriamente el índice correcto (entre 0 y availableCount-1)
        correctIndex = Random.Range(0, availableCount);
        string correctColorName = colorNames[correctIndex];

        // 2. Elegir un color de texto (para pintar el nombre) que no sea el correcto, entre los disponibles
        int textColorIndex;
        do
        {
            textColorIndex = Random.Range(0, availableCount);
        } while (textColorIndex == correctIndex);
        Color displayColor = colorValues[textColorIndex];

        // Configurar el texto: el contenido es el nombre correcto, pero se muestra en displayColor
        colorWordText.text = correctColorName;
        colorWordText.color = displayColor;

        // 3. Preparar las opciones para los botones
        // Queremos que entre las 4 opciones aparezcan:
        // - El color correcto (correctIndex)
        // - El color de texto (textColorIndex)
        // - 2 opciones adicionales elegidas aleatoriamente de entre los disponibles, sin repetir los anteriores
        List<int> candidateIndices = new List<int>();
        candidateIndices.Add(correctIndex);
        candidateIndices.Add(textColorIndex);

        // Crear una lista de índices disponibles que NO sean ya usados
        List<int> remainingIndices = new List<int>();
        for (int i = 0; i < availableCount; i++)
        {
            if (i != correctIndex && i != textColorIndex)
                remainingIndices.Add(i);
        }
        // Barajar la lista de índices restantes
        for (int i = 0; i < remainingIndices.Count; i++)
        {
            int randomIndex = Random.Range(i, remainingIndices.Count);
            int temp = remainingIndices[i];
            remainingIndices[i] = remainingIndices[randomIndex];
            remainingIndices[randomIndex] = temp;
        }
        // Tomar los dos primeros de la lista barajada y añadirlos a las opciones
        if (remainingIndices.Count >= 2)
        {
            candidateIndices.Add(remainingIndices[0]);
            candidateIndices.Add(remainingIndices[1]);
        }
        else if (remainingIndices.Count == 1)
        {
            candidateIndices.Add(remainingIndices[0]);
        }

        // Barajar la lista final de 4 opciones para que el orden sea aleatorio
        for (int i = 0; i < candidateIndices.Count; i++)
        {
            int randomIndex = Random.Range(i, candidateIndices.Count);
            int temp = candidateIndices[i];
            candidateIndices[i] = candidateIndices[randomIndex];
            candidateIndices[randomIndex] = temp;
        }

        // 4. Asignar las opciones a los botones (asumiendo que candidateButtons tiene 4 botones)
        for (int i = 0; i < candidateButtons.Count; i++)
        {
            int assignedIndex = candidateIndices[i];
            candidateButtons[i].image.color = colorValues[assignedIndex];
            candidateButtons[i].onClick.RemoveAllListeners();
            int indexCaptured = assignedIndex; // Captura para el closure
            candidateButtons[i].onClick.AddListener(() => OnCandidateSelected(indexCaptured));
        }
    }

    // Llamado cuando se selecciona un botón
    public void OnCandidateSelected(int selectedIndex)
    {
        if (selectedIndex == correctIndex)
        {
            // Respuesta correcta: se suma un punto y se reinicia la ronda.
            score++;
            UpdateScoreText();

            // Ajustar el tiempo: se reduce en 0.1, pero nunca por debajo de 2.5
            startTime = Mathf.Max(2.5f, startTime - 0.1f);
            currentTime = startTime;

            // Aquí podrías incrementar la dificultad de otras formas (por ejemplo, velocidad, etc.)
            SetupRound();
        }
        else
        {
            // Respuesta incorrecta: puedes dar feedback (por ejemplo, sonido o animación).
            // Por ahora, no se suma nada.
        }
    }

    private void UpdateScoreText()
    {
        scoreText.text = "Puntos: " + score;
    }

    private void EndGame()
    {
        // Aquí se podría guardar el récord, etc.
        SceneManager.LoadScene("Menu");
    }
}
