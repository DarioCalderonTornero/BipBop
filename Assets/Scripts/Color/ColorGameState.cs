using System;
using UnityEngine;

public class ColorGameState : MonoBehaviour
{
    public static ColorGameState Instance { get; private set; }

    public event EventHandler OnPlayingColorGame;
    public event EventHandler OnGameOverColorGame;

    public enum ColorGameStateEnum
    {
        None,
        Countdown,
        Playing,
        GameOver,
    }

    public ColorGameStateEnum colorGameState;

    [SerializeField] private float timer;
    [SerializeField] private float timerMax = 10f;

    private float countDownTimer = 3f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        colorGameState = ColorGameStateEnum.Countdown;
        countDownTimer = 3f;

        if (ColorCountDownUI.Instance != null)
        {
            ColorCountDownUI.Instance.Show();
        }
    }

    private void Update()
    {
        ColorGameStates();
    }

    private void ColorGameStates()
    {
        switch (colorGameState)
        {
            case ColorGameStateEnum.None:
                break;

            case ColorGameStateEnum.Countdown:
                countDownTimer -= Time.deltaTime;
                timer = timerMax;

                if (countDownTimer <= 0f)
                {
                    colorGameState = ColorGameStateEnum.Playing;
                }
                break;

            case ColorGameStateEnum.Playing:
                OnPlayingColorGame?.Invoke(this, EventArgs.Empty);
                break;

            case ColorGameStateEnum.GameOver:
                OnGameOverColorGame?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    public float GetCountDownTimer()
    {
        return countDownTimer;
    }
}
