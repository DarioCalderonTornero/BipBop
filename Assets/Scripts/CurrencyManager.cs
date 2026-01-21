using System;
using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    public event Action<int> OnCoinsChanged;

    [SerializeField] private int coins = 0;

    private const string CoinsKey = "CoinCount";          // PlayerPrefs key
    private const string PF_COINS_KEY = "CoinCount";      // PlayFab UserData key

    private Coroutine saveDebounce;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 1) Carga local inmediata (para no ver 0 mientras loguea)
            coins = PlayerPrefs.GetInt(CoinsKey, 0);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        if (PlayFabLoginManager.Instance != null)
            PlayFabLoginManager.Instance.OnLoginSuccess += HandleLoginSuccess;
    }

    private void OnDisable()
    {
        if (PlayFabLoginManager.Instance != null)
            PlayFabLoginManager.Instance.OnLoginSuccess -= HandleLoginSuccess;
    }

    private void Start()
    {
        // Notifica en Start para que la UI que se subscribe en Awake/Start no se lo pierda
        OnCoinsChanged?.Invoke(coins);

        // Si ya estaba logueado (por orden de escena), carga remoto ya
        if (PlayFabLoginManager.Instance != null && PlayFabLoginManager.Instance.IsLoggedIn)
            LoadCoinsFromPlayFab();
    }

    private void HandleLoginSuccess()
    {
        LoadCoinsFromPlayFab();
    }

    // =====================
    //  API pública
    // =====================

    public int GetCoins() => coins;

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        coins += amount;
        SaveLocal();
        Notify();

        SaveRemoteDebounced();
    }

    public void SpendCoins(int amount)
    {
        if (amount <= 0) return;

        coins = Mathf.Max(0, coins - amount);
        SaveLocal();
        Notify();

        SaveRemoteDebounced();
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0) return true;
        if (coins < amount) return false;

        coins -= amount;
        SaveLocal();
        Notify();

        SaveRemoteDebounced();
        return true;
    }

    // =====================
    //  Local
    // =====================

    private void SaveLocal()
    {
        PlayerPrefs.SetInt(CoinsKey, coins);
        PlayerPrefs.Save();
    }

    private void Notify()
    {
        OnCoinsChanged?.Invoke(coins);
    }

    private void OnApplicationPause(bool pause)
    {
        if (!pause) return;

        SaveLocal();
        // flush remoto al pausar (opcional, pero útil en móvil)
        SaveCoinsToPlayFab();
    }

    private void OnApplicationQuit()
    {
        SaveLocal();
        SaveCoinsToPlayFab();
    }

    // =====================
    //  PlayFab (UserData)
    // =====================

    private void LoadCoinsFromPlayFab()
    {
        if (PlayFabLoginManager.Instance == null || !PlayFabLoginManager.Instance.IsLoggedIn)
            return;

        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
            result =>
            {
                var data = result.Data;

                // Si no existe en PlayFab (cuenta nueva), subimos lo local como inicial
                if (data == null || !data.ContainsKey(PF_COINS_KEY))
                {
                    Debug.Log("No hay monedas en PlayFab -> subiendo las locales como iniciales.");
                    SaveCoinsToPlayFab();
                    return;
                }

                // Parse seguro
                if (data.TryGetValue(PF_COINS_KEY, out var entry) && int.TryParse(entry.Value, out var serverCoins))
                {
                    coins = Mathf.Max(0, serverCoins);
                    SaveLocal();  // PlayFab manda -> reflejamos en local
                    Notify();

                    Debug.Log($"Monedas cargadas desde PlayFab: {coins}");
                }
                else
                {
                    Debug.LogWarning("CoinCount en PlayFab existe pero no se pudo parsear. Mantengo local.");
                }
            },
            error => Debug.LogWarning("Error al cargar monedas: " + error.GenerateErrorReport()));
    }

    private void SaveRemoteDebounced()
    {
        if (saveDebounce != null) StopCoroutine(saveDebounce);
        saveDebounce = StartCoroutine(SaveRemoteDebounceRoutine());
    }

    private IEnumerator SaveRemoteDebounceRoutine()
    {
        yield return new WaitForSeconds(1.0f);
        SaveCoinsToPlayFab();
        saveDebounce = null;
    }

    private void SaveCoinsToPlayFab()
    {
        if (PlayFabLoginManager.Instance == null || !PlayFabLoginManager.Instance.IsLoggedIn)
            return;

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { PF_COINS_KEY, coins.ToString() }
            },
            Permission = UserDataPermission.Private
        };

        PlayFabClientAPI.UpdateUserData(request,
            _ => Debug.Log("Monedas guardadas en PlayFab (privadas)."),
            error => Debug.LogWarning("Error al guardar monedas: " + error.GenerateErrorReport()));
    }
}
