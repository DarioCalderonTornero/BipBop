using System.Collections;
using UnityEngine;
using static LogicaJuego;

public class ClassicModeUIEffects : MonoBehaviour
{
    public static ClassicModeUIEffects Instance { get; private set; }

    [Header("Root de UI a animar")]
    [SerializeField] private RectTransform uiRoot;

    [Header("Parámetros de movimiento")]
    [SerializeField] private float moveDistance = 60f;
    [SerializeField] private float moveDuration = 0.08f;

    [Header("Parámetros de rotación")]
    [SerializeField] private float rotateAngle = 15f;
    [SerializeField] private float rotateDuration = 0.08f;

    [Header("Parámetros de zoom")]
    [SerializeField] private float zoomFactorIn = 1.15f;
    [SerializeField] private float zoomFactorOut = 0.85f;
    [SerializeField] private float zoomDuration = 0.08f;

    [Header("Parámetros de shake")]
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeMagnitude = 25f;

    // Para el efecto de TAP (un pequeño "bump")
    [Header("Parámetros de tap")]
    [SerializeField] private float tapScale = 1.1f;
    [SerializeField] private float tapDuration = 0.06f;

    private Vector3 originalPos;
    private Vector3 originalScale;
    private Quaternion originalRot;

    private Coroutine currentEffect;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (uiRoot == null)
            uiRoot = GetComponent<RectTransform>();

        originalPos = uiRoot.localPosition;
        originalScale = uiRoot.localScale;
        originalRot = uiRoot.localRotation;
    }

    public void PlayEffectForTask(TaskType taskType)
    {
        if (uiRoot == null) return;

        // Si hay una animación en marcha, la paramos y reseteamos
        if (currentEffect != null)
        {
            StopCoroutine(currentEffect);
            ResetTransform();
        }

        switch (taskType)
        {
            case TaskType.SwipeRight:
                currentEffect = StartCoroutine(DoMoveEffect(Vector2.right));
                break;
            case TaskType.SwipeLeft:
                currentEffect = StartCoroutine(DoMoveEffect(Vector2.left));
                break;
            case TaskType.SwipeUp:
                currentEffect = StartCoroutine(DoMoveEffect(Vector2.up));
                break;
            case TaskType.SwipeDown:
                currentEffect = StartCoroutine(DoMoveEffect(Vector2.down));
                break;

            case TaskType.RotateRight:
                currentEffect = StartCoroutine(DoRotateEffect(-rotateAngle)); // horario
                break;
            case TaskType.RotateLeft:
                currentEffect = StartCoroutine(DoRotateEffect(rotateAngle));  // antihorario
                break;

            case TaskType.Shake:
                currentEffect = StartCoroutine(DoShakeEffect());
                break;

            case TaskType.ZoomIn:
                currentEffect = StartCoroutine(DoZoomEffect(zoomFactorIn));
                break;
            case TaskType.ZoomOut:
                currentEffect = StartCoroutine(DoZoomEffect(zoomFactorOut));
                break;

            case TaskType.LookDown:
                // pequeño golpe hacia abajo y vuelta
                currentEffect = StartCoroutine(DoMoveEffect(Vector2.down));
                break;

            case TaskType.Tap:
            default:
                // Para TAP: un pequeño “bump” de escala
                currentEffect = StartCoroutine(DoTapEffect());
                break;
        }
    }

    private void ResetTransform()
    {
        uiRoot.localPosition = originalPos;
        uiRoot.localScale = originalScale;
        uiRoot.localRotation = originalRot;
    }

    private IEnumerator DoMoveEffect(Vector2 dir)
    {
        Vector3 offset = new Vector3(dir.x, dir.y, 0f) * moveDistance;

        // Ida
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / moveDuration;
            uiRoot.localPosition = Vector3.Lerp(originalPos, originalPos + offset, t);
            yield return null;
        }

        // Vuelta
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / moveDuration;
            uiRoot.localPosition = Vector3.Lerp(originalPos + offset, originalPos, t);
            yield return null;
        }

        ResetTransform();
        currentEffect = null;
    }

    private IEnumerator DoRotateEffect(float angle)
    {
        Quaternion targetRot = Quaternion.Euler(0, 0, angle) * originalRot;

        // Ida
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / rotateDuration;
            uiRoot.localRotation = Quaternion.Slerp(originalRot, targetRot, t);
            yield return null;
        }

        // Vuelta
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / rotateDuration;
            uiRoot.localRotation = Quaternion.Slerp(targetRot, originalRot, t);
            yield return null;
        }

        ResetTransform();
        currentEffect = null;
    }

    private IEnumerator DoZoomEffect(float targetScaleFactor)
    {
        Vector3 targetScale = originalScale * targetScaleFactor;

        // Ida
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / zoomDuration;
            uiRoot.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        // Vuelta
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / zoomDuration;
            uiRoot.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        ResetTransform();
        currentEffect = null;
    }

    private IEnumerator DoShakeEffect()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            Vector2 randomOffset = Random.insideUnitCircle * shakeMagnitude;
            uiRoot.localPosition = originalPos + new Vector3(randomOffset.x, randomOffset.y, 0f);
            yield return null;
        }

        ResetTransform();
        currentEffect = null;
    }

    private IEnumerator DoTapEffect()
    {
        Vector3 big = originalScale * tapScale;
        float half = tapDuration;

        // Subir
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / half;
            uiRoot.localScale = Vector3.Lerp(originalScale, big, t);
            yield return null;
        }

        // Bajar
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / half;
            uiRoot.localScale = Vector3.Lerp(big, originalScale, t);
            yield return null;
        }

        ResetTransform();
        currentEffect = null;
    }
}
