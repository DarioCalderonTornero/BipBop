using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class TurboUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TurboController turbo;
    [SerializeField] private Image fillImage;

    [Header("Post")]
    [SerializeField] private Volume volume;

    [Header("Vignette Settings")]
    [SerializeField, Range(0f, 1f)] private float dangerIntensity = 0.35f;
    [SerializeField, Range(0.05f, 1.5f)] private float vignetteLerpTime = 0.25f;

    private Vignette vignette;
    private Coroutine vignetteRoutine;

    private void Awake()
    {
        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Vertical;
            fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
        }

        CacheVignette();

        // Estado inicial: apagado pero preparado
        if (vignette != null)
        {
            vignette.active = true;                 // lo dejamos activo para que el lerp funcione
            vignette.intensity.Override(0f);
        }
    }

    private void OnEnable()
    {
        if (turbo != null)
        {
            turbo.OnChargeChanged += Turbo_OnChargeChanged;
            turbo.OnStateChanged += Turbo_OnStateChanged;
            turbo.OnDangerStarted += Turbo_OnDangerStarted;
            turbo.OnDangerCanceled += Turbo_OnDangerCanceled;
        }

        Refresh(turbo != null ? turbo.Charge01 : 0f);
    }

    private void OnDisable()
    {
        if (turbo != null)
        {
            turbo.OnChargeChanged -= Turbo_OnChargeChanged;
            turbo.OnStateChanged -= Turbo_OnStateChanged;
            turbo.OnDangerStarted -= Turbo_OnDangerStarted;
            turbo.OnDangerCanceled -= Turbo_OnDangerCanceled;
        }

        if (vignetteRoutine != null)
        {
            StopCoroutine(vignetteRoutine);
            vignetteRoutine = null;
        }
    }

    private void Turbo_OnChargeChanged(float charge01) => Refresh(charge01);

    private void Turbo_OnStateChanged(TurboController.TurboState state) { }

    private void Turbo_OnDangerStarted() => FadeVignetteTo(dangerIntensity);
    private void Turbo_OnDangerCanceled() => FadeVignetteTo(0f);

    private void Refresh(float charge01)
    {
        if (fillImage == null) return;
        fillImage.fillAmount = Mathf.Clamp01(charge01);
    }

    private void CacheVignette()
    {
        vignette = null;

        if (volume == null || volume.profile == null)
        {
            Debug.LogWarning("TurboUI: Volume o Profile no asignado.");
            return;
        }

        if (!volume.profile.TryGet(out vignette))
        {
            Debug.LogWarning("TurboUI: No hay Vignette en el Volume Profile.");
        }
    }

    private void FadeVignetteTo(float target)
    {
        if (vignette == null) return;

        // asegúrate de que está activo para que se vea el lerp
        vignette.active = true;

        if (vignetteRoutine != null)
            StopCoroutine(vignetteRoutine);

        vignetteRoutine = StartCoroutine(FadeVignetteRoutine(target));
    }

    private IEnumerator FadeVignetteRoutine(float target)
    {
        float start = vignette.intensity.value;
        float t = 0f;

        // Evita división por 0
        float duration = Mathf.Max(0.01f, vignetteLerpTime);

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration; // unscaled por si hay pausa/slowmo
            float v = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
            vignette.intensity.Override(v);
            yield return null;
        }

        vignette.intensity.Override(target);

        // Opcional: si quieres que quede "apagado de verdad"
        // (solo cuando target es 0)
        // vignette.active = target > 0.0001f;

        vignetteRoutine = null;
    }
}
