using System;
using UnityEngine;

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
    }

    private void Start()
    {
        gridGameState = GridGameStateEnum.Countdown;
        countDownTimer = 3f;

        if (GridCountDownUI.Instance != null)
        {
            GridCountDownUI.Instance.Show();
        }

        // Empezar la caída del personaje durante la cuenta atrás
        if (GridGameManager.Instance != null)
        {
            GridGameManager.Instance.StartIntroDropDuringCountdown();
        }
    }

    private void Update()
    {
        GridGameState();
    }

    private void GridGameState()
    {
        switch (gridGameState)
        {
            case GridGameStateEnum.Countdown:
                countDownTimer -= Time.deltaTime;
                timer = timerMax;

                if (countDownTimer <= 0f)
                {
                    gridGameState = GridGameStateEnum.Go;

                    if (GridCountDownUI.Instance != null)
                    {
                        GridCountDownUI.Instance.ShowGo(0.7f);
                    }

                    // Cuando termina la cuenta atrás: primera gema + flechas
                    if (GridGameManager.Instance != null)
                    {
                        GridGameManager.Instance.StartGameplayAfterCountdown();
                    }
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

    public float GetCountDownTimer()
    {
        return countDownTimer;
    }
    public void StartGameAfterGo()
    {
        // Llamado desde la UI del "GO" cuando desaparece:
        gridGameState = GridGameStateEnum.Playing;
    }
}
