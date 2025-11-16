using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

public class DailyMissionManager : MonoBehaviour
{
    public static DailyMissionManager Instance;

    [Header("Config")]
    [SerializeField] private List<MissionTemplate> missionTemplates;
    [SerializeField] private int missionsPerDay = 3;

    [Header("UI")]
    [SerializeField] private string missionsContainerName = "MissionsContainer";
    [SerializeField] private MissionUI missionUIPrefab;
    [SerializeField] private string timerTextName = "DailyMissionsTimerText";

    private List<DailyMission> activeMissions = new List<DailyMission>();
    private List<MissionUI> missionUIList = new List<MissionUI>();

    private Transform missionsContainer;
    private TextMeshProUGUI timerText;

    [Header("Localization")]
    [SerializeField] private LocalizedString dailyMissionsTimerLabel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadMissions();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale newLocale)
    {
        RefreshTimerText();
        // Si quisieras, también podrías refrescar la UI de misiones aquí
        RefreshUI();
    }

    public void RegisterTimerText(TextMeshProUGUI text)
    {
        timerText = text;
        RefreshTimerText();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Ya NO hace falta buscar el timer por nombre
        GameObject containerObj = GameObject.Find(missionsContainerName);
        if (containerObj != null)
            missionsContainer = containerObj.transform;

        GenerateAndDisplayMissions();
        RefreshTimerText();
    }

    public void AddProgress(string missionId, int amount = 1)
    {
        DailyMission mission = activeMissions.Find(m => m.template.id == missionId && !m.IsCompleted);
        if (mission != null)
        {
            mission.AddProgress(amount);
            RefreshUI();

            if (mission.IsCompleted && !mission.rewardClaimed)
            {
                mission.rewardClaimed = true;

                // Monedas
                CurrencyManager.Instance.AddCoins(mission.template.reward);

                //  Añadir experiencia
                PlayerLevelManager.Instance.AddXP(mission.template.xpReward);

                Debug.Log($"Misión completada: {mission.template.description}. " +
                          $"Recompensa: {mission.template.reward} monedas y {mission.template.xpReward} XP");
            }

            SaveMissions();
        }
    }

    private void RefreshUI()
    {
        if (missionUIList.Count == 0)
        {
            GenerateAndDisplayMissions();
            return;
        }

        foreach (var ui in missionUIList)
            ui.Refresh();
    }

    private void GenerateAndDisplayMissions()
    {
        if (missionsContainer == null) return;

        foreach (Transform child in missionsContainer)
            Destroy(child.gameObject);

        missionUIList.Clear();

        if (activeMissions.Count == 0)
        {
            // Generar nuevas misiones
            List<MissionTemplate> pool = new List<MissionTemplate>();
            foreach (var t in missionTemplates)
                if (t != null) pool.Add(t);

            for (int i = 0; i < missionsPerDay && pool.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                DailyMission newMission = new DailyMission(pool[index]);
                activeMissions.Add(newMission);
                pool.RemoveAt(index);
            }

            SaveMissions();
        }

        // Crear UI
        foreach (var mission in activeMissions)
        {
            MissionUI ui = Instantiate(missionUIPrefab, missionsContainer);
            ui.Setup(mission, null);
            missionUIList.Add(ui);
        }
    }

    #region Guardado
    private void SaveMissions()
    {
        string json = JsonUtility.ToJson(new DailyMissionsSaveData(activeMissions));
        PlayerPrefs.SetString("DailyMissionsData", json);
        PlayerPrefs.Save();
    }

    private void LoadMissions()
    {
        if (PlayerPrefs.HasKey("DailyMissionsData"))
        {
            string json = PlayerPrefs.GetString("DailyMissionsData");
            DailyMissionsSaveData data = JsonUtility.FromJson<DailyMissionsSaveData>(json);

            if (data != null && data.activeMissions != null)
            {
                activeMissions.Clear();
                foreach (var save in data.activeMissions)
                {
                    MissionTemplate template = missionTemplates.Find(t => t.id == save.templateId);
                    if (template != null)
                    {
                        DailyMission mission = new DailyMission(template);
                        mission.currentProgress = save.currentProgress;
                        mission.rewardClaimed = save.rewardClaimed;
                        activeMissions.Add(mission);
                    }
                    else
                    {
                        Debug.LogWarning($"No se encontró template con id {save.templateId}, misión descartada.");
                    }
                }
            }
        }
    }
    #endregion

    #region Timer
    public void RefreshTimerText()
    {
        if (timerText != null && DailyMissionsTimer.Instance != null)
        {
            string remaining = DailyMissionsTimer.Instance.GetRemainingTimeString();
            string localized = dailyMissionsTimerLabel.GetLocalizedString(remaining);

            Debug.Log($"[DailyMissionManager] Timer -> '{localized}'");

            timerText.text = localized;
        }
    }

    public void ResetMissions()
    {
        activeMissions.Clear();
        missionUIList.Clear();
        GenerateAndDisplayMissions();
        SaveMissions();
    }
    #endregion
}

[Serializable]
public class DailyMissionSaveData
{
    public string templateId;
    public int currentProgress;
    public bool rewardClaimed;

    public DailyMissionSaveData(DailyMission mission)
    {
        templateId = mission.template != null ? mission.template.id : "";
        currentProgress = mission.currentProgress;
        rewardClaimed = mission.rewardClaimed;
    }
}

[Serializable]
public class DailyMissionsSaveData
{
    public List<DailyMissionSaveData> activeMissions;

    public DailyMissionsSaveData(List<DailyMission> missions)
    {
        activeMissions = new List<DailyMissionSaveData>();
        foreach (var m in missions)
            activeMissions.Add(new DailyMissionSaveData(m));
    }
}
