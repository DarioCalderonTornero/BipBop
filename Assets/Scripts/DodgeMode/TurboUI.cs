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

    private Vignette vignette;

    private void Awake()
    {
        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Vertical;
            fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
        }

        CacheVignette();
        SetVignette(false);
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

    private void Turbo_OnDangerCanceled()
    {
        SetVignette(false);
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
    }

    private void Turbo_OnChargeChanged(float charge01) => Refresh(charge01);

    private void Turbo_OnStateChanged(TurboController.TurboState state)
    {
        
    }

    private void Turbo_OnDangerStarted()
    {
        SetVignette(true);
    }


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

        // Busca el override de Vignette dentro del profile
        if (!volume.profile.TryGet(out vignette))
        {
            Debug.LogWarning("TurboUI: No hay Vignette en el Volume Profile.");
        }
    }

    private void SetVignette(bool enabled)
    {
        if (vignette == null) return;

        vignette.active = enabled;

        // Recomendado: también setear intensidad para asegurar el efecto
        if (enabled)
            vignette.intensity.Override(dangerIntensity);
        else
            vignette.intensity.Override(0f);
    }
}
