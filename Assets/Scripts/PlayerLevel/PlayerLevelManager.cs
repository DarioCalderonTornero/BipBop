using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLevelManager : MonoBehaviour
{
    public static PlayerLevelManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI xpText;
    public Image xpFillImage;

    [Header("Level Settings")]
    public int currentLevel = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 1000;
    public float xpGrowthMultiplier = 1.2f;

    private const string PREF_LEVEL = "PlayerLevel";
    private const string PREF_XP = "PlayerXP";
    private const string PREF_XP_NEXT = "PlayerXPNext";

    // Flags
    private bool loadedFromPlayFab = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
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
        // 1) Siempre primero local (para que no se vea 0 mientras carga)
        LoadLocalData();
        UpdateUI();

        // 2) Si ya está logueado, cargamos PlayFab ya.
        // Si no, lo hará HandleLoginSuccess cuando termine el login.
        if (PlayFabLoginManager.Instance != null && PlayFabLoginManager.Instance.IsLoggedIn)
            LoadLevelFromPlayFab();
    }

    private void HandleLoginSuccess()
    {
        // Login completado -> ahora sí podemos leer PlayFab
        LoadLevelFromPlayFab();
    }

    // Añadir XP (por ejemplo, al terminar partida)
    public void AddXP(int amount)
    {
        if (amount <= 0) return;

        currentXP += amount;

        bool leveledUp = false;
        while (currentXP >= xpToNextLevel)
        {
            LevelUp();
            leveledUp = true;
        }

        SaveLocalData();

        // Guardamos remoto solo si estamos logueados
        if (PlayFabLoginManager.Instance != null && PlayFabLoginManager.Instance.IsLoggedIn)
        {
            SaveLevelToPlayFab();
            if (leveledUp) UpdateLevelStatistic(); // evita spamear ranking por cada +1 XP
        }

        UpdateUI();
    }

    private void LevelUp()
    {
        currentXP -= xpToNextLevel;
        currentLevel++;
        xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * xpGrowthMultiplier);
        Debug.Log($"Nivel {currentLevel} alcanzado!");
    }

    private void UpdateUI()
    {
        if (levelText) levelText.text = currentLevel.ToString();
        if (xpText) xpText.text = $"{currentXP} / {xpToNextLevel}";
        if (xpFillImage) xpFillImage.fillAmount = (float)currentXP / Mathf.Max(1, xpToNextLevel);
    }

    #region Local Save / Load
    private void SaveLocalData()
    {
        PlayerPrefs.SetInt(PREF_LEVEL, currentLevel);
        PlayerPrefs.SetInt(PREF_XP, currentXP);
        PlayerPrefs.SetInt(PREF_XP_NEXT, xpToNextLevel);
        PlayerPrefs.Save();
    }

    private void LoadLocalData()
    {
        currentLevel = PlayerPrefs.GetInt(PREF_LEVEL, 1);
        currentXP = PlayerPrefs.GetInt(PREF_XP, 0);
        xpToNextLevel = PlayerPrefs.GetInt(PREF_XP_NEXT, 1000);
    }
    #endregion

    #region PlayFab Save / Load
    public void SaveLevelToPlayFab()
    {
        if (PlayFabLoginManager.Instance == null || !PlayFabLoginManager.Instance.IsLoggedIn)
            return;

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "PlayerLevel", currentLevel.ToString() },
                { "PlayerXP", currentXP.ToString() },
                { "PlayerXPNext", xpToNextLevel.ToString() }
            },
            Permission = UserDataPermission.Private // mejor que Public para progreso
        };

        PlayFabClientAPI.UpdateUserData(request,
            _ => Debug.Log("Nivel y XP guardados en PlayFab (privados)."),
            error => Debug.LogWarning("Error al guardar nivel/XP: " + error.GenerateErrorReport()));
    }

    public void LoadLevelFromPlayFab()
    {
        if (PlayFabLoginManager.Instance == null || !PlayFabLoginManager.Instance.IsLoggedIn)
            return;

        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
            result =>
            {
                var data = result.Data;

                // Si no hay datos en PlayFab, creamos los datos iniciales a partir de lo local
                if (data == null || !data.ContainsKey("PlayerLevel"))
                {
                    Debug.Log("No hay datos de nivel en PlayFab -> subiendo los locales como iniciales.");
                    SaveLevelToPlayFab();
                    UpdateUI();
                    return;
                }

                // Parseo seguro
                if (data.TryGetValue("PlayerLevel", out var lv)) int.TryParse(lv.Value, out currentLevel);
                if (data.TryGetValue("PlayerXP", out var xp)) int.TryParse(xp.Value, out currentXP);
                if (data.TryGetValue("PlayerXPNext", out var nx)) int.TryParse(nx.Value, out xpToNextLevel);

                loadedFromPlayFab = true;

                Debug.Log($"Nivel cargado desde PlayFab: Nivel {currentLevel}, XP {currentXP}/{xpToNextLevel}");

                // PlayFab manda -> actualizamos local
                SaveLocalData();
                UpdateUI();

                // (opcional) asegurar statistic al entrar
                UpdateLevelStatistic();
            },
            error => Debug.LogWarning("Error al cargar nivel: " + error.GenerateErrorReport()));
    }
    #endregion

    #region PlayFab Statistic (ranking)
    public void UpdateLevelStatistic()
    {
        if (PlayFabLoginManager.Instance == null || !PlayFabLoginManager.Instance.IsLoggedIn)
            return;

        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate
                {
                    StatisticName = "PlayerLevel",
                    Value = currentLevel
                }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request,
            _ => Debug.Log("Estadística de nivel actualizada en PlayFab."),
            error => Debug.LogWarning("Error al actualizar estadística: " + error.GenerateErrorReport()));
    }
    #endregion
}
