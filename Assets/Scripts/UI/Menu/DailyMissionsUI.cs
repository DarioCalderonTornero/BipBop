using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DailyMissionsUI : MonoBehaviour
{
    [SerializeField] private Image dailyMissionsPanel;
    [SerializeField] private TextMeshProUGUI timerDailyMissionsText;
    [SerializeField] private Button dailyMissionsButton;
    [SerializeField] private Button closeDailyMissionButton;

    private void Awake()
    {
        dailyMissionsButton.onClick.AddListener(() =>
        {
            Show();
        });

        closeDailyMissionButton.onClick.AddListener(() =>
        {
            Hide();
        });
    }

    private void Start()
    {
        Hide();
    }

    private void Hide()
    {
        dailyMissionsPanel.gameObject.SetActive(false);
        timerDailyMissionsText.gameObject.SetActive(false);
        closeDailyMissionButton.gameObject.SetActive(false);    
    }

    private void Show()
    {
        dailyMissionsPanel.gameObject.SetActive(true);
        timerDailyMissionsText.gameObject.SetActive(true);
        closeDailyMissionButton.gameObject.SetActive(true);
    }
}
