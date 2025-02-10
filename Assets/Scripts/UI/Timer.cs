using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    [SerializeField] private float timer;
    [SerializeField] private float timerMax = 10f;
    [SerializeField] private Image timerImage;

    public event EventHandler OnCountDown;
    public event EventHandler OnPlaying;
    public event EventHandler OnGameOver;

    private float countDownTimer = 3f;

    public enum States { CountDown, Playing, GameOver }
    private States states;

    private void Awake()
    {
        timerImage.fillAmount = 0;
    }

    private void Update()
    {

        switch (states)
        {
            case States.CountDown:
                OnCountDown?.Invoke(this, EventArgs.Empty);
                countDownTimer -= Time.deltaTime;
                timer = timerMax;
                if (countDownTimer < 0)
                {
                    states = States.Playing;
                }
                break;

            case States.Playing:
                OnPlaying?.Invoke(this, EventArgs.Empty);   
                timer -= Time.deltaTime;
                if (timer < 0)
                {
                    states = States.GameOver;
                }
                break;

            case States.GameOver:
                OnGameOver?.Invoke(this, EventArgs.Empty);
                break;



        }

        timerImage.fillAmount = GetGameTimerUI();
    }

    private float GetGameTimerUI()
    {
        return 1 - (timer / timerMax);
    }
}
