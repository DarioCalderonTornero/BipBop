using System;
using System.Collections;
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
        differentGameState = DifferentGameStateEnum.None;
        countDownTimer = 3f;
    }

    private void Start()
    {
        // Si alguien ya arrancó el countdown (por ejemplo, DifferentManager.StartGame),
        // NO lo pises.
        if (differentGameState != DifferentGameStateEnum.None)
            return;

        differentGameState = DifferentGameStateEnum.None;
        countDownTimer = 3f;

        if (DifferentManager.Instance != null)
            DifferentManager.Instance.PauseGameplay();

        if (DifferentCountDownUI.Instance != null)
            DifferentCountDownUI.Instance.Hide();
    }

    private void Update()
    {
        UpdateState();
    }

    private void UpdateState()
    {
        switch (differentGameState)
        {
            case DifferentGameStateEnum.Countdown:
                HandleCountdown();
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

    private void HandleCountdown()
    {
        countDownTimer -= Time.deltaTime;

        if (countDownTimer <= 0f)
        {
            countDownTimer = 0f;
            differentGameState = DifferentGameStateEnum.Go;

            if (DifferentCountDownUI.Instance != null)
                DifferentCountDownUI.Instance.ShowGo(0.7f);

            StartCoroutine(StartAfterGoRoutine(0.7f));
        }
    }

    private IEnumerator StartAfterGoRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartGameAfterGo();
    }

    public void StartGameAfterGo()
    {
        differentGameState = DifferentGameStateEnum.Playing;

        if (DifferentManager.Instance != null)
            DifferentManager.Instance.OnCountdownFinishedStartPlaying();
    }

    public float GetCountDownTimer()
    {
        return countDownTimer;
    }

    // Por si quieres forzar GameOver desde fuera
    public void SetGameOver()
    {
        differentGameState = DifferentGameStateEnum.GameOver;
    }

    public void StartCountdown()
    {
        differentGameState = DifferentGameStateEnum.Countdown;
        countDownTimer = 3f;

        if (DifferentManager.Instance != null)
            DifferentManager.Instance.PauseGameplay();

        if (DifferentCountDownUI.Instance != null)
            DifferentCountDownUI.Instance.Show();
    }
}
