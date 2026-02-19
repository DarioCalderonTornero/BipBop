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
    [SerializeField] private HoleOverlay holeOverlay;

    [Header("Tap Anywhere")]
    [SerializeField] private Button tapCatcherButton;

    [Header("Game UI root")]
    [SerializeField] private CanvasGroup gameUiCanvasGroup;

    private int index = 0;

    // Reparent + placeholder (el placeholder ocupa el hueco en el Layout)
    private Transform savedParent;
    private int savedSiblingIndex;
    private RectTransform savedTarget;
    private Button savedTargetButton;
    private GameObject layoutPlaceholder; // invisible, mantiene el hueco en el Layout

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

    [SerializeField] private GameObject inputBlockerObject;


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
        // Limpieza previa
        CleanupTargetStep();
        var step = steps[i];

        // ==========================================
        // 1. BLOQUEO ABSOLUTO (Antes de cualquier delay)
        // ==========================================
        // Activamos el objeto que tapa TODA la pantalla.
        if (inputBlockerObject != null)
        {
            inputBlockerObject.SetActive(true);
        }

        // Ocultamos el contenido visual del tutorial (el texto y flechas)
        // pero el InputBlocker de arriba sigue vivo porque es independiente.
        SetTutorialVisible(false);
        tooltipPanel.gameObject.SetActive(false);
        tapCatcherButton.gameObject.SetActive(false);

        // Esperamos el tiempo que hayas definido
        float d = Mathf.Max(0f, step.delayBeforeStep);
        if (d > 0f)
        {
            yield return new WaitForSeconds(d);
        }

        // ==========================================
        // 2. FIN DEL DELAY: PASAMOS AL CONTROL SELECTIVO
        // ==========================================
        // Desactivamos el bloqueador total para permitir que el Tutorial actúe.
        if (inputBlockerObject != null)
        {
            inputBlockerObject.SetActive(false);
        }

        // Mostramos el contenido visual
        SetTutorialVisible(true);
        tutorialText.text = step.text;

        // --- CAMBIO CLAVE AQUÍ ---
        // Esperamos al final del frame para que el ContentSizeFitter o el Layout 
        // calculen el tamaño REAL del panel con el nuevo texto.
        yield return new WaitForEndOfFrame();

        // Activamos el tapCatcherButton
        tapCatcherButton.gameObject.SetActive(true);

        if (step.type == StepType.TapAnywhere)
        {
            tapCatcherButton.interactable = true;
            yield return AnimateOverlayTo(Rect.zero, holeAnimDuration); // sin agujero = overlay completo
            PositionTooltipDefault(); // Posición central
        }
        else
        {
            tapCatcherButton.interactable = false;
            if (step.target != null)
            {
                // Mueve el target al TutorialCanvas y deja un placeholder en su sitio.
                BringTargetToTutorial(step.target);

                // Registrar listener en el botón original para avanzar al pulsar.
                savedTargetButton = step.target.GetComponent<Button>();
                if (savedTargetButton != null)
                    savedTargetButton.onClick.AddListener(Next);

                yield return AnimateOverlayTo(GetHoleRectLocal(step.target, step.holePadding), holeAnimDuration);

                // Posicionamos respecto al target original (que no se ha movido)
                PositionTooltipNearTarget(step.target, step.tooltipOffset);
            }
        }

        // El Clamp siempre al final, después de haber decidido la posición inicial
        ClampTooltipToCanvas();
        StartCoroutine(FadeInTooltip());
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

    // ---------- Reparent con placeholder ----------

    /// <summary>
    /// Mueve el target al TutorialCanvas (para que quede encima del overlay)
    /// y deja un placeholder invisible en su lugar dentro del Layout original,
    /// con el mismo tamaño, para que los hermanos no se redistribuyan.
    /// </summary>
    private void BringTargetToTutorial(RectTransform target)
    {
        savedTarget = target;
        savedParent = target.parent;
        savedSiblingIndex = target.GetSiblingIndex();

        // --- Crear placeholder que ocupa el hueco en el Layout ---
        layoutPlaceholder = new GameObject("__TutorialPlaceholder__", typeof(RectTransform));
        layoutPlaceholder.transform.SetParent(savedParent, false);
        layoutPlaceholder.transform.SetSiblingIndex(savedSiblingIndex);

        // Copiar el LayoutElement si existe, o forzar el mismo sizeDelta/preferredSize
        RectTransform placeholderRT = (RectTransform)layoutPlaceholder.transform;
        placeholderRT.anchorMin = target.anchorMin;
        placeholderRT.anchorMax = target.anchorMax;
        placeholderRT.pivot = target.pivot;
        placeholderRT.sizeDelta = target.sizeDelta;
        placeholderRT.anchoredPosition = target.anchoredPosition;

        LayoutElement srcLE = target.GetComponent<LayoutElement>();
        if (srcLE != null)
        {
            LayoutElement dstLE = layoutPlaceholder.AddComponent<LayoutElement>();
            dstLE.minWidth = srcLE.minWidth;
            dstLE.minHeight = srcLE.minHeight;
            dstLE.preferredWidth = srcLE.preferredWidth;
            dstLE.preferredHeight = srcLE.preferredHeight;
            dstLE.flexibleWidth = srcLE.flexibleWidth;
            dstLE.flexibleHeight = srcLE.flexibleHeight;
            dstLE.ignoreLayout = srcLE.ignoreLayout;
        }
        else
        {
            // Sin LayoutElement explícito: forzamos el tamaño con uno nuevo
            // para que el HorizontalLayoutGroup lo respete igual que al original.
            LayoutElement le = layoutPlaceholder.AddComponent<LayoutElement>();
            le.preferredWidth = target.rect.width;
            le.preferredHeight = target.rect.height;
        }

        // Invisble: no tiene Image ni nada que se vea
        CanvasGroup cg = layoutPlaceholder.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        // --- Mover el original al TutorialCanvas encima del overlay ---
        target.SetParent(tutorialCanvas.transform, true); // worldPositionStays=true
        target.SetAsLastSibling();
    }

    private void RestoreTarget()
    {
        if (savedTarget != null)
        {
            savedTarget.SetParent(savedParent, true);
            savedTarget.SetSiblingIndex(savedSiblingIndex);
            savedTarget = null;
            savedParent = null;
            savedSiblingIndex = 0;
        }

        if (layoutPlaceholder != null)
        {
            Destroy(layoutPlaceholder);
            layoutPlaceholder = null;
        }
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

    // ── Overlay: malla única sin solapamientos ─────────────────────────────────────────────

    // Rect.zero = sin agujero (overlay completo). En espacio LOCAL del HoleOverlay.
    private Rect _currentHole = Rect.zero;

    /// <summary>Convierte el target a un Rect en espacio local del HoleOverlay.</summary>
    private Rect GetHoleRectLocal(RectTransform target, Vector2 padding)
    {
        RectTransform overlayRT = (RectTransform)holeOverlay.transform;

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        // corners[0]=BL, corners[2]=TR
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayRT,
            RectTransformUtility.WorldToScreenPoint(null, corners[0]),
            null, out var bl);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayRT,
            RectTransformUtility.WorldToScreenPoint(null, corners[2]),
            null, out var tr);

        return Rect.MinMaxRect(
            Mathf.Min(bl.x, tr.x) - padding.x,
            Mathf.Min(bl.y, tr.y) - padding.y,
            Mathf.Max(bl.x, tr.x) + padding.x,
            Mathf.Max(bl.y, tr.y) + padding.y);
    }

    /// <summary>Anima el agujero del overlay. Rect.zero = overlay completo (sin agujero).</summary>
    private IEnumerator AnimateOverlayTo(Rect targetHole, float duration)
    {
        if (duration <= 0f)
        {
            _currentHole = targetHole;
            holeOverlay.SetHole(targetHole);
            yield break;
        }

        Rect startHole = _currentHole;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float eased = holeAnimCurve != null
                ? holeAnimCurve.Evaluate(Mathf.Clamp01(t / duration))
                : Mathf.Clamp01(t / duration);

            Rect hole = new Rect(
                Mathf.LerpUnclamped(startHole.x, targetHole.x, eased),
                Mathf.LerpUnclamped(startHole.y, targetHole.y, eased),
                Mathf.LerpUnclamped(startHole.width, targetHole.width, eased),
                Mathf.LerpUnclamped(startHole.height, targetHole.height, eased));

            holeOverlay.SetHole(hole);
            yield return null;
        }

        _currentHole = targetHole;
        holeOverlay.SetHole(targetHole);
    }

    private static Vector2 Lerp2(Vector2 a, Vector2 b, float t) => Vector2.LerpUnclamped(a, b, t);

    // ---------- Tooltip positioning ----------
    private void PositionTooltipNearTarget(RectTransform target, Vector2 offset)
    {
        RectTransform root = (RectTransform)tutorialCanvas.transform;

        // 1. Forzamos que el panel tenga el pivot y anchors en el centro para que PosX/PosY sean lógicos
        tooltipPanel.anchorMin = new Vector2(0.5f, 0.5f);
        tooltipPanel.anchorMax = new Vector2(0.5f, 0.5f);
        tooltipPanel.pivot = new Vector2(0.5f, 0.5f);

        // 2. Localizamos el centro del botón en el espacio del Canvas del tutorial
        Vector3 targetWorldCenter = target.TransformPoint(target.rect.center);
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, targetWorldCenter);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPos, null, out var localPoint))
        {
            // 3. ASIGNACIÓN DIRECTA: Posición del botón + tu Offset
            // Esto es lo que verás en el Inspector como Pos X y Pos Y
            tooltipPanel.anchoredPosition = localPoint + offset;
        }
    }

    private void PositionTooltipDefault()
    {
        tooltipPanel.anchoredPosition = defaultTooltipPos;
        ClampTooltipToCanvas();
    }

    private void ClampTooltipToCanvas()
    {
        RectTransform root = (RectTransform)tutorialCanvas.transform;

        // Forzamos actualización por si acaso
        Canvas.ForceUpdateCanvases();

        // Obtenemos las esquinas del panel en espacio local del Canvas
        Vector3[] corners = new Vector3[4];
        tooltipPanel.GetLocalCorners(corners);

        // Convertimos las esquinas a la posición actual en el canvas
        float width = tooltipPanel.rect.width;
        float height = tooltipPanel.rect.height;
        Vector2 pos = tooltipPanel.anchoredPosition;

        // Límites del Canvas (considerando que el pivot es 0.5, 0.5)
        float canvasW = root.rect.width;
        float canvasH = root.rect.height;

        float minX = -canvasW / 2 + (width / 2) + tooltipScreenPadding.x;
        float maxX = canvasW / 2 - (width / 2) - tooltipScreenPadding.x;
        float minY = -canvasH / 2 + (height / 2) + tooltipScreenPadding.y;
        float maxY = canvasH / 2 - (height / 2) - tooltipScreenPadding.y;

        // Aplicamos el "candado"
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        tooltipPanel.anchoredPosition = pos;
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