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
    [SerializeField] private float swipeThreshold = 0.08f;

    [Header("Dots")]
    [SerializeField] private Image[] pageDots;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = Color.gray;

    private int currentPage = 0;
    private bool isSnapping = false;
    private bool isDragging = false;
    private float targetPos = 0f;
    private float dragStartPos = 0f;

    private void Awake()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();

        // Estado inicial: página 0
        SetPage(0, instant: true);
    }

    private void Update()
    {
        if (scrollRect == null)
            return;

        // 1) Si estamos haciendo snap, interpolamos hacia targetPos
        if (isSnapping)
        {
            float newPos = Mathf.Lerp(
                scrollRect.horizontalNormalizedPosition,
                targetPos,
                Time.deltaTime * snapSpeed
            );

            scrollRect.horizontalNormalizedPosition = newPos;

            if (Mathf.Abs(newPos - targetPos) < 0.001f)
            {
                scrollRect.horizontalNormalizedPosition = targetPos;
                isSnapping = false;
            }
        }
        // 2) Si NO hay snap activo y NO estamos arrastrando,
        //    forzamos la posición a la página más cercana (0, 0.5, 1, etc.)
        else if (!isDragging)
        {
            float step = (pageCount <= 1) ? 0f : 1f / (pageCount - 1);

            if (step > 0f)
            {
                float pos = scrollRect.horizontalNormalizedPosition;
                int nearestIndex = Mathf.RoundToInt(pos / step);
                nearestIndex = Mathf.Clamp(nearestIndex, 0, pageCount - 1);

                float snappedPos = step * nearestIndex;

                // Solo si está lo bastante lejos lo reajustamos
                if (Mathf.Abs(pos - snappedPos) > 0.0001f)
                {
                    scrollRect.horizontalNormalizedPosition = snappedPos;
                    currentPage = nearestIndex;
                    UpdateDots();
                }
            }
        }
    }

    // Llamado desde el manager para hacer un reset fuerte tras el layout
    public void HardResetToFirstPage()
    {
        if (scrollRect == null) return;

        isSnapping = false;
        isDragging = false;
        scrollRect.velocity = Vector2.zero;

        Canvas.ForceUpdateCanvases();

        currentPage = 0;
        targetPos = 0f;
        scrollRect.horizontalNormalizedPosition = 0f;

        UpdateDots();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (scrollRect == null) return;

        isSnapping = false;
        isDragging = true;
        dragStartPos = scrollRect.horizontalNormalizedPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (scrollRect == null || pageCount <= 1) return;

        isDragging = false;

        float endPos = scrollRect.horizontalNormalizedPosition;
        float delta = endPos - dragStartPos;

        int targetPage = currentPage;

        if (Mathf.Abs(delta) > swipeThreshold)
        {
            // Deslizar hacia la derecha -> página siguiente
            // Deslizar hacia la izquierda -> página anterior
            if (delta > 0f)
                targetPage = Mathf.Min(currentPage + 1, pageCount - 1);
            else
                targetPage = Mathf.Max(currentPage - 1, 0);
        }
        else
        {
            // Snap a la más cercana
            float step = (pageCount <= 1) ? 0f : 1f / (pageCount - 1);
            if (step > 0f)
                targetPage = Mathf.RoundToInt(endPos / step);
        }

        SetPage(targetPage, instant: false);
    }

    private void SetPage(int pageIndex, bool instant)
    {
        pageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);
        currentPage = pageIndex;

        float normalized = (pageCount <= 1) ? 0f : (float)pageIndex / (pageCount - 1);

        if (scrollRect != null)
        {
            if (instant)
            {
                isSnapping = false;
                scrollRect.velocity = Vector2.zero;
                scrollRect.horizontalNormalizedPosition = normalized;
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

    // Por si quieres llamarlo desde otros sitios
    public void GoToPage(int index, bool instant = false)
    {
        if (instant && scrollRect != null)
            scrollRect.velocity = Vector2.zero;

        SetPage(index, instant);
    }
}
