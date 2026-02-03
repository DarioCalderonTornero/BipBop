using System;
using UnityEngine;
using System.Collections;

public class GridState : MonoBehaviour
{
    public static GridState Instance { get; private set; }

    public event EventHandler OnPlayinGridGame;
    public event EventHandler OnGameOverGridGame;

    public enum GridGameStateEnum
    {
        None,
        Countdown,
        Go,
        Playing,
        GameOver,
    }

    public GridGameStateEnum gridGameState;

    [SerializeField] private float timer;
    [SerializeField] private float timerMax = 10f;

    private float countDownTimer = 3f;

    private void Awake()
    {
        Instance = this;
        gridGameState = GridGameStateEnum.None;
        countDownTimer = 3f;
    }

    private void Start()
    {
        // ✅ NO arrancar aquí. Espera a que GridGameManager decida (tutorial ON/OFF)
        // Esto evita el bug de order-of-execution.
    }

    private void Update()
    {
        GridGameState();
    }

    public void StartCountdown()
    {
        gridGameState = GridGameStateEnum.Countdown;
        countDownTimer = 3f;

        if (GridCountDownUI.Instance != null)
            GridCountDownUI.Instance.Show();

        // ✅ caída durante la cuenta atrás
        if (GridGameManager.Instance != null)
            GridGameManager.Instance.StartIntroDropDuringCountdown();
    }

    private void GridGameState()
    {
        switch (gridGameState)
        {
            case GridGameStateEnum.None:
                break;

            case GridGameStateEnum.Countdown:
                countDownTimer -= Time.deltaTime;
                timer = timerMax;

                if (countDownTimer <= 0f)
                {
                    countDownTimer = 0f;
                    gridGameState = GridGameStateEnum.Go;

                    if (GridCountDownUI.Instance != null)
                        GridCountDownUI.Instance.ShowGo(0.7f);
                }
                break;

            case GridGameStateEnum.Go:
                break;

            case GridGameStateEnum.Playing:
                OnPlayinGridGame?.Invoke(this, EventArgs.Empty);
                break;

            case GridGameStateEnum.GameOver:
                OnGameOverGridGame?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    public float GetCountDownTimer() => countDownTimer;

    public void StartGameAfterGo()
    {
        gridGameState = GridGameStateEnum.Playing;

        // ✅ arrancar gameplay REAL justo al terminar GO
        if (GridGameManager.Instance != null)
            GridGameManager.Instance.StartGameplayAfterCountdown();
    }
}
