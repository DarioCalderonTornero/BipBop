using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DailyMissionsTimer : MonoBehaviour
{
    public static DailyMissionsTimer Instance;

    [Header("Debug")]
    [SerializeField] private bool useCustomResetTime = false;
    [SerializeField] private int resetHour = 0;
    [SerializeField] private int resetMinute = 0;
    [SerializeField] private bool forceResetForDebug = false;

    public DateTime NextResetTime { get; private set; }
    public event Action OnResetReached;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Cargar siguiente reset
            if (PlayerPrefs.HasKey("DailyMissionsNextReset"))
                NextResetTime = DateTime.Parse(PlayerPrefs.GetString("DailyMissionsNextReset"));
            else
                NextResetTime = CalculateNextReset();

            if (forceResetForDebug)
                NextResetTime = DateTime.Now.AddSeconds(5);

            PlayerPrefs.SetString("DailyMissionsNextReset", NextResetTime.ToString("O"));
            PlayerPrefs.Save();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Ya no hace falta buscar ningún TextMeshPro aquí
    }

    private void Update()
    {
        TimeSpan remaining = NextResetTime - DateTime.Now;

        if (remaining.TotalSeconds <= 0)
        {
            NextResetTime = CalculateNextReset();
            PlayerPrefs.SetString("DailyMissionsNextReset", NextResetTime.ToString("O"));
            PlayerPrefs.Save();

            OnResetReached?.Invoke();

            // Reset de misiones
            if (DailyMissionManager.Instance != null)
                DailyMissionManager.Instance.ResetMissions();

            remaining = NextResetTime - DateTime.Now; // recalcular
        }

        // MPORTANTE: ya NO escribimos en ningún timerText aquí
        // El texto lo actualiza DailyMissionManager.RefreshTimerText()
    }

    private DateTime CalculateNextReset()
    {
        DateTime now = DateTime.Now;
        int hour = useCustomResetTime ? resetHour : 0;
        int minute = useCustomResetTime ? resetMinute : 0;

        DateTime reset = now.Date.AddHours(hour).AddMinutes(minute);
        if (now > reset) reset = reset.AddDays(1);

        return reset;
    }

    public string GetRemainingTimeString()
    {
        TimeSpan remaining = NextResetTime - DateTime.Now;
        if (remaining.TotalSeconds < 0) remaining = TimeSpan.Zero;
        return $"{remaining:hh\\:mm\\:ss}";
    }
}
