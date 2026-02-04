using UnityEngine;
using UnityEngine.UI;

public class ScrollRectHintArrows : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private GameObject leftHint;
    [SerializeField] private GameObject rightHint;

    [Header("Tuning")]
    [SerializeField, Range(0f, 0.2f)] private float edgeThreshold = 0.02f; // margen para evitar parpadeos

    private void Reset()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    private void OnEnable()
    {
        if (scrollRect != null)
            scrollRect.onValueChanged.AddListener(_ => Refresh());
        Refresh();
    }

    private void OnDisable()
    {
        if (scrollRect != null)
            scrollRect.onValueChanged.RemoveListener(_ => Refresh());
    }

    private void LateUpdate()
    {
        // Por si el contenido cambia (localization, layout, etc.)
        Refresh();
    }

    private void Refresh()
    {
        if (scrollRect == null || scrollRect.content == null || scrollRect.viewport == null)
            return;

        RectTransform content = scrollRect.content;
        RectTransform viewport = scrollRect.viewport;

        // Si no hay overflow, no hay flechas.
        float contentWidth = content.rect.width;
        float viewWidth = viewport.rect.width;

        bool hasOverflow = contentWidth > viewWidth + 0.5f;

        if (!hasOverflow)
        {
            if (leftHint) leftHint.SetActive(false);
            if (rightHint) rightHint.SetActive(false);
            return;
        }

        // 0 = totalmente a la izquierda, 1 = totalmente a la derecha
        float n = scrollRect.horizontalNormalizedPosition;

        bool showLeft = n > edgeThreshold;
        bool showRight = n < 1f - edgeThreshold;

        if (leftHint) leftHint.SetActive(showLeft);
        if (rightHint) rightHint.SetActive(showRight);
    }
}
