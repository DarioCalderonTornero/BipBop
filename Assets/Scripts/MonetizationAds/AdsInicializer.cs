using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.LevelPlay;
using System;

public class AdsInicializer : MonoBehaviour
{
    [SerializeField] private string _androidAppKey = "5974898";
    [SerializeField] private string _iOSAppKey = "5974899";

    private string _appKey;

    public static event Action OnLevelPlayInitialized;

    // ? NUEVO: flag para saber si LevelPlay ya se inicializó
    public static bool IsInitialized { get; private set; } = false;

    void Awake()
    {
        InitializeLevelPlay();
    }

    public void InitializeLevelPlay()
    {
#if UNITY_ANDROID
        _appKey = _androidAppKey;
#elif UNITY_IOS
        _appKey = _iOSAppKey;
#endif

        LevelPlay.OnInitSuccess += OnInitializationComplete;
        LevelPlay.OnInitFailed += OnInitializationFailed;

        LevelPlay.SetMetaData("is_test_suite", "enable");

        LevelPlay.Init(_appKey);
    }

    private void OnInitializationComplete(LevelPlayConfiguration config)
    {
        Debug.Log("LevelPlay SDK initialization complete.");

        // ? Marcamos que ya está inicializado
        IsInitialized = true;

        // Avisamos a todos los scripts que esperan la inicialización
        OnLevelPlayInitialized?.Invoke();
    }

    private void OnInitializationFailed(LevelPlayInitError error)
    {
        Debug.LogError($"LevelPlay SDK Initialization Failed: {error}");
    }
}