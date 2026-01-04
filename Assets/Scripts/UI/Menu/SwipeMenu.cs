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
    [SerializeField] private Button page1ButtonExpanded;
    [SerializeField] private Button page1ButtonRecortado;

    [SerializeField] private Button page2ButtonExpanded;
    [SerializeField] private Button page2ButtonRecortedLeft;
    [SerializeField] private Button page2ButtonRecortedRight;

    [SerializeField] private Button page3ButtonNormal;
    [SerializeField] private Button page3ButtonExpanded;
    [SerializeField] private Button page3ButtonRecorted;

    private void Awake()
    {
        currentPage = 1;
        targetPos = swipeContainerRect.localPosition;
        dragThresold = Screen.width / 15f;

        HideAllPageButtons();
        BindAllButtonListeners();

        // Estado inicial: página 1 seleccionada
        ShowStateForSelectedPage(1);

        UpdateButtons();
        UpdateBar();
    }

    // =========================
    // LISTENERS
    // =========================
    private void BindAllButtonListeners()
    {
        // Página 1 (todas las variantes llevan a seleccionar 1)
        page1ButtonNormal.onClick.AddListener(() => SelectPage(1));
        page1ButtonExpanded.onClick.AddListener(() => SelectPage(1));
        page1ButtonRecortado.onClick.AddListener(() => SelectPage(1));

        // Página 2
        page2ButtonExpanded.onClick.AddListener(() => SelectPage(2));
        page2ButtonRecortedLeft.onClick.AddListener(() => SelectPage(2));
        page2ButtonRecortedRight.onClick.AddListener(() => SelectPage(2));

        // Página 3
        page3ButtonNormal.onClick.AddListener(() => SelectPage(3));
        page3ButtonExpanded.onClick.AddListener(() => SelectPage(3));
        page3ButtonRecorted.onClick.AddListener(() => SelectPage(3));
    }

    // =========================
    // STATE (activar solo 3 botones)
    // =========================
    private void HideAllPageButtons()
    {
        page1ButtonNormal.gameObject.SetActive(false);
        page1ButtonExpanded.gameObject.SetActive(false);
        page1ButtonRecortado.gameObject.SetActive(false);

        page2ButtonExpanded.gameObject.SetActive(false);
        page2ButtonRecortedLeft.gameObject.SetActive(false);
        page2ButtonRecortedRight.gameObject.SetActive(false);

        page3ButtonNormal.gameObject.SetActive(false);
        page3ButtonExpanded.gameObject.SetActive(false);
        page3ButtonRecorted.gameObject.SetActive(false);
    }

    /// <summary>
    /// Activa exactamente 3 botones (uno por página), según cuál esté seleccionada.
    /// Esto evita solapes/taps raros y asegura que siempre hay listener.
    /// </summary>
    private void ShowStateForSelectedPage(int selectedPage)
    {
        HideAllPageButtons();

        switch (selectedPage)
        {
            case 1:
                // 1 seleccionado: 1 expanded, 2 recorte left, 3 normal
                page1ButtonExpanded.gameObject.SetActive(true);
                page2ButtonRecortedLeft.gameObject.SetActive(true);
                page3ButtonNormal.gameObject.SetActive(true);
                break;

            case 2:
                // 2 seleccionado: 1 recortado, 2 expanded, 3 recortado
                page1ButtonRecortado.gameObject.SetActive(true);
                page2ButtonExpanded.gameObject.SetActive(true);
                page3ButtonRecorted.gameObject.SetActive(true);
                break;

            case 3:
                // 3 seleccionado: 1 normal, 2 recorte right, 3 expanded
                page1ButtonNormal.gameObject.SetActive(true);
                page2ButtonRecortedRight.gameObject.SetActive(true);
                page3ButtonExpanded.gameObject.SetActive(true);
                break;
        }
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
        ShowStateForSelectedPage(currentPage);
        UpdateButtons();
        UpdateBar();
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
}
