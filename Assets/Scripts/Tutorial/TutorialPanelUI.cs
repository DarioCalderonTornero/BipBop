using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPanelUI : MonoBehaviour
{
    [Header("Segments (orden manual)")]
    [SerializeField] private List<RectTransform> segments = new();

    [Header("UI")]
    [SerializeField] private Button startButton;

    [Header("Animation")]
    [SerializeField] private float slideFromX = -1200f;
    [SerializeField] private float segmentDuration = 0.35f;
    [SerializeField] private float segmentStagger = 0.5f;
    [SerializeField] private bool disableButtonUntilEnd = true;

    public event Action OnClosed;

    private readonly List<Vector2> originalPositions = new();
    private readonly List<CanvasGroup> canvasGroups = new();

    private Coroutine showRoutine;

    private void Awake()
    {
        if (startButton != null)
            startButton.onClick.AddListener(Close);

        CacheSegments();
        PrepareHidden();
    }

    private void OnEnable()
    {
        Show();
    }

    private void CacheSegments()
    {
        originalPositions.Clear();
        canvasGroups.Clear();

        for (int i = 0; i < segments.Count; i++)
        {
            RectTransform rt = segments[i];
            if (rt == null) continue;

            originalPositions.Add(rt.anchoredPosition);

            CanvasGroup cg = rt.GetComponent<CanvasGroup>();
            if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
            canvasGroups.Add(cg);
        }
    }

    private void PrepareHidden()
    {
        for (int i = 0; i < segments.Count; i++)
        {
            RectTransform rt = segments[i];
            CanvasGroup cg = canvasGroups[i];

            rt.anchoredPosition = originalPositions[i] + new Vector2(slideFromX, 0f);
            cg.alpha = 0f;
        }

        if (startButton != null && disableButtonUntilEnd)
            startButton.interactable = false;
    }

    public void Show()
    {
        if (showRoutine != null)
            StopCoroutine(showRoutine);

        PrepareHidden();
        showRoutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        for (int i = 0; i < segments.Count; i++)
        {
            yield return AnimateIn(
                segments[i],
                canvasGroups[i],
                originalPositions[i]
            );

            yield return new WaitForSecondsRealtime(segmentStagger);
        }

        if (startButton != null)
            startButton.interactable = true;

        showRoutine = null;
    }

    private IEnumerator AnimateIn(RectTransform rt, CanvasGroup cg, Vector2 endPos)
    {
        float t = 0f;
        Vector2 startPos = endPos + new Vector2(slideFromX, 0f);

        while (t < segmentDuration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / segmentDuration);

            // EaseOutCubic
            float eased = 1f - Mathf.Pow(1f - u, 3f);

            rt.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, eased);
            cg.alpha = eased;

            yield return null;
        }

        rt.anchoredPosition = endPos;
        cg.alpha = 1f;
    }

    private void Close()
    {
        OnClosed?.Invoke();
        Destroy(gameObject);
    }
}
