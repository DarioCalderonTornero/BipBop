using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class TutorialPanelUI : MonoBehaviour
{
    [Header("Segments (orden manual)")]
    [SerializeField] private List<RectTransform> segments = new();

    [Header("UI")]
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI startButtonText; // texto del botón

    [Header("Animation")]
    [SerializeField] private float slideFromX = -1200f;
    [SerializeField] private float segmentDuration = 0.35f;

    [Header("Button Localization")]
    [SerializeField] private LocalizedString lsNext;
    [SerializeField] private LocalizedString lsPlay;

    [Header("Button Font Sizes")]
    [SerializeField] private float nextFontSize = 36f;
    [SerializeField] private float playFontSize = 42f;

    public event Action OnClosed;

    private readonly List<Vector2> originalPositions = new();
    private readonly List<CanvasGroup> canvasGroups = new();

    private Coroutine animCo;
    private int currentIndex = -1;
    private bool isAnimating;

    private void Awake()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnButtonPressed);
        }

        CacheSegments();
        PrepareHidden();
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        StartTutorial();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void CacheSegments()
    {
        originalPositions.Clear();
        canvasGroups.Clear();

        for (int i = 0; i < segments.Count; i++)
        {
            RectTransform rt = segments[i];
            if (rt == null)
            {
                originalPositions.Add(Vector2.zero);
                canvasGroups.Add(null);
                continue;
            }

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
            if (rt == null) continue;

            CanvasGroup cg = canvasGroups[i];
            rt.anchoredPosition = originalPositions[i] + new Vector2(slideFromX, 0f);
            if (cg != null) cg.alpha = 0f;

            // ✅ NO desactivar: siempre visibles (aunque estén fuera y alpha 0)
            rt.gameObject.SetActive(true);
        }

        SetButtonInteractable(false);
        SetButtonLabel(isLast: false);
    }

    private void StartTutorial()
    {
        PrepareHidden();
        currentIndex = -1;
        ShowNextSegment();
    }

    private void OnButtonPressed()
    {
        if (isAnimating) return;

        // último -> cerrar
        if (currentIndex >= segments.Count - 1)
        {
            Close();
            return;
        }

        ShowNextSegment();
    }

    private void ShowNextSegment()
    {
        currentIndex++;

        if (currentIndex < 0 || currentIndex >= segments.Count) return;

        bool isLast = (currentIndex == segments.Count - 1);
        SetButtonLabel(isLast);

        if (animCo != null) StopCoroutine(animCo);
        animCo = StartCoroutine(AnimateCurrentIn());
    }

    private IEnumerator AnimateCurrentIn()
    {
        isAnimating = true;
        SetButtonInteractable(false);

        RectTransform rt = segments[currentIndex];
        if (rt == null)
        {
            isAnimating = false;
            SetButtonInteractable(true);
            yield break;
        }

        CanvasGroup cg = canvasGroups[currentIndex];
        Vector2 endPos = originalPositions[currentIndex];
        Vector2 startPos = endPos + new Vector2(slideFromX, 0f);

        float t = 0f;

        rt.anchoredPosition = startPos;
        if (cg != null) cg.alpha = 0f;

        while (t < segmentDuration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / segmentDuration);

            // EaseOutCubic
            float eased = 1f - Mathf.Pow(1f - u, 3f);

            rt.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, eased);
            if (cg != null) cg.alpha = eased;

            yield return null;
        }

        rt.anchoredPosition = endPos;
        if (cg != null) cg.alpha = 1f;

        isAnimating = false;
        SetButtonInteractable(true);
        animCo = null;
    }

    private void SetButtonInteractable(bool value)
    {
        if (startButton != null)
            startButton.interactable = value;
    }

    private void SetButtonLabel(bool isLast)
    {
        if (startButtonText == null) return;

        if (isLast)
        {
            startButtonText.text = (lsPlay != null) ? lsPlay.GetLocalizedString() : "Jugar";
            startButtonText.fontSize = playFontSize;
        }
        else
        {
            startButtonText.text = (lsNext != null) ? lsNext.GetLocalizedString() : "Siguiente";
            startButtonText.fontSize = nextFontSize;
        }
    }

    private void OnLocaleChanged(Locale _)
    {
        // si aún no hemos empezado, asume "Siguiente"
        bool isLast = (currentIndex >= 0 && currentIndex == segments.Count - 1);
        SetButtonLabel(isLast);
    }

    private void Close()
    {
        OnClosed?.Invoke();
        Destroy(gameObject);
    }
}
