using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class MissionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image xpIconImage;
    [SerializeField] private TextMeshProUGUI xpRewardText;

    [Header("Estilo de misión completada")]
    [SerializeField] private Sprite completedIcon;
    [SerializeField] private Color completedTextColor = Color.gray;

    private DailyMission mission;
    private Color defaultTextColor;
    private bool initialized = false;

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    public void Setup(DailyMission mission, Sprite missionIcon)
    {
        if (mission == null)
        {
            Debug.LogError("DailyMission null detectada en Setup()");
            return;
        }

        if (mission.template == null)
        {
            Debug.LogError($"MissionUI.Setup: El template es null en la misión con progreso {mission.currentProgress}");
            return;
        }

        this.mission = mission;

        xpRewardText.text = mission.template.xpReward.ToString();

        // Guardamos color original
        defaultTextColor = descriptionText.color;

        // Icono
        iconImage.sprite = missionIcon != null ? missionIcon : mission.template.icon;

        // Texto localizado
        UpdateDescriptionText();

        Refresh();
        initialized = true;
    }

    private void UpdateDescriptionText()
    {
        if (mission == null || mission.template == null) return;

        // Si la descripción es Smart String tipo "Juega {0} partidas...", pasamos goal
        descriptionText.text = mission.template.description.GetLocalizedString(mission.template.goal);
    }

    public void Refresh()
    {
        if (mission == null || mission.template == null) return;

        // Progreso numérico (esto normalmente no hace falta localizarlo)
        progressText.text = $"{mission.currentProgress}/{mission.template.goal}";

        if (mission.IsCompleted)
        {
            descriptionText.color = completedTextColor;
            descriptionText.fontStyle = FontStyles.Italic;
            if (completedIcon != null)
                iconImage.sprite = completedIcon;
        }
        else
        {
            descriptionText.color = defaultTextColor;
            descriptionText.fontStyle = FontStyles.Normal;
            iconImage.sprite = mission.template.icon;
        }
    }

    private void OnLocaleChanged(Locale newLocale)
    {
        if (!initialized || mission == null) return;

        // Re-localizar descripción al cambiar de idioma
        UpdateDescriptionText();
        // El estilo (completado o no) se mantiene igual
        Refresh();
    }
}
