using UnityEngine;

[CreateAssetMenu(menuName = "Avatars/FX/ChromAberr Pulse + Random Glitch")]
public class Fx_ChromAberrPulseRandomGlitchSO : AvatarFxSO
{
    [Header("Property names (reales del shader)")]
    public string chromAberrAmountProp = "_ChromAberrAmount";
    public string glitchAmountProp = "_GlitchAmount";
    public string glitchSizeProp = "_GlitchSize";

    [Header("Chrom Aberration Timings")]
    [Tooltip("Cada X segundos se lanza el evento completo (fade in + hold + fade out).")]
    public float chromCycleSeconds = 6f;

    [Tooltip("Duración del 0->1 y 1->0")]
    public float chromTransitionSeconds = 1f;

    [Tooltip("Tiempo manteniendo ChromAberrAmount=1 antes de volver a 0")]
    public float chromHoldSeconds = 2f;

    [Header("Glitch Timings")]
    public float glitchRandomizeSeconds = 3f;

    [Header("Glitch Ranges")]
    public float glitchAmountMin = 0f;
    public float glitchAmountMax = 20f;
    public float glitchSizeMin = 0.25f;
    public float glitchSizeMax = 5f;

    public override IAvatarFxRuntime CreateRuntime()
        => new Runtime(this);

    private class Runtime : IAvatarFxRuntime
    {
        private enum ChromPhase { Waiting, FadeIn, Hold, FadeOut }

        private readonly Fx_ChromAberrPulseRandomGlitchSO cfg;
        private Material mat;

        // Chrom state
        private ChromPhase chromPhase;
        private float chromTimer;
        private float chromWaitTimer;

        // Glitch state
        private float glitchTimer;

        public Runtime(Fx_ChromAberrPulseRandomGlitchSO cfg) => this.cfg = cfg;

        public void Init(Material mat)
        {
            this.mat = mat;

            chromPhase = ChromPhase.Waiting;
            chromTimer = 0f;
            chromWaitTimer = 0f;

            glitchTimer = 0f;

            // Estado inicial
            SafeSetFloat(cfg.chromAberrAmountProp, 0f);
            ApplyRandomGlitch(); // arranca ya con valores random
        }

        public void Tick(float dtUnscaled)
        {
            if (mat == null) return;

            // 1) Glitch siempre activo, cambia cada 3s
            glitchTimer += dtUnscaled;
            if (glitchTimer >= cfg.glitchRandomizeSeconds)
            {
                glitchTimer = 0f;
                ApplyRandomGlitch();
            }

            // 2) Chrom aberration por fases
            switch (chromPhase)
            {
                case ChromPhase.Waiting:
                    {
                        SafeSetFloat(cfg.chromAberrAmountProp, 0f);

                        chromWaitTimer += dtUnscaled;
                        if (chromWaitTimer >= cfg.chromCycleSeconds)
                        {
                            chromWaitTimer = 0f;
                            chromTimer = 0f;
                            chromPhase = ChromPhase.FadeIn;
                        }
                        break;
                    }

                case ChromPhase.FadeIn:
                    {
                        chromTimer += dtUnscaled;

                        float u = (cfg.chromTransitionSeconds <= 0f)
                            ? 1f
                            : Mathf.Clamp01(chromTimer / cfg.chromTransitionSeconds);

                        float eased = Smooth01(u);
                        SafeSetFloat(cfg.chromAberrAmountProp, Mathf.Lerp(0f, 1f, eased));

                        if (u >= 1f)
                        {
                            chromTimer = 0f;
                            chromPhase = ChromPhase.Hold;
                            SafeSetFloat(cfg.chromAberrAmountProp, 1f);
                        }
                        break;
                    }

                case ChromPhase.Hold:
                    {
                        SafeSetFloat(cfg.chromAberrAmountProp, 1f);

                        chromTimer += dtUnscaled;
                        if (chromTimer >= cfg.chromHoldSeconds)
                        {
                            chromTimer = 0f;
                            chromPhase = ChromPhase.FadeOut;
                        }
                        break;
                    }

                case ChromPhase.FadeOut:
                    {
                        chromTimer += dtUnscaled;

                        float u = (cfg.chromTransitionSeconds <= 0f)
                            ? 1f
                            : Mathf.Clamp01(chromTimer / cfg.chromTransitionSeconds);

                        float eased = Smooth01(u);
                        SafeSetFloat(cfg.chromAberrAmountProp, Mathf.Lerp(1f, 0f, eased));

                        if (u >= 1f)
                        {
                            chromTimer = 0f;
                            chromPhase = ChromPhase.Waiting;
                            SafeSetFloat(cfg.chromAberrAmountProp, 0f);
                        }
                        break;
                    }
            }
        }

        private void ApplyRandomGlitch()
        {
            float gAmount = Random.Range(cfg.glitchAmountMin, cfg.glitchAmountMax);
            float gSize = Random.Range(cfg.glitchSizeMin, cfg.glitchSizeMax);

            SafeSetFloat(cfg.glitchAmountProp, gAmount);
            SafeSetFloat(cfg.glitchSizeProp, gSize);
        }

        private float Smooth01(float t) => t * t * (3f - 2f * t);

        private void SafeSetFloat(string prop, float value)
        {
            if (string.IsNullOrEmpty(prop) || mat == null) return;
            if (mat.HasProperty(prop)) mat.SetFloat(prop, value);
        }

        public void Dispose() { }
    }
}