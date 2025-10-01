using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DailyMissionsUI : MonoBehaviour
{
    [SerializeField] private Image dailyMissionsPanel;
    [SerializeField] private TextMeshProUGUI timerDailyMissionsText;
    [SerializeField] private Button dailyMissionsButton;
    [SerializeField] private Button closeDailyMissionButton;

    private bool isVisible = false;

    private void Awake()
    {
        dailyMissionsButton.onClick.AddListener(() =>
        {
            Show();
        });

        /* closeDailyMissionButton.onClick.AddListener(() =>
        {
            Hide();
        }); */
    }

    private void Start()
    {
        Hide();
    }

    private void Hide()
    {
        isVisible = false;
        dailyMissionsPanel.gameObject.SetActive(false);
        timerDailyMissionsText.gameObject.SetActive(false);
        closeDailyMissionButton.gameObject.SetActive(false);
    }

    private void Show()
    {
        isVisible = true;
        dailyMissionsPanel.gameObject.SetActive(true);
        timerDailyMissionsText.gameObject.SetActive(true);
        closeDailyMissionButton.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (isVisible && Input.GetMouseButtonDown(0))
        {
            // Detecta si el click fue sobre la UI
            if (!IsPointerOverUIObject(dailyMissionsPanel.gameObject))
            {
                Hide();
            }
        }
    }

    private bool IsPointerOverUIObject(GameObject panel)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (result.gameObject == panel || result.gameObject.transform.IsChildOf(panel.transform))
            {
                return true; // Click dentro del panel
            }
        }

        return false; // Click fuera del panel
    }
}
