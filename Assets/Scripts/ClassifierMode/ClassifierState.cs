using System;
using System.Collections;
using UnityEngine;

public class ClassifierState : MonoBehaviour
{
    public static ClassifierState Instance { get; private set; }

    public event EventHandler OnPlayingClassifierGame;

    public enum ClassiferGameStateEnum
    {
        None,
        Countdown,
        Go,
        Playing,
        GameOver,
    }

    public ClassiferGameStateEnum classifierGameState;

    private float countDownTimer = 3f;

    [SerializeField] private float goDuration = 0.7f;

    private void Awake()
    {
        Instance = this;
        classifierGameState = ClassiferGameStateEnum.None;
        countDownTimer = 3f;
    }

    private void Update()
    {
        UpdateState();
    }

    public void StartCountdown()
    {
        classifierGameState = ClassiferGameStateEnum.Countdown;
        countDownTimer = 3f;

        if (ClassifierCountdownUI.Instance != null)
            ClassifierCountdownUI.Instance.Show();
    }

    private void UpdateState()
    {
        switch (classifierGameState)
        {
            case ClassiferGameStateEnum.Countdown:
                HandleCountdown();
                break;
        }
    }

    private void HandleCountdown()
    {
        countDownTimer -= Time.deltaTime;

        if (countDownTimer <= 0f)
        {
            countDownTimer = 0f;
            classifierGameState = ClassiferGameStateEnum.Go;

            // El "GO" lo gestiona la UI y al terminar debe llamar a StartGameAfterGo()
            if (ClassifierCountdownUI.Instance != null)
            {
                ClassifierCountdownUI.Instance.ShowGo(goDuration);
            }
            else
            {
                StartCoroutine(GoFallbackThenPlay());
            }
        }
    }

    private IEnumerator GoFallbackThenPlay()
    {
        yield return new WaitForSeconds(goDuration);
        StartGameAfterGo();
    }

    public void StartGameAfterGo()
    {
        if (classifierGameState == ClassiferGameStateEnum.Playing) return;

        classifierGameState = ClassiferGameStateEnum.Playing;
        OnPlayingClassifierGame?.Invoke(this, EventArgs.Empty);
    }

    public float GetCountDownTimer() => countDownTimer;

    public void SetGameOver()
    {
        classifierGameState = ClassiferGameStateEnum.GameOver;
    }
}