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
    private Coroutine goRoutine;

    private void Awake()
    {
        Instance = this;
        classifierGameState = ClassiferGameStateEnum.None;
        countDownTimer = 3f;
    }

    private void Start()
    {
        StartCountdown();
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

            case ClassiferGameStateEnum.Playing:
                break;

            case ClassiferGameStateEnum.GameOver:
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

            if (ClassifierCountdownUI.Instance != null)
                ClassifierCountdownUI.Instance.ShowGo(goDuration);

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
        classifierGameState = ClassiferGameStateEnum.Playing;
        OnPlayingClassifierGame?.Invoke(this, EventArgs.Empty);
    }

    public float GetCountDownTimer() => countDownTimer;

    public void SetGameOver()
    {
        classifierGameState = ClassiferGameStateEnum.GameOver;
    }
}

