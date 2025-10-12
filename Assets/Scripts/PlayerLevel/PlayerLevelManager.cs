using System.Collections;
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

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        LoadLocalData();
        LoadLevelFromPlayFab();
        UpdateUI();
    }

    // Añadir XP (por ejemplo, al terminar partida)
    public void AddXP(int amount)
    {
        currentXP += amount;
        if (currentXP >= xpToNextLevel)
        {
            LevelUp();
        }
        SaveLocalData();
        SaveLevelToPlayFab();
        UpdateLevelStatistic();
        UpdateUI();
    }

    private void LevelUp()
    {
        currentXP -= xpToNextLevel;
        currentLevel++;
        xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * xpGrowthMultiplier);
        Debug.Log($" Nivel {currentLevel} alcanzado!");
    }

    private void UpdateUI()
    {
        if (levelText) levelText.text = currentLevel.ToString();
        if (xpText) xpText.text = $"{currentXP} / {xpToNextLevel}";
        if (xpFillImage) xpFillImage.fillAmount = (float)currentXP / xpToNextLevel;
    }

    #region  Local Save / Load
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

    #region  PlayFab Save / Load
    public void SaveLevelToPlayFab()
    {
        if (!PlayFabLoginManager.Instance || !PlayFabLoginManager.Instance.IsLoggedIn)
            return;

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "PlayerLevel", currentLevel.ToString() },
                { "PlayerXP", currentXP.ToString() },
                { "PlayerXPNext", xpToNextLevel.ToString() }
            }
        };

        PlayFabClientAPI.UpdateUserData(request,
            result => Debug.Log("Nivel y XP guardados en PlayFab."),
            error => Debug.LogWarning("Error al guardar nivel/XP: " + error.GenerateErrorReport()));
    }

    public void LoadLevelFromPlayFab()
    {
        if (!PlayFabLoginManager.Instance || !PlayFabLoginManager.Instance.IsLoggedIn)
            return;

        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
            result =>
            {
                if (result.Data != null && result.Data.ContainsKey("PlayerLevel"))
                {
                    int.TryParse(result.Data["PlayerLevel"].Value, out currentLevel);
                    int.TryParse(result.Data["PlayerXP"].Value, out currentXP);
                    int.TryParse(result.Data["PlayerXPNext"].Value, out xpToNextLevel);
                    Debug.Log($"Datos de nivel cargados: Nivel {currentLevel}, XP {currentXP}/{xpToNextLevel}");
                    SaveLocalData();
                    UpdateUI();
                }
                else
                {
                    Debug.Log("No se encontraron datos de nivel en PlayFab, usando locales.");
                }
            },
            error => Debug.LogWarning("Error al cargar datos de nivel: " + error.GenerateErrorReport()));
    }
    #endregion

    #region  PlayFab Statistic (para ranking de nivel)
    public void UpdateLevelStatistic()
    {
        if (!PlayFabLoginManager.Instance || !PlayFabLoginManager.Instance.IsLoggedIn)
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
            result => Debug.Log("Estadística de nivel actualizada en PlayFab."),
            error => Debug.LogWarning("Error al actualizar estadística de nivel: " + error.GenerateErrorReport()));
    }
    #endregion
}
