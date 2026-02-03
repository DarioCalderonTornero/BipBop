// GameStates.cs
using System;
using System.Collections;
using UnityEngine;

public class GameStates : MonoBehaviour
{
    public static GameStates Instance { get; private set; }

    public event EventHandler OnCountDown;
    public event EventHandler OnPlaying;
    public event EventHandler OnGameOver;

    private float countDownTimer = 3f;

    public enum States { None, CountDown, Go, Playing, GameOver }
    private States states = States.None;

    [SerializeField] private float goDuration = 0.8f;

    private Coroutine goRoutine;

    private void Awake()
    {
        Instance = this;
        states = States.None;
        countDownTimer = 3f;
    }

    private void Update()
    {
        GameState();
    }

    public void StartCountdown()
    {
        states = States.CountDown;
        countDownTimer = 3f;

        if (LogicaJuego.Instance != null)
            LogicaJuego.Instance.PauseGameplay();
    }

    private void GameState()
    {
        switch (states)
        {
            case States.CountDown:
                OnCountDown?.Invoke(this, EventArgs.Empty);
                countDownTimer -= Time.deltaTime;

                if (countDownTimer <= 0f)
                {
                    countDownTimer = 0f;
                    states = States.Go;

                    // Mostrar GO aquí (UI)
                    if (CountDownUI.Instance != null && LogicaJuego.Instance != null)
                        CountDownUI.Instance.ShowMessage(LogicaJuego.Instance.goText.GetLocalizedString());

                    if (goRoutine != null) StopCoroutine(goRoutine);
                    goRoutine = StartCoroutine(GoThenPlay());
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

    private IEnumerator GoThenPlay()
    {
        yield return new WaitForSeconds(goDuration);
        states = States.Playing;

        if (LogicaJuego.Instance != null)
            LogicaJuego.Instance.OnCountdownFinishedStartPlaying();

        goRoutine = null;
    }

    public bool PlayGame() => states == States.Playing;
    public bool CountDown() => states == States.CountDown || states == States.Go;
    public float GetCountDownTime() => countDownTimer;

    // Por si quieres forzar GameOver desde fuera
    public void SetGameOver()
    {
        states = States.GameOver;
    }
}
