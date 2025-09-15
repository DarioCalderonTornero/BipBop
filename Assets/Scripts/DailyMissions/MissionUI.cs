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

    private DailyMission mission;

    public void Setup(DailyMission mission, Sprite missionIcon)
    {
        if (mission == null)
        {
            Debug.LogError(" DailyMission null detectada en Setup()");
            return;
        }

        this.mission = mission;
        descriptionText.text = mission.template.description;
        iconImage.sprite = missionIcon != null ? missionIcon : mission.template.icon;
        Refresh();
    }

    public void Refresh()
    {
        if (mission == null) return;
        progressText.text = $"{mission.currentProgress}/{mission.template.goal}";
    }
}
