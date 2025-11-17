using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VerticalScrollWithHorizontalSwipe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Referencias")]
    public ScrollRect verticalScroll;   // ScrollRect vertical de tienda/rankings
    public SwipeMenu swipeMenu;         // Tu script de páginas horizontales

    [Header("Ajustes")]
    public float deadZone = 20f;        // Pixels mínimos para decidir la dirección

    private Vector2 startPos;
    private bool dragging = false;

    private enum DragMode { None, Vertical, Horizontal }
    private DragMode mode = DragMode.None;

    private void Awake()
    {
        if (verticalScroll == null)
            verticalScroll = GetComponent<ScrollRect>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        startPos = eventData.position;
        dragging = true;
        mode = DragMode.None;
        // No llamamos aún al ScrollRect, esperamos a saber si es vertical u horizontal
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging) return;

        Vector2 delta = eventData.position - startPos;

        // Aún no sabemos qué tipo de drag es
        if (mode == DragMode.None)
        {
            if (delta.magnitude > deadZone)
            {
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                {
                    // 👉 Es swipe horizontal (cambiar pestaña)
                    mode = DragMode.Horizontal;
                    verticalScroll.enabled = false; // bloqueamos el scroll mientras sea horizontal
                }
                else
                {
                    // 👉 Es scroll vertical normal
                    mode = DragMode.Vertical;

                    // Le pasamos el beginDrag al ScrollRect para que empiece a scrollear
                    (verticalScroll as IBeginDragHandler)?.OnBeginDrag(eventData);
                }
            }
        }

        if (mode == DragMode.Vertical)
        {
            (verticalScroll as IDragHandler)?.OnDrag(eventData);
        }
        // Si es horizontal, aquí no hacemos nada más: solo nos interesa en OnEndDrag
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging) return;

        Vector2 delta = eventData.position - startPos;

        if (mode == DragMode.Vertical)
        {
            (verticalScroll as IEndDragHandler)?.OnEndDrag(eventData);
        }
        else if (mode == DragMode.Horizontal)
        {
            // Interpretamos el swipe lateral
            if (delta.x > deadZone)
            {
                // Deslizó hacia la derecha → ir a página anterior
                swipeMenu?.Previous();
            }
            else if (delta.x < -deadZone)
            {
                // Deslizó hacia la izquierda → ir a página siguiente
                swipeMenu?.Next();
            }
        }

        verticalScroll.enabled = true;
        dragging = false;
        mode = DragMode.None;
    }
}
