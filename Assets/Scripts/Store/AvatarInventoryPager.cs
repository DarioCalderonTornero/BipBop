using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AvatarInventoryPager : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Header("Refs")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private int pageCount = 3;

    [Header("Snap")]
    [SerializeField] private float snapSpeed = 10f;
    [SerializeField] private float swipeThreshold = 0.08f; // cuánto tienes que arrastrar para cambiar de página

    [Header("Dots")]
    [SerializeField] private Image[] pageDots;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = Color.gray;

    private int currentPage = 0;
    private bool isSnapping = false;
    private float targetPos = 0f;
    private float dragStartPos = 0f;

    private void Awake()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();

        // Aseguramos estado inicial
        SetPage(0, instant: true);
    }

    private void Update()
    {
        if (!isSnapping || scrollRect == null) return;

        float newPos = Mathf.Lerp(scrollRect.horizontalNormalizedPosition, targetPos, Time.deltaTime * snapSpeed);
        scrollRect.horizontalNormalizedPosition = newPos;

        if (Mathf.Abs(newPos - targetPos) < 0.001f)
        {
            scrollRect.horizontalNormalizedPosition = targetPos;
            isSnapping = false;
        }
    }

    // ========================
    //  DRAG
    // ========================
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (scrollRect == null) return;
        isSnapping = false;
        dragStartPos = scrollRect.horizontalNormalizedPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (scrollRect == null || pageCount <= 1) return;

        float endPos = scrollRect.horizontalNormalizedPosition;
        float delta = endPos - dragStartPos;

        int targetPage = currentPage;

        if (Mathf.Abs(delta) > swipeThreshold)
        {
            // 🔄 CONTROL INVERTIDO
            if (delta < 0f)
                targetPage = Mathf.Max(currentPage - 1, 0);             // arrastrar a la izquierda → IR A LA IZQUIERDA
            else
                targetPage = Mathf.Min(currentPage + 1, pageCount - 1); // arrastrar a la derecha → IR A LA DERECHA
        }
        else
        {
            targetPage = Mathf.RoundToInt(endPos * (pageCount - 1));
        }

        SetPage(targetPage, instant: false);
    }

    // ========================
    //  CAMBIO DE PÁGINA
    // ========================
    private void SetPage(int pageIndex, bool instant)
    {
        pageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);
        currentPage = pageIndex;

        float normalized = (pageCount <= 1) ? 0f : (float)pageIndex / (pageCount - 1);

        if (scrollRect != null)
        {
            if (instant)
            {
                scrollRect.horizontalNormalizedPosition = normalized;
                isSnapping = false;
            }
            else
            {
                targetPos = normalized;
                isSnapping = true;
            }
        }

        UpdateDots();
    }

    private void UpdateDots()
    {
        if (pageDots == null) return;

        for (int i = 0; i < pageDots.Length; i++)
        {
            if (pageDots[i] == null) continue;

            pageDots[i].color = (i == currentPage) ? activeColor : inactiveColor;
        }
    }

    // Por si quieres cambiar de página desde botones
    public void GoToPage(int index)
    {
        SetPage(index, instant: false);
    }
}
