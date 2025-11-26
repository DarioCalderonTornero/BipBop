using System;
using UnityEngine;

public class DodgeState : MonoBehaviour
{
    public static DodgeState Instance { get; private set; }

    public event EventHandler OnPlayingDodgeGame;
    public event EventHandler OnGameOverDodgeGame;

    public enum DodgeGameStateEnum
    {
        None,
        Countdown,
        Go,
        Playing,
        GameOver,
    }

    public DodgeGameStateEnum dodgeGameState;

    [SerializeField] private float timer;
    [SerializeField] private float timerMax = 10f;

    private float countDownTimer = 3f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        dodgeGameState = DodgeGameStateEnum.Countdown;
        countDownTimer = 3f;

        if (DodgeCountDownUI.Instance != null)
        {
            DodgeCountDownUI.Instance.Show();
        }
    }

    private void Update()
    {
        DodgeGameState();
    }

    private void DodgeGameState()
    {
        switch (dodgeGameState)
        {
            case DodgeGameStateEnum.None:
                break;

            case DodgeGameStateEnum.Countdown:
                countDownTimer -= Time.deltaTime;
                timer = timerMax;

                if (countDownTimer <= 0f)
                {
                    dodgeGameState = DodgeGameStateEnum.Go;

                    if (DodgeCountDownUI.Instance != null)
                    {
                        DodgeCountDownUI.Instance.ShowGo(0.7f);
                    }
                }
                break;

            case DodgeGameStateEnum.Go:
                break;

            case DodgeGameStateEnum.Playing:
                OnPlayingDodgeGame?.Invoke(this, EventArgs.Empty);
                break;

            case DodgeGameStateEnum.GameOver:
                OnGameOverDodgeGame?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    public float GetCountDownTimer()
    {
        return countDownTimer;
    }
    public void StartGameAfterGo()
    {
        dodgeGameState = DodgeGameStateEnum.Playing;
    }
}
