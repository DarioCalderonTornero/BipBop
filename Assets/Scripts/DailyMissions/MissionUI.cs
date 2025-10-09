using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image xpIconImage;
    [SerializeField] private TextMeshProUGUI xpRewardText;

    [Header("Estilo de misión completada")]
    [SerializeField] private Sprite completedIcon;              // Asigna un icono distinto en inspector
    [SerializeField] private Color completedTextColor = Color.gray;

    private DailyMission mission;
    private Color defaultTextColor;

    public void Setup(DailyMission mission, Sprite missionIcon)
    {
        xpRewardText.text = mission.template.xpReward.ToString();

        if (mission == null)
        {
            Debug.LogError("DailyMission null detectada en Setup()");
            return;
        }

        this.mission = mission;

        if (mission.template == null)
        {
            Debug.LogError($"MissionUI.Setup: El template es null en la misión con progreso {mission.currentProgress}");
            return;
        }

        // Guardamos color original para poder restaurarlo si hace falta
        defaultTextColor = descriptionText.color;

        descriptionText.text = mission.template.description;
        iconImage.sprite = missionIcon != null ? missionIcon : mission.template.icon;

        Refresh();
    }

    public void Refresh()
    {
        if (mission == null) return;

        progressText.text = $"{mission.currentProgress}/{mission.template.goal}";

        if (mission.IsCompleted)
        {
            // Estilo completado
            descriptionText.color = completedTextColor;
            descriptionText.fontStyle = FontStyles.Italic;
            if (completedIcon != null)
                iconImage.sprite = completedIcon;
        }
        else
        {
            // Estilo normal
            descriptionText.color = defaultTextColor;
            descriptionText.fontStyle = FontStyles.Normal;
            iconImage.sprite = mission.template.icon;
        }
    }
}
