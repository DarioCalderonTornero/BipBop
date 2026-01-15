using System.Collections;
using UnityEngine;
using static LogicaJuego;

public class ClassicModeUIEffects : MonoBehaviour
{
    public static ClassicModeUIEffects Instance { get; private set; }

    [Header("Targets de UI a animar (sin Empty)")]
    [Tooltip("Arrastra aquí los RectTransforms que quieres animar a la vez (texto, icono, fondo, etc.).")]
    [SerializeField] private RectTransform[] uiTargets;

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

    [Header("Parámetros de tap")]
    [SerializeField] private float tapScale = 1.1f;
    [SerializeField] private float tapDuration = 0.06f;

    private Vector2[] originalAnchoredPos;
    private Vector3[] originalScale;
    private Quaternion[] originalRot;

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
        // Si no has asignado targets, usa el RectTransform del objeto (fallback)
        if (uiTargets == null || uiTargets.Length == 0)
        {
            var rt = GetComponent<RectTransform>();
            if (rt != null) uiTargets = new[] { rt };
        }

        CacheOriginals();
    }

    private void CacheOriginals()
    {
        if (uiTargets == null) return;

        originalAnchoredPos = new Vector2[uiTargets.Length];
        originalScale = new Vector3[uiTargets.Length];
        originalRot = new Quaternion[uiTargets.Length];

        for (int i = 0; i < uiTargets.Length; i++)
        {
            if (uiTargets[i] == null) continue;

            originalAnchoredPos[i] = uiTargets[i].anchoredPosition;
            originalScale[i] = uiTargets[i].localScale;
            originalRot[i] = uiTargets[i].localRotation;
        }
    }

    public void PlayEffectForTask(TaskType taskType)
    {
        if (uiTargets == null || uiTargets.Length == 0) return;

        // Si hay una animación en marcha, la paramos y reseteamos
        if (currentEffect != null)
        {
            StopCoroutine(currentEffect);
            ResetTransforms();
        }

        switch (taskType)
        {
            case TaskType.SwipeRight: currentEffect = StartCoroutine(DoMoveEffect(Vector2.right)); break;
            case TaskType.SwipeLeft: currentEffect = StartCoroutine(DoMoveEffect(Vector2.left)); break;
            case TaskType.SwipeUp: currentEffect = StartCoroutine(DoMoveEffect(Vector2.up)); break;
            case TaskType.SwipeDown: currentEffect = StartCoroutine(DoMoveEffect(Vector2.down)); break;

            case TaskType.RotateRight: currentEffect = StartCoroutine(DoRotateEffect(-rotateAngle)); break; // horario
            case TaskType.RotateLeft: currentEffect = StartCoroutine(DoRotateEffect(rotateAngle)); break; // antihorario

            case TaskType.Shake: currentEffect = StartCoroutine(DoShakeEffect()); break;

            case TaskType.ZoomIn: currentEffect = StartCoroutine(DoZoomEffect(zoomFactorIn)); break;
            case TaskType.ZoomOut: currentEffect = StartCoroutine(DoZoomEffect(zoomFactorOut)); break;

            case TaskType.LookDown:
                currentEffect = StartCoroutine(DoMoveEffect(Vector2.down));
                break;

            case TaskType.Tap:
            default:
                currentEffect = StartCoroutine(DoTapEffect());
                break;
        }
    }

    private void ResetTransforms()
    {
        for (int i = 0; i < uiTargets.Length; i++)
        {
            var rt = uiTargets[i];
            if (rt == null) continue;

            rt.anchoredPosition = originalAnchoredPos[i];
            rt.localScale = originalScale[i];
            rt.localRotation = originalRot[i];
        }
    }

    private IEnumerator DoMoveEffect(Vector2 dir)
    {
        Vector2 offset = dir * moveDistance;

        // Ida
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / moveDuration;
            for (int i = 0; i < uiTargets.Length; i++)
            {
                var rt = uiTargets[i];
                if (rt == null) continue;

                rt.anchoredPosition = Vector2.Lerp(originalAnchoredPos[i], originalAnchoredPos[i] + offset, t);
            }
            yield return null;
        }

        // Vuelta
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / moveDuration;
            for (int i = 0; i < uiTargets.Length; i++)
            {
                var rt = uiTargets[i];
                if (rt == null) continue;

                rt.anchoredPosition = Vector2.Lerp(originalAnchoredPos[i] + offset, originalAnchoredPos[i], t);
            }
            yield return null;
        }

        ResetTransforms();
        currentEffect = null;
    }

    private IEnumerator DoRotateEffect(float angle)
    {
        // Ida
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / rotateDuration;
            for (int i = 0; i < uiTargets.Length; i++)
            {
                var rt = uiTargets[i];
                if (rt == null) continue;

                Quaternion targetRot = Quaternion.Euler(0, 0, angle) * originalRot[i];
                rt.localRotation = Quaternion.Slerp(originalRot[i], targetRot, t);
            }
            yield return null;
        }

        // Vuelta
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / rotateDuration;
            for (int i = 0; i < uiTargets.Length; i++)
            {
                var rt = uiTargets[i];
                if (rt == null) continue;

                Quaternion targetRot = Quaternion.Euler(0, 0, angle) * originalRot[i];
                rt.localRotation = Quaternion.Slerp(targetRot, originalRot[i], t);
            }
            yield return null;
        }

        ResetTransforms();
        currentEffect = null;
    }

    private IEnumerator DoZoomEffect(float targetScaleFactor)
    {
        // Ida
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / zoomDuration;
            for (int i = 0; i < uiTargets.Length; i++)
            {
                var rt = uiTargets[i];
                if (rt == null) continue;

                Vector3 targetScale = originalScale[i] * targetScaleFactor;
                rt.localScale = Vector3.Lerp(originalScale[i], targetScale, t);
            }
            yield return null;
        }

        // Vuelta
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / zoomDuration;
            for (int i = 0; i < uiTargets.Length; i++)
            {
                var rt = uiTargets[i];
                if (rt == null) continue;

                Vector3 targetScale = originalScale[i] * targetScaleFactor;
                rt.localScale = Vector3.Lerp(targetScale, originalScale[i], t);
            }
            yield return null;
        }

        ResetTransforms();
        currentEffect = null;
    }

    private IEnumerator DoShakeEffect()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            Vector2 randomOffset = Random.insideUnitCircle * shakeMagnitude;

            for (int i = 0; i < uiTargets.Length; i++)
            {
                var rt = uiTargets[i];
                if (rt == null) continue;

                rt.anchoredPosition = originalAnchoredPos[i] + randomOffset;
            }

            yield return null;
        }

        ResetTransforms();
        currentEffect = null;
    }

    private IEnumerator DoTapEffect()
    {
        float half = tapDuration;

        // Subir
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / half;
            for (int i = 0; i < uiTargets.Length; i++)
            {
                var rt = uiTargets[i];
                if (rt == null) continue;

                Vector3 big = originalScale[i] * tapScale;
                rt.localScale = Vector3.Lerp(originalScale[i], big, t);
            }
            yield return null;
        }

        // Bajar
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / half;
            for (int i = 0; i < uiTargets.Length; i++)
            {
                var rt = uiTargets[i];
                if (rt == null) continue;

                Vector3 big = originalScale[i] * tapScale;
                rt.localScale = Vector3.Lerp(big, originalScale[i], t);
            }
            yield return null;
        }

        ResetTransforms();
        currentEffect = null;
    }
}
