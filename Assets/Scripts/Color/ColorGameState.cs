using System;
using System.Collections;
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
        Go,
        Playing,
        GameOver,
    }

    public ColorGameStateEnum colorGameState;

    private float countDownTimer = 3f;

    [SerializeField] private float goDuration = 0.7f;

    private Coroutine goRoutine;
    private Coroutine ensureUiRoutine;

    private void Awake()
    {
        Instance = this;
        colorGameState = ColorGameStateEnum.None;
        countDownTimer = 3f;
    }

    private void Start()
    {
        // ❌ NO auto StartCountdown aquí (lo arranca el manager después del tutorial)
        /* if (ColorCountDownUI.Instance != null)
            ColorCountDownUI.Instance.Hide();*/
    }

    private void Update()
    {
        UpdateState();
    }

    public void StartCountdown()
    {
        colorGameState = ColorGameStateEnum.Countdown;
        countDownTimer = 3f;

        // intento inmediato
        if (ColorCountDownUI.Instance != null)
        {
            ColorCountDownUI.Instance.Show();
        }
        else
        {
            if (ensureUiRoutine != null) StopCoroutine(ensureUiRoutine);
            ensureUiRoutine = StartCoroutine(EnsureCountdownUIShown());
        }
    }

    private IEnumerator EnsureCountdownUIShown()
    {
        yield return null;

        if (colorGameState == ColorGameStateEnum.Countdown &&
            ColorCountDownUI.Instance != null)
        {
            ColorCountDownUI.Instance.Show();
        }

        ensureUiRoutine = null;
    }

    private void UpdateState()
    {
        switch (colorGameState)
        {
            case ColorGameStateEnum.Countdown:
                HandleCountdown();
                break;

            case ColorGameStateEnum.Playing:
                OnPlayingColorGame?.Invoke(this, EventArgs.Empty);
                break;

            case ColorGameStateEnum.GameOver:
                OnGameOverColorGame?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    private void HandleCountdown()
    {
        countDownTimer -= Time.deltaTime;

        if (countDownTimer <= 0f)
        {
            countDownTimer = 0f;
            colorGameState = ColorGameStateEnum.Go;

            if (ensureUiRoutine != null) { StopCoroutine(ensureUiRoutine); ensureUiRoutine = null; }

            if (ColorCountDownUI.Instance != null)
                ColorCountDownUI.Instance.ShowGo(goDuration);

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

    public float GetCountDownTimer() => countDownTimer;

    public void StartGameAfterGo()
    {
        colorGameState = ColorGameStateEnum.Playing;
    }

    public void SetGameOver()
    {
        colorGameState = ColorGameStateEnum.GameOver;
    }
}
