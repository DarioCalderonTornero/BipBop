using System;
using UnityEngine;

public class DifferentState : MonoBehaviour
{
    public static DifferentState Instance { get; private set; }

    public event EventHandler OnPlayingDifferentGame;
    public event EventHandler OnGameOverDifferentGame;

    public enum DifferentGameStateEnum
    {
        None,
        Countdown,
        Go,
        Playing,
        GameOver,
    }

    public DifferentGameStateEnum differentGameState;

    [SerializeField] private float timer;
    [SerializeField] private float timerMax = 10f;

    private float countDownTimer = 3f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        differentGameState = DifferentGameStateEnum.Countdown;
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
        switch (differentGameState)
        {
            case DifferentGameStateEnum.Countdown:
                countDownTimer -= Time.deltaTime;
                timer = timerMax;

                if (countDownTimer <= 0f)
                {
                    differentGameState = DifferentGameStateEnum.Go;

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

            case DifferentGameStateEnum.Go:
                break;

            case DifferentGameStateEnum.Playing:
                OnPlayingDifferentGame?.Invoke(this, EventArgs.Empty);
                break;

            case DifferentGameStateEnum.GameOver:
                OnGameOverDifferentGame?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    public float GetCountDownTimer()
    {
        return countDownTimer;
    }
    public void StartGameAfterGo()
    {
        differentGameState = DifferentGameStateEnum.Playing;
    }
}
