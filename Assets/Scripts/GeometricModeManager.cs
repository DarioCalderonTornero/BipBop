using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GeometricModeManager : MonoBehaviour
{
    public static GeometricModeManager Instance { get; private set; }

    [Header("UI Elements")]
    public TextMeshProUGUI instructionText;  
    public TextMeshProUGUI scoreText;        
    public Slider timeSlider;                
    public float startTime = 5f;            

    [Header("Game Settings")]
    public float speedMultiplier = 1f;
    public float speedIncreaseFactor = 1.1f; 
    public float timeDecreaseFactor = 0.95f; 

    [Header("Shapes")]
    public List<BouncingShape> shapes;       

    private float currentTime;
    private int score = 0;
    private BouncingShape currentTarget;

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
        ChooseNewTarget();
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

    
    public void OnShapeTapped(BouncingShape shape)
    {
        if (shape == currentTarget)
        {
            if (shape == currentTarget)
            {
                shape.TemporarilyChangeColor(Color.green, 1f);
                score++;
                UpdateScoreText();

                startTime = Mathf.Max(2.5f, startTime - 0.1f);
                currentTime = startTime; 

                speedMultiplier = Mathf.Min(2f, speedMultiplier * speedIncreaseFactor);
                UpdateShapesSpeed();

                ChooseNewTarget();
            }
            else
            {
                shape.TemporarilyChangeColor(Color.red, 1f);
            }
        }
    }

    private void UpdateScoreText()
    {
        scoreText.text = "Puntos: " + score;
    }

    private void ChooseNewTarget()
    {
        foreach (BouncingShape s in shapes)
        {
            s.SetNormalColor();
        }

        int index = Random.Range(0, shapes.Count);
        currentTarget = shapes[index];
        instructionText.text = "¡Toca " + currentTarget.shapeName + "!";
    }

    private void UpdateShapesSpeed()
    {
        foreach (BouncingShape s in shapes)
        {
            s.UpdateSpeed(speedMultiplier);
        }
    }

    private void EndGame()
    {
        SceneManager.LoadScene("Menu");  
    }
}
