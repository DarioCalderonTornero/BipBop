using System;
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

    [SerializeField] private float timer;
    [SerializeField] private float timerMax = 10f;

    private float countDownTimer = 3f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        geometricGameState = GeometricGameStateEnum.Countdown;
        countDownTimer = 3f;

        if (GeometricCountDownUI.Instance != null)
        {
            GeometricCountDownUI.Instance.Show();
        }
    }

    private void Update()
    {
        GeometricGameState();
    }

    private void GeometricGameState()
    {
        switch (geometricGameState)
        {
            case GeometricGameStateEnum.None:
                break;

            case GeometricGameStateEnum.Countdown:
                countDownTimer -= Time.deltaTime;
                timer = timerMax;

                if (countDownTimer <= 0f)
                {
                    geometricGameState = GeometricGameStateEnum.Go;

                    if (GeometricCountDownUI.Instance != null)
                    {
                        GeometricCountDownUI.Instance.ShowGo(0.7f);
                    }
                }
                break;

            case GeometricGameStateEnum.Go:
                break;

            case GeometricGameStateEnum.Playing:
                OnPlayingGeometricGame?.Invoke(this, EventArgs.Empty);
                break;

            case GeometricGameStateEnum.GameOver:
                OnGameOverGeometricGame?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    public float GetCountDownTimer()
    {
        return countDownTimer;
    }
    public void StartGameAfterGo()
    {
        geometricGameState = GeometricGameStateEnum.Playing;
    }
}
