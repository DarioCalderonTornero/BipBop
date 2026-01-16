using UnityEngine;

[CreateAssetMenu(menuName = "Avatars/FX/ChangeColor + Distortion Pulse")]
public class Fx_ChangeColorDistortionPulseSO : AvatarFxSO
{
    [Header("Property names (reales del shader)")]
    public string change1ToleranceProp = "_Change1Tolerance";
    public string change1NewColorProp = "_Change1NewColor";
    public string distortionAmountProp = "_DistortionAmount";

    [Header("Timings")]
    public float normalSeconds = 4f;
    public float pulseSeconds = 1f;

    [Header("Values")]
    public float normalTolerance = 1f;
    public float pulseTolerance = 0f;
    public float pulseDistortionAmount = 0.85f;

    [Header("Random color (HSV) - bright")]
    [Range(0f, 1f)] public float hueMin = 0f;
    [Range(0f, 1f)] public float hueMax = 1f;
    [Range(0f, 1f)] public float satMin = 0.75f;
    [Range(0f, 1f)] public float satMax = 1.00f;
    [Range(0f, 1f)] public float valMin = 0.80f;
    [Range(0f, 1f)] public float valMax = 1.00f;

    public override IAvatarFxRuntime CreateRuntime()
        => new Runtime(this);

    private class Runtime : IAvatarFxRuntime
    {
        private readonly Fx_ChangeColorDistortionPulseSO cfg;
        private Material mat;

        // Fase
        private bool inPulse;
        private float timer;

        // Estado estable actual: true => tolerance=1 (original), false => tolerance=0 (cambiado)
        private bool stableIsOriginal = true;

        // Para el pulso actual
        private float pulseFromTol;
        private float pulseToTol;

        public Runtime(Fx_ChangeColorDistortionPulseSO cfg) => this.cfg = cfg;

        public void Init(Material mat)
        {
            this.mat = mat;
            timer = 0f;
            inPulse = false;

            stableIsOriginal = true; // empieza en original
            ApplyStableState();
        }

        public void Tick(float dtUnscaled)
        {
            if (mat == null) return;

            timer += dtUnscaled;

            if (!inPulse)
            {
                // Mantener el estado estable actual durante normalSeconds
                ApplyStableState();

                if (timer >= cfg.normalSeconds)
                {
                    // arrancar pulso
                    timer = 0f;
                    inPulse = true;

                    pulseFromTol = stableIsOriginal ? cfg.normalTolerance : cfg.pulseTolerance;
                    pulseToTol = stableIsOriginal ? cfg.pulseTolerance : cfg.normalTolerance;

                    // Si vamos a entrar al estado "cambiado" (tolerance 0), elegimos un color nuevo
                    if (pulseToTol == cfg.pulseTolerance)
                    {
                        float h = Random.Range(cfg.hueMin, cfg.hueMax);
                        float s = Random.Range(cfg.satMin, cfg.satMax);
                        float v = Random.Range(cfg.valMin, cfg.valMax);

                        Color c = Color.HSVToRGB(h, s, v);
                        c.a = 1f;

                        SafeSetColor(cfg.change1NewColorProp, c);
                    }
                }
            }
            else
            {
                float u = (cfg.pulseSeconds <= 0f) ? 1f : Mathf.Clamp01(timer / cfg.pulseSeconds);
                float eased = Smooth01(u);

                // ✅ Tolerance suave desde el estado actual al siguiente
                float tol = Mathf.Lerp(pulseFromTol, pulseToTol, eased);
                SafeSetFloat(cfg.change1ToleranceProp, tol);

                // ✅ Distorsión campana (0 -> pico -> 0)
                float bell = Mathf.Sin(Mathf.PI * u);
                float dist = bell * cfg.pulseDistortionAmount;
                SafeSetFloat(cfg.distortionAmountProp, dist);

                if (timer >= cfg.pulseSeconds)
                {
                    // terminar pulso: fijar el nuevo estado estable
                    timer = 0f;
                    inPulse = false;

                    stableIsOriginal = !stableIsOriginal; // alterna 1 <-> 0

                    ApplyStableState(); // asegura valores exactos
                }
            }
        }

        private void ApplyStableState()
        {
            float tol = stableIsOriginal ? cfg.normalTolerance : cfg.pulseTolerance;

            SafeSetFloat(cfg.change1ToleranceProp, tol);
            SafeSetFloat(cfg.distortionAmountProp, 0f);
        }

        private float Smooth01(float t) => t * t * (3f - 2f * t);

        private void SafeSetFloat(string prop, float value)
        {
            if (string.IsNullOrEmpty(prop) || mat == null) return;
            if (mat.HasProperty(prop)) mat.SetFloat(prop, value);
        }

        private void SafeSetColor(string prop, Color value)
        {
            if (string.IsNullOrEmpty(prop) || mat == null) return;
            if (mat.HasProperty(prop)) mat.SetColor(prop, value);
        }

        public void Dispose() { }
    }

}
