using UnityEngine;
using UnityEngine.UI;

public class TurboUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TurboController turbo;
    [SerializeField] private Image fillImage; // el Image con Type=Filled Vertical

    [Header("Color Thresholds (0..1)")]
    [Range(0f, 1f)][SerializeField] private float orangeAt = 0.5f;
    [Range(0f, 1f)][SerializeField] private float redAt = 0.85f;

    [Header("Colors")]
    [SerializeField] private Color green = new Color(0.2f, 1f, 0.2f, 1f);
    [SerializeField] private Color orange = new Color(1f, 0.6f, 0.1f, 1f);
    [SerializeField] private Color red = new Color(1f, 0.2f, 0.2f, 1f);

    private void Awake()
    {
        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Vertical;
            fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
        }
    }

    private void OnEnable()
    {
        if (turbo != null)
        {
            turbo.OnChargeChanged += Turbo_OnChargeChanged;
            turbo.OnStateChanged += Turbo_OnStateChanged;
        }

        // pintar estado inicial
        Refresh(turbo != null ? turbo.Charge01 : 0f);
    }

    private void OnDisable()
    {
        if (turbo != null)
        {
            turbo.OnChargeChanged -= Turbo_OnChargeChanged;
            turbo.OnStateChanged -= Turbo_OnStateChanged;
        }
    }

    private void Turbo_OnChargeChanged(float charge01)
    {
        Refresh(charge01);
    }

    private void Turbo_OnStateChanged(TurboController.TurboState state)
    {
        // Por si quieres luego efectos (parpadeo en rojo, etc.)
        // De momento no hace falta.
    }

    private void Refresh(float charge01)
    {
        if (fillImage == null) return;

        charge01 = Mathf.Clamp01(charge01);
        fillImage.fillAmount = charge01;
    }
}
