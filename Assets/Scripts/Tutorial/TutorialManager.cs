using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    private const string PREF_TUTORIAL_DONE = "TutorialCompleted";

    public enum StepType { TapAnywhere, RequireTargetClick }

    [System.Serializable]
    public class TutorialStep
    {
        [TextArea] public string text;
        public StepType type = StepType.TapAnywhere;

        [Header("Delay")]
        [Min(0f)] public float delayBeforeStep = 0f;

        [Header("Only if RequireTargetClick")]
        public RectTransform target;
        public Vector2 holePadding = new Vector2(30, 30);

        [Header("Tooltip placement")]
        public Vector2 tooltipOffset = new Vector2(0, 180);
    }

    [Header("Steps")]
    [SerializeField] private List<TutorialStep> steps = new();

    [Header("UI Refs")]
    [SerializeField] private Canvas tutorialCanvas;
    [SerializeField] private TMP_Text tutorialText;
    [SerializeField] private RectTransform tooltipPanel;

    [Header("Overlay parts")]
    [SerializeField] private RectTransform darkTop;
    [SerializeField] private RectTransform darkBottom;
    [SerializeField] private RectTransform darkLeft;
    [SerializeField] private RectTransform darkRight;
    [SerializeField] private RectTransform highlightBorder;

    [Header("Tap Anywhere")]
    [SerializeField] private Button tapCatcherButton;

    [Header("Game UI root")]
    [SerializeField] private CanvasGroup gameUiCanvasGroup;

    private int index = 0;

    // reparent control
    private Transform savedParent;
    private int savedSiblingIndex;
    private RectTransform savedTarget;
    private Button savedTargetButton;

    private Coroutine stepRoutine;

    [Header("Overlay margin (pixels)")]
    [SerializeField] private float overscanPixels = 30f;

    [Header("Tutorial Root (for show/hide)")]
    [SerializeField] private CanvasGroup tutorialRootCanvasGroup;

    [Header("Hole Animation")]
    [SerializeField, Min(0f)] private float holeAnimDuration = 0.25f;
    [SerializeField] private AnimationCurve holeAnimCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Tooltip Fade")]
    [SerializeField, Min(0f)] private float tooltipFadeDuration = 0.15f;

    [SerializeField] private CanvasGroup inputBlockerCanvasGroup;

    [SerializeField] private Vector2 defaultTooltipPos = new Vector2(0f, -350f);
    [SerializeField] private Vector2 tooltipScreenPadding = new Vector2(40f, 60f);


    private void Awake()
    {
        tooltipCanvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
        if (tooltipCanvasGroup == null)
            tooltipCanvasGroup = tooltipPanel.gameObject.AddComponent<CanvasGroup>();

        if (PlayerPrefs.GetInt(PREF_TUTORIAL_DONE, 0) == 0)
        {
            tutorialCanvas.gameObject.SetActive(true);
            BlockAllInput(true);
        }
    }

    private void Start()
    {
        if (PlayerPrefs.GetInt(PREF_TUTORIAL_DONE, 0) == 1)
        {
            tutorialCanvas.gameObject.SetActive(false);
            BlockAllInput(false);
            return;
        }

        tapCatcherButton.onClick.RemoveAllListeners();
        tapCatcherButton.onClick.AddListener(() =>
        {
            if (index >= 0 && index < steps.Count && steps[index].type == StepType.TapAnywhere)
                Next();
        });

        index = 0;
        StartStep(index);
    }

    private void StartStep(int i)
    {
        if (stepRoutine != null) StopCoroutine(stepRoutine);
        stepRoutine = StartCoroutine(ShowStepRoutine(i));
    }

    private IEnumerator ShowStepRoutine(int i)
    {
        CleanupTargetStep();

        var step = steps[i];


        // =========================
        // DELAY (invisible)
        // =========================
        SetTutorialVisible(false);
        tooltipPanel.gameObject.SetActive(false);

        // durante delay, NO avanzar
        tapCatcherButton.gameObject.SetActive(true);
        tapCatcherButton.interactable = false;

        float d = Mathf.Max(0f, step.delayBeforeStep);
        if (d > 0f)
            yield return new WaitForSeconds(d);

        // =========================
        // STEP (visible)
        // =========================
        SetTutorialVisible(true);
        tutorialText.text = step.text;

        // TAP ANYWHERE
        if (step.type == StepType.TapAnywhere)
        {
            yield return AnimateOverlayTo(BuildFullDarkState(), holeAnimDuration);

            PositionTooltipDefault();
            StartCoroutine(FadeInTooltip());

            // ✅ permitir el click SOLO aquí (tapCatcher está encima del blocker)
            tapCatcherButton.gameObject.SetActive(true);
            tapCatcherButton.interactable = true;

            yield break;
        }

        // REQUIRE TARGET CLICK
        tapCatcherButton.gameObject.SetActive(false);

        BringTargetToTutorial(step.target);

        savedTargetButton = step.target.GetComponent<Button>();
        if (savedTargetButton != null)
        {
            savedTargetButton.onClick.RemoveListener(Next);
            savedTargetButton.onClick.AddListener(Next);
        }

        BringTargetToTutorial(step.target);

        yield return AnimateOverlayTo(BuildHoleState(step.target, step.holePadding), holeAnimDuration);

        if (highlightBorder != null)
        {
            Rect hole = GetTargetRectInOverlaySpace(step.target, step.holePadding);
            highlightBorder.gameObject.SetActive(true);
            SetRect(highlightBorder, hole.xMin, hole.yMin, hole.width, hole.height);
        }

        PositionTooltipNearTarget(step.target, step.tooltipOffset);
        ClampTooltipToCanvas();
        StartCoroutine(FadeInTooltip());

        savedTargetButton = step.target.GetComponent<Button>();
        if (savedTargetButton != null)
            savedTargetButton.onClick.AddListener(Next);
        else
            Debug.LogWarning("El target no tiene Button.");
    }

    private void BlockAllInput(bool blocked)
    {
        if (inputBlockerCanvasGroup == null) return;

        inputBlockerCanvasGroup.gameObject.SetActive(blocked);
        inputBlockerCanvasGroup.alpha = 0f;          // invisible
        inputBlockerCanvasGroup.interactable = blocked;
        inputBlockerCanvasGroup.blocksRaycasts = blocked;
    }

    private void AllowTapAnywhere(bool allow)
    {
        // TapCatcher va en el canvas del tutorial (por encima del blocker),
        // así que no necesitamos “agujeros”: solo activar el tapCatcher.
        tapCatcherButton.gameObject.SetActive(true);
        tapCatcherButton.interactable = allow;
    }

    private void Next()
    {
        CleanupTargetStep();

        index++;
        if (index >= steps.Count)
        {
            CompleteTutorial();
            return;
        }

        StartStep(index);
    }

    private void CompleteTutorial()
    {
        if (stepRoutine != null)
        {
            StopCoroutine(stepRoutine);
            stepRoutine = null;
        }

        CleanupTargetStep();

        PlayerPrefs.SetInt(PREF_TUTORIAL_DONE, 1);
        PlayerPrefs.Save();

        BlockAllInput(false);

        SetTutorialVisible(true);
        tutorialCanvas.gameObject.SetActive(false);
    }

    // ---------- Reparent target ----------
    private void BringTargetToTutorial(RectTransform target)
    {
        savedTarget = target;
        savedParent = target.parent;
        savedSiblingIndex = target.GetSiblingIndex();

        // Mantener posición visual: SetParent con worldPositionStays=true
        target.SetParent(tutorialCanvas.transform, true);
        target.SetAsLastSibling(); // por encima del overlay
    }

    private void RestoreTarget()
    {
        if (savedTarget == null) return;

        savedTarget.SetParent(savedParent, true);
        savedTarget.SetSiblingIndex(savedSiblingIndex);

        savedTarget = null;
        savedParent = null;
        savedSiblingIndex = 0;
    }

    private void CleanupTargetStep()
    {
        if (savedTargetButton != null)
        {
            savedTargetButton.onClick.RemoveListener(Next);
            savedTargetButton = null;
        }

        RestoreTarget();
    }

    private void SetByAnchors(RectTransform rt, float axMin, float ayMin, float axMax, float ayMax)
    {
        rt.anchorMin = new Vector2(axMin, ayMin);
        rt.anchorMax = new Vector2(axMax, ayMax);

        // CLAVE: esto evita que se quede "gigante" o con offsets viejos
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    private Rect GetTargetRectInOverlaySpace(RectTransform target, Vector2 padding)
    {
        RectTransform root = (RectTransform)tutorialCanvas.transform;

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        Vector2 s0 = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        Vector2 s2 = RectTransformUtility.WorldToScreenPoint(null, corners[2]);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, s0, null, out var p0);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, s2, null, out var p2);

        float xMin = Mathf.Min(p0.x, p2.x) - padding.x;
        float xMax = Mathf.Max(p0.x, p2.x) + padding.x;
        float yMin = Mathf.Min(p0.y, p2.y) - padding.y;
        float yMax = Mathf.Max(p0.y, p2.y) + padding.y;

        float W = root.rect.width;
        float H = root.rect.height;

        xMin += W * 0.5f; xMax += W * 0.5f;
        yMin += H * 0.5f; yMax += H * 0.5f;

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private struct OverlayState
    {
        public Vector2 topMin, topMax;
        public Vector2 bottomMin, bottomMax;
        public Vector2 leftMin, leftMax;
        public Vector2 rightMin, rightMax;

        public bool topActive, bottomActive, leftActive, rightActive;
    }

    private OverlayState GetCurrentOverlayState()
    {
        return new OverlayState
        {
            topMin = darkTop.anchorMin,
            topMax = darkTop.anchorMax,
            bottomMin = darkBottom.anchorMin,
            bottomMax = darkBottom.anchorMax,
            leftMin = darkLeft.anchorMin,
            leftMax = darkLeft.anchorMax,
            rightMin = darkRight.anchorMin,
            rightMax = darkRight.anchorMax,

            topActive = darkTop.gameObject.activeSelf,
            bottomActive = darkBottom.gameObject.activeSelf,
            leftActive = darkLeft.gameObject.activeSelf,
            rightActive = darkRight.gameObject.activeSelf
        };
    }

    private void ApplyOverlayState(OverlayState s)
    {
        darkTop.gameObject.SetActive(s.topActive);
        darkBottom.gameObject.SetActive(s.bottomActive);
        darkLeft.gameObject.SetActive(s.leftActive);
        darkRight.gameObject.SetActive(s.rightActive);

        SetByAnchors(darkTop, s.topMin.x, s.topMin.y, s.topMax.x, s.topMax.y);
        SetByAnchors(darkBottom, s.bottomMin.x, s.bottomMin.y, s.bottomMax.x, s.bottomMax.y);
        SetByAnchors(darkLeft, s.leftMin.x, s.leftMin.y, s.leftMax.x, s.leftMax.y);
        SetByAnchors(darkRight, s.rightMin.x, s.rightMin.y, s.rightMax.x, s.rightMax.y);
    }

    private OverlayState BuildHoleState(RectTransform target, Vector2 padding)
    {
        // Todos activos en modo agujero
        OverlayState s = new OverlayState
        {
            topActive = true,
            bottomActive = true,
            leftActive = true,
            rightActive = true
        };

        Rect hole = GetTargetRectInOverlaySpace(target, padding);

        RectTransform root = (RectTransform)tutorialCanvas.transform;
        float W = root.rect.width;
        float H = root.rect.height;
        if (W <= 0f || H <= 0f) return GetCurrentOverlayState();

        float xMin = Mathf.Clamp01(hole.xMin / W);
        float xMax = Mathf.Clamp01(hole.xMax / W);
        float yMin = Mathf.Clamp01(hole.yMin / H);
        float yMax = Mathf.Clamp01(hole.yMax / H);

        // TOP
        s.topMin = new Vector2(0f, yMax);
        s.topMax = new Vector2(1f, 1f);

        // BOTTOM
        s.bottomMin = new Vector2(0f, 0f);
        s.bottomMax = new Vector2(1f, yMin);

        // LEFT
        s.leftMin = new Vector2(0f, yMin);
        s.leftMax = new Vector2(xMin, yMax);

        // RIGHT
        s.rightMin = new Vector2(xMax, yMin);
        s.rightMax = new Vector2(1f, yMax);

        return s;
    }

    private OverlayState BuildFullDarkState()
    {
        // Solo darkBottom ocupa todo
        OverlayState s = GetCurrentOverlayState();
        s.topActive = false;
        s.leftActive = false;
        s.rightActive = false;
        s.bottomActive = true;

        s.bottomMin = new Vector2(0f, 0f);
        s.bottomMax = new Vector2(1f, 1f);

        return s;
    }

    private IEnumerator AnimateOverlayTo(OverlayState targetState, float duration)
    {
        // Si duration 0 => aplica sin animar
        if (duration <= 0f)
        {
            ApplyOverlayState(targetState);
            yield break;
        }

        // Importante: activar todos los que vayan a participar para ver la animación
        // (si un panel está desactivado no se ve animar, por eso lo forzamos activo durante la interpolación)
        darkTop.gameObject.SetActive(true);
        darkBottom.gameObject.SetActive(true);
        darkLeft.gameObject.SetActive(true);
        darkRight.gameObject.SetActive(true);

        OverlayState start = GetCurrentOverlayState();

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // para que funcione aunque pauses timeScale
            float u = Mathf.Clamp01(t / duration);
            float eased = holeAnimCurve != null ? holeAnimCurve.Evaluate(u) : u;

            OverlayState s = new OverlayState
            {
                topActive = true,
                bottomActive = true,
                leftActive = true,
                rightActive = true,

                topMin = Lerp2(start.topMin, targetState.topMin, eased),
                topMax = Lerp2(start.topMax, targetState.topMax, eased),

                bottomMin = Lerp2(start.bottomMin, targetState.bottomMin, eased),
                bottomMax = Lerp2(start.bottomMax, targetState.bottomMax, eased),

                leftMin = Lerp2(start.leftMin, targetState.leftMin, eased),
                leftMax = Lerp2(start.leftMax, targetState.leftMax, eased),

                rightMin = Lerp2(start.rightMin, targetState.rightMin, eased),
                rightMax = Lerp2(start.rightMax, targetState.rightMax, eased),
            };

            ApplyOverlayState(s);
            yield return null;
        }

        // Al final, aplicamos el target “real” (incluye activar/desactivar correctos)
        ApplyOverlayState(targetState);
    }

    private static Vector2 Lerp2(Vector2 a, Vector2 b, float t) => Vector2.LerpUnclamped(a, b, t);

    // ---------- Tooltip positioning ----------
    private void PositionTooltipNearTarget(RectTransform target, Vector2 offset)
    {
        RectTransform root = (RectTransform)tutorialCanvas.transform;

        Vector3 worldPos = target.TransformPoint(target.rect.center);
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, worldPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPos, null, out var local);
        tooltipPanel.anchoredPosition = local + offset;
    }

    private void PositionTooltipDefault()
    {
        tooltipPanel.anchoredPosition = defaultTooltipPos;
        ClampTooltipToCanvas();
    }

    private void ClampTooltipToCanvas()
    {
        var root = (RectTransform)tutorialCanvas.transform;
        if (root == null) return;

        Canvas.ForceUpdateCanvases();

        // Rect del tooltip en espacio del root
        Vector3[] corners = new Vector3[4];
        tooltipPanel.GetWorldCorners(corners);

        Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);

        // límites de pantalla con padding
        float leftLimit = tooltipScreenPadding.x;
        float rightLimit = Screen.width - tooltipScreenPadding.x;
        float bottomLimit = tooltipScreenPadding.y;
        float topLimit = Screen.height - tooltipScreenPadding.y;

        Vector2 delta = Vector2.zero;

        if (min.x < leftLimit) delta.x += (leftLimit - min.x);
        if (max.x > rightLimit) delta.x -= (max.x - rightLimit);
        if (min.y < bottomLimit) delta.y += (bottomLimit - min.y);
        if (max.y > topLimit) delta.y -= (max.y - topLimit);

        if (delta != Vector2.zero)
        {
            // convertir delta screen -> local
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, delta, null, out var localDelta);
            tooltipPanel.anchoredPosition += localDelta;
        }
    }

    // ---------- Rect utils ----------
    private void SetRect(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    private void SetTutorialVisible(bool visible)
    {
        if (tutorialRootCanvasGroup == null) return;

        tutorialRootCanvasGroup.alpha = visible ? 1f : 0f;
        tutorialRootCanvasGroup.interactable = visible;   // solo interactúa cuando se ve
        tutorialRootCanvasGroup.blocksRaycasts = visible; // el overlay solo bloquea cuando se ve
    }
    private CanvasGroup tooltipCanvasGroup;

    private IEnumerator FadeInTooltip()
    {
        tooltipCanvasGroup.alpha = 0f;
        tooltipPanel.gameObject.SetActive(true);

        float t = 0f;
        while (t < tooltipFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            tooltipCanvasGroup.alpha = Mathf.Clamp01(t / tooltipFadeDuration);
            yield return null;
        }

        tooltipCanvasGroup.alpha = 1f;
    }



}
