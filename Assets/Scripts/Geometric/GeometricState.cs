// GeometricState.cs
using System;
using System.Collections;
using UnityEngine;

public class GeometricState : MonoBehaviour
{
    public static GeometricState Instance { get; private set; }

    public event EventHandler OnPlayingGeometricGame;
    public event EventHandler OnGameOverGeometricGame;

    public enum GeometricGameStateEnum
    {
        None,
        Countdown,
        Go,
        Playing,
        GameOver,
    }

    public GeometricGameStateEnum geometricGameState;

    private float countDownTimer = 3f;

    [SerializeField] private float goDuration = 0.7f;
    private Coroutine goRoutine;

    private void Awake()
    {
        Instance = this;
        geometricGameState = GeometricGameStateEnum.None;
        countDownTimer = 3f;
    }

    private void Start()
    {
        // No auto-arrancar aquí: lo arranca el Manager después del tutorial
        /* if (GeometricCountDownUI.Instance != null)
            GeometricCountDownUI.Instance.Hide();*/
    }

    private void Update()
    {
        UpdateState();
    }

    public void StartCountdown()
    {
        geometricGameState = GeometricGameStateEnum.Countdown;
        countDownTimer = 3f;

        if (GeometricCountDownUI.Instance != null)
            GeometricCountDownUI.Instance.Show();
    }

    private void UpdateState()
    {
        switch (geometricGameState)
        {
            case GeometricGameStateEnum.Countdown:
                HandleCountdown();
                break;

            case GeometricGameStateEnum.Playing:
                OnPlayingGeometricGame?.Invoke(this, EventArgs.Empty);
                break;

            case GeometricGameStateEnum.GameOver:
                OnGameOverGeometricGame?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    private void HandleCountdown()
    {
        countDownTimer -= Time.deltaTime;

        if (countDownTimer <= 0f)
        {
            countDownTimer = 0f;
            geometricGameState = GeometricGameStateEnum.Go;

            if (GeometricCountDownUI.Instance != null)
                GeometricCountDownUI.Instance.ShowGo(goDuration);

            if (goRoutine != null) StopCoroutine(goRoutine);
            goRoutine = StartCoroutine(GoThenPlay());
        }
    }

    private IEnumerator GoThenPlay()
    {
        yield return new WaitForSeconds(goDuration);
        StartGameAfterGo();
        goRoutine = null;
    }

    public void StartGameAfterGo()
    {
        geometricGameState = GeometricGameStateEnum.Playing;
    }

    public float GetCountDownTimer() => countDownTimer;

    public void SetGameOver()
    {
        geometricGameState = GeometricGameStateEnum.GameOver;
    }
}
