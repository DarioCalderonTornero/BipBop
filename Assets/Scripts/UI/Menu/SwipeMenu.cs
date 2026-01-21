using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SwipeMenu : MonoBehaviour, IEndDragHandler
{
    [SerializeField] private int maxPage;

    private int currentPage;
    private Vector3 targetPos;

    [SerializeField] private Vector3 pageStep;
    [SerializeField] private RectTransform swipeContainerRect;

    [SerializeField] private float dragThresold;
    [SerializeField] private float tweenTime;

    [SerializeField] private LeanTweenType tweenType;

    [SerializeField] private Image[] barImage;
    [SerializeField] private Sprite barClosed, barOpen;

    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    [Header("Buttons (Variants)")]
    [SerializeField] private Button page1ButtonNormal;
    [SerializeField] private Button page2ButtonExpanded;
    [SerializeField] private Button page3ButtonNormal;

    [Header("Page Buttons - Visual")]
    [SerializeField] private Color selectedColor = new Color(1f, 0.7f, 0.2f, 1f); // el que quieras
    [SerializeField] private Color unselectedColor = Color.white;

    [Header("Pop")]
    [SerializeField] private float popScale = 1.08f;
    [SerializeField] private float popUpTime = 0.06f;
    [SerializeField] private float popDownTime = 0.08f;
    [SerializeField] private LeanTweenType popEaseUp = LeanTweenType.easeOutBack;
    [SerializeField] private LeanTweenType popEaseDown = LeanTweenType.easeOutQuad;

    private Vector3 page1BaseScale;
    private Vector3 page2BaseScale;
    private Vector3 page3BaseScale;

    private void Awake()
    {
        currentPage = 1;
        targetPos = swipeContainerRect.localPosition;
        dragThresold = Screen.width / 15f;

        CacheBaseScales();
        BindAllButtonListeners();

        UpdateButtons();
        UpdateBar();
        UpdatePageButtonsVisuals(); // <-- importante
    }

    private void CacheBaseScales()
    {
        if (page1ButtonNormal) page1BaseScale = page1ButtonNormal.transform.localScale;
        if (page2ButtonExpanded) page2BaseScale = page2ButtonExpanded.transform.localScale;
        if (page3ButtonNormal) page3BaseScale = page3ButtonNormal.transform.localScale;
    }

    private void BindAllButtonListeners()
    {
        if (page1ButtonNormal)
            page1ButtonNormal.onClick.AddListener(() => OnPageButtonPressed(1, page1ButtonNormal.transform, page1BaseScale));

        if (page2ButtonExpanded)
            page2ButtonExpanded.onClick.AddListener(() => OnPageButtonPressed(2, page2ButtonExpanded.transform, page2BaseScale));

        if (page3ButtonNormal)
            page3ButtonNormal.onClick.AddListener(() => OnPageButtonPressed(3, page3ButtonNormal.transform, page3BaseScale));
    }

    private void OnPageButtonPressed(int page, Transform buttonTf, Vector3 baseScale)
    {
        Pop(buttonTf, baseScale);
        SelectPage(page);
    }

    private void Pop(Transform t, Vector3 baseScale)
    {
        if (t == null) return;

        // Cancela tweens previos para que no se acumulen
        LeanTween.cancel(t.gameObject);

        t.localScale = baseScale;

        Vector3 up = baseScale * popScale;
        LeanTween.scale(t.gameObject, up, popUpTime).setEase(popEaseUp).setOnComplete(() =>
        {
            LeanTween.scale(t.gameObject, baseScale, popDownTime).setEase(popEaseDown);
        });
    }

    // =========================
    // PAGE SELECT / MOVE
    // =========================
    private void SelectPage(int page)
    {
        page = Mathf.Clamp(page, 1, maxPage);

        if (currentPage == page)
            return;

        int delta = page - currentPage;

        currentPage = page;
        targetPos += pageStep * delta;

        MovePage();
        UpdateButtons();
        UpdateBar();
        UpdatePageButtonsVisuals(); // <-- importante
    }

    public void Next()
    {
        if (currentPage < maxPage)
            SelectPage(currentPage + 1);
    }

    public void Previous()
    {
        if (currentPage > 1)
            SelectPage(currentPage - 1);
    }

    private void MovePage()
    {
        swipeContainerRect.LeanMoveLocal(targetPos, tweenTime).setEase(tweenType);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (Mathf.Abs(eventData.position.x - eventData.pressPosition.x) > dragThresold)
        {
            if (eventData.position.x > eventData.pressPosition.x) Previous();
            else Next();
        }
        else
        {
            MovePage();
        }
    }

    // =========================
    // UI
    // =========================
    private void UpdateBar()
    {
        if (barImage == null || barImage.Length == 0) return;
        if (barClosed == null || barOpen == null) return;

        for (int i = 0; i < barImage.Length; i++)
        {
            if (barImage[i] != null)
                barImage[i].sprite = barClosed;
        }

        int idx = currentPage - 1;
        if (idx >= 0 && idx < barImage.Length && barImage[idx] != null)
            barImage[idx].sprite = barOpen;
    }

    private void UpdateButtons()
    {
        if (previousButton != null)
            previousButton.interactable = currentPage != 1;

        if (nextButton != null)
            nextButton.interactable = currentPage != maxPage;
    }

    private void UpdatePageButtonsVisuals()
    {
        SetButtonColor(page1ButtonNormal, currentPage == 1 ? selectedColor : unselectedColor);
        SetButtonColor(page2ButtonExpanded, currentPage == 2 ? selectedColor : unselectedColor);
        SetButtonColor(page3ButtonNormal, currentPage == 3 ? selectedColor : unselectedColor);
    }

    private void SetButtonColor(Button btn, Color c)
    {
        if (btn == null) return;

        // Ideal: cambia el Image del propio botón
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = c;
    }
}
