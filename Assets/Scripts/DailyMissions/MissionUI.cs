using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class MissionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("XP Reward")]
    [SerializeField] private Image xpIconImage;
    [SerializeField] private TextMeshProUGUI xpRewardText;

    [Header("Coin Reward")]
    [SerializeField] private Image coinIconImage;
    [SerializeField] private TextMeshProUGUI coinRewardText;

    [Header("Icono de misión")]
    [SerializeField] private Image missionIconImage;

    [Header("Completed Style")]
    [SerializeField] private Color completedColor = Color.gray;

    private DailyMission mission;
    private Color defaultColor;
    private bool initialized;

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    public void Setup(DailyMission mission)
    {
        this.mission = mission;

        defaultColor = descriptionText.color;

        if (missionIconImage != null)
        {
            if (mission.template.missionIcon != null)
            {
                missionIconImage.sprite = mission.template.missionIcon;
                missionIconImage.gameObject.SetActive(true);
            }
            else
            {
                missionIconImage.gameObject.SetActive(false);
            }
        }

        // --- XP ---
        xpRewardText.text = "+" + mission.template.xpReward;
        xpIconImage.sprite = mission.template.xpIcon;

        // --- Monedas ---
        coinRewardText.text = "+" + mission.template.coinReward;
        coinIconImage.sprite = mission.template.coinIcon;

        UpdateDescriptionText();
        Refresh();

        initialized = true;
    }

    private void UpdateDescriptionText()
    {
        descriptionText.text =
            mission.template.description.GetLocalizedString(mission.template.goal);
    }

    public void Refresh()
    {
        progressText.text = $"{mission.currentProgress}/{mission.template.goal}";

        if (mission.IsCompleted)
        {
            SetCompletedColors();
        }
        else
        {
            SetNormalColors();
        }
    }

    private void SetCompletedColors()
    {
        descriptionText.color = completedColor;
        progressText.color = completedColor;

        xpIconImage.color = completedColor;
        coinIconImage.color = completedColor;

        xpRewardText.color = completedColor;
        coinRewardText.color = completedColor;
    }

    private void SetNormalColors()
    {
        descriptionText.color = defaultColor;
        progressText.color = defaultColor;

        xpIconImage.color = Color.white;
        coinIconImage.color = Color.white;

        xpRewardText.color = Color.black;
        coinRewardText.color = Color.black;
    }

    private void OnLocaleChanged(Locale locale)
    {
        if (!initialized) return;

        UpdateDescriptionText();
        Refresh();
    }
}
