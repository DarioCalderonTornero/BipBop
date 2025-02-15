using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameStates : MonoBehaviour
{
    public static GameStates Instance {  get; private set; }

    public event EventHandler OnCountDown;
    public event EventHandler OnPlaying;
    public event EventHandler OnGameOver;

    [SerializeField] private float timer;
    [SerializeField] private float timerMax = 10f;

    private float countDownTimer = 3f;

    public enum States { CountDown, Playing, GameOver }
    private States states;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
       //MainMenu.Instance.OnPlayButton += MainMenu_OnPlayButton;
    }

    private void Update()
    {
        GameState();
    }




    private void GameState()
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
                break;

            case States.GameOver:
                OnGameOver?.Invoke(this, EventArgs.Empty);
                break;

        }
    }

    public float GetGameTimerUI()
    {
        return 1 - (timer / timerMax);
    }

    public bool PlayGame()
    {
        return states == States.Playing;
    }

    public bool countDown()
    {
        return states == States.CountDown;
    }
}
