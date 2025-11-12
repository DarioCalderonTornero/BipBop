using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DailyMissionsUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RectTransform dailyMissionsPanel;
    [SerializeField] private RectTransform titleTextRect; // Texto que también se mueve
    [SerializeField] private TextMeshProUGUI timerDailyMissionsText;
    [SerializeField] private Button dailyMissionsButton;

    [Header("Animación")]
    [SerializeField] private float slideDuration = 0.4f;
    [SerializeField] private float hiddenXOffset = 600f;

    private bool isVisible = false;
    private bool isAnimating = false; // Nueva bandera para bloquear el botón
    private Vector2 shownPanelPos, hiddenPanelPos;
    private Vector2 shownTextPos, hiddenTextPos;
    private Coroutine slideCoroutine;

    private void Awake()
    {
        dailyMissionsButton.onClick.AddListener(OnDailyMissionsButtonClicked);
    }

    private void Start()
    {
        shownPanelPos = dailyMissionsPanel.anchoredPosition;
        hiddenPanelPos = shownPanelPos + new Vector2(hiddenXOffset, 0f);

        if (titleTextRect != null)
        {
            shownTextPos = titleTextRect.anchoredPosition;
            hiddenTextPos = shownTextPos + new Vector2(hiddenXOffset, 0f);
            titleTextRect.anchoredPosition = hiddenTextPos;
        }

        dailyMissionsPanel.anchoredPosition = hiddenPanelPos;
        timerDailyMissionsText.gameObject.SetActive(false);
    }

    private void OnDailyMissionsButtonClicked()
    {
        // Si está animando, no permite pulsar
        if (isAnimating)
            return;

        if (isVisible)
            Hide();
        else
            Show();
    }

    private void Show()
    {
        isVisible = true;
        timerDailyMissionsText.gameObject.SetActive(true);

        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlideBoth(
            dailyMissionsPanel, titleTextRect,
            dailyMissionsPanel.anchoredPosition, shownPanelPos,
            titleTextRect?.anchoredPosition ?? Vector2.zero, shownTextPos,
            () => { isAnimating = false; } // Rehabilitar al terminar
        ));
    }

    private void Hide()
    {
        isVisible = false;

        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlideBoth(
            dailyMissionsPanel, titleTextRect,
            dailyMissionsPanel.anchoredPosition, hiddenPanelPos,
            titleTextRect?.anchoredPosition ?? Vector2.zero, hiddenTextPos,
            () =>
            {
                timerDailyMissionsText.gameObject.SetActive(false);
                isAnimating = false; // Rehabilitar botón
            }
        ));
    }

    private IEnumerator SlideBoth(
        RectTransform panel, RectTransform text,
        Vector2 fromPanel, Vector2 toPanel,
        Vector2 fromText, Vector2 toText,
        System.Action onComplete = null)
    {
        isAnimating = true; // Bloquea el botón al iniciar animación

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);

            panel.anchoredPosition = Vector2.Lerp(fromPanel, toPanel, t);
            if (text != null)
                text.anchoredPosition = Vector2.Lerp(fromText, toText, t);

            yield return null;
        }

        panel.anchoredPosition = toPanel;
        if (text != null) text.anchoredPosition = toText;

        onComplete?.Invoke();
    }

    private void Update()
    {
        if (isVisible && !isAnimating && Input.GetMouseButtonDown(0))
        {
            GameObject clickedUI = GetClickedUIObject();

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
