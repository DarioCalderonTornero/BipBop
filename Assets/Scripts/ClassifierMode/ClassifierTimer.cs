using UnityEngine;
using UnityEngine.UI;
using System;

public class ClassifierTimer : MonoBehaviour
{
    [Header("Configuración de Tiempo")]
    public Image timerBar;
    public float initialMaxTime = 7f;
    public float timeDecreasePerHit = 0.1f;
    public float minTimeLimit = 2f;

    // Evento que avisará a otros scripts cuando el tiempo se acabe
    public event EventHandler OnTimeOut;

    private float currentMaxTime;
    private float currentTimeRemaining;
    private bool isRunning = false;

    void Awake()
    {
        currentMaxTime = initialMaxTime;
    }

    void Update()
    {
        if (!isRunning) return;

        currentTimeRemaining -= Time.deltaTime;

        if (timerBar != null)
        {
            timerBar.fillAmount = currentTimeRemaining / currentMaxTime;
        }

        if (currentTimeRemaining <= 0)
        {
            currentTimeRemaining = 0;
            isRunning = false;
            OnTimeOut?.Invoke(this, EventArgs.Empty); // Disparamos el evento de que hemos perdido
        }
    }

    public void ResetAndStartTimer()
    {
        currentTimeRemaining = currentMaxTime;
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void ApplySuccessReduction()
    {
        currentMaxTime -= timeDecreasePerHit;
        if (currentMaxTime < minTimeLimit)
        {
            currentMaxTime = minTimeLimit;
        }
    }
}