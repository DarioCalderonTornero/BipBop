using System.Collections;
using UnityEngine;

public class StoreScreenSlider : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform screensRoot;
    [SerializeField] private RectTransform viewport;

    [Header("Pages (exactamente 2)")]
    [SerializeField] private RectTransform storeMainPanel;
    [SerializeField] private RectTransform dailyLuckPanel;

    [Header("Anim")]
    [SerializeField] private float slideDuration = 0.25f;
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private int currentPage = 0;
    private Coroutine slideRoutine;

    private void Awake()
    {
        if (viewport == null && transform.parent is RectTransform rt)
            viewport = rt;
    }

    private void OnEnable()
    {
        StartCoroutine(RefreshLayoutNextFrame());
    }

    private IEnumerator RefreshLayoutNextFrame()
    {
        yield return null; // espera a que el layout calcule tamaños en móvil
        Canvas.ForceUpdateCanvases();
        ApplyPageSizes();
        GoToPage(currentPage, instant: true);
    }

    private void ApplyPageSizes()
    {
        if (viewport == null) return;

        float w = viewport.rect.width;
        float h = viewport.rect.height;

        if (storeMainPanel != null) storeMainPanel.sizeDelta = new Vector2(w, h);
        if (dailyLuckPanel != null) dailyLuckPanel.sizeDelta = new Vector2(w, h);

        if (screensRoot != null)
            screensRoot.sizeDelta = new Vector2(w * 2f, h);
    }

    public void GoToStore(bool instant = false) => GoToPage(0, instant);
    public void GoToDailyLuck(bool instant = false) => GoToPage(1, instant);

    public void GoToPage(int page, bool instant = false)
    {
        currentPage = Mathf.Clamp(page, 0, 1);
        if (screensRoot == null || viewport == null) return;

        Canvas.ForceUpdateCanvases();
        float pageWidth = viewport.rect.width;

        Vector2 target = new Vector2(-currentPage * pageWidth, screensRoot.anchoredPosition.y);

        if (slideRoutine != null) StopCoroutine(slideRoutine);

        if (instant || slideDuration <= 0f)
        {
            screensRoot.anchoredPosition = target;
            return;
        }

        slideRoutine = StartCoroutine(SlideTo(target));
    }

    private IEnumerator SlideTo(Vector2 target)
    {
        Vector2 start = screensRoot.anchoredPosition;
        float t = 0f;

        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / slideDuration);
            float e = slideCurve.Evaluate(p);
            screensRoot.anchoredPosition = Vector2.Lerp(start, target, e);
            yield return null;
        }

        screensRoot.anchoredPosition = target;
        slideRoutine = null;
    }

    private void OnRectTransformDimensionsChange()
    {
        // por rotación/cambio resolución
        if (!isActiveAndEnabled) return;
        Canvas.ForceUpdateCanvases();
        ApplyPageSizes();
        GoToPage(currentPage, instant: true);
    }
}
