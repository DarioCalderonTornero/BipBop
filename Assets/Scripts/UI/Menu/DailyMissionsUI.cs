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

    private bool isVisible = false;

    private void Awake()
    {
        dailyMissionsButton.onClick.AddListener(OnDailyMissionsButtonClicked);
    }

    private void Start()
    {
        Hide();
    }

    private void OnDailyMissionsButtonClicked()
    {
        // Si está abierto, se cierra; si está cerrado, se abre.
        if (isVisible)
            Hide();
        else
            Show();
    }

    private void Hide()
    {
        isVisible = false;
        dailyMissionsPanel.gameObject.SetActive(false);
        timerDailyMissionsText.gameObject.SetActive(false);
    }

    private void Show()
    {
        isVisible = true;
        dailyMissionsPanel.gameObject.SetActive(true);
        timerDailyMissionsText.gameObject.SetActive(true);
    }

    private void Update()
    {
        // Si el panel está visible y se toca cualquier parte de la pantalla
        if (isVisible && Input.GetMouseButtonDown(0))
        {
            GameObject clickedUI = GetClickedUIObject();

            // Si se ha hecho clic en cualquier parte (botón o fuera del panel)
            if (clickedUI == null ||
                clickedUI == dailyMissionsButton.gameObject ||
                !clickedUI.transform.IsChildOf(dailyMissionsPanel.transform))
            {
                Hide();
            }
        }
    }

    private GameObject GetClickedUIObject()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        return results.Count > 0 ? results[0].gameObject : null;
    }
}
