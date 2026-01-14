using UnityEngine;
using UnityEngine.EventSystems;

public class TurboButtonUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private TurboController turbo;

    public void OnPointerDown(PointerEventData eventData)
    {
        turbo.Hold();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        turbo.Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        turbo.Release();
    }
}
