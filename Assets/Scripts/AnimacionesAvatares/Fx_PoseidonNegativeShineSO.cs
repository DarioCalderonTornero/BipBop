using UnityEngine;

[CreateAssetMenu(menuName = "Avatars/FX/Poseidon Negative + Shine + Base ContrastWarp")]
public class Fx_PoseidonNegativeShineSO : AvatarFxSO
{
    [Header("Keywords (shader_feature / multi_compile)")]
    public string negativeKeyword = "NEGATIVE_ON";
    public string shineKeyword = "SHINE_ON";
    public string contrastKeyword = "CONTRAST_ON";
    public string warpKeyword = "WARP_ON";

    [Header("Negative")]
    public string negativeAmountProp = "_NegativeAmount";
    public float negativeWaitSeconds = 4f;
    public float negativeRiseSeconds = 0.5f;
    public float negativeHoldSeconds = 3f;
    public float negativeFallSeconds = 0.5f;

    [Header("Shine")]
    public string shineColorProp = "_ShineColor";
    public string shineLocationProp = "_ShineLocation";
    public string shineRotateProp = "_ShineRotate";
    public string shineWidthProp = "_ShineWidth";
    public string shineGlowProp = "_ShineGlow";

    public float shineLoopSeconds = 0.5f;
    [Range(0f, 1f)] public float shineWidth = 0.25f;
    [Range(0f, 5f)] public float shineGlow = 0.25f;
    public float shineRotate = 1.5f;
    public Color shineColor = new Color32(0, 2, 255, 255); // 0002FF

    [Header("Contrast / Brightness (always on)")]
    public string contrastProp = "_Contrast";
    public string brightnessProp = "_Brightness";
    public float contrast = 1.25f;
    public float brightness = 0f;

    [Header("Warp Distortion (always on)")]
    public string warpStrengthProp = "_WarpStrength";
    public string warpSpeedProp = "_WarpSpeed";
    public string warpScaleProp = "_WarpScale";
    public float warpStrength = 0.002f;
    public float warpSpeed = 4f;
    public float warpScale = 0.5f;

    public override IAvatarFxRuntime CreateRuntime()
        => new Runtime(this);

    private class Runtime : IAvatarFxRuntime
    {
        private readonly Fx_PoseidonNegativeShineSO cfg;
        private Material mat;

        private enum NegativePhase
        {
            Waiting,
            Rising,
            Holding,
            Falling
        }

        private NegativePhase negativePhase;
        private float negativeTimer;
        private float shineTimer;

        public Runtime(Fx_PoseidonNegativeShineSO cfg)
        {
            this.cfg = cfg;
        }

        public void Init(Material mat)
        {
            this.mat = mat;

            negativePhase = NegativePhase.Waiting;
            negativeTimer = 0f;
            shineTimer = 0f;

            EnableKeyword(cfg.negativeKeyword);
            EnableKeyword(cfg.shineKeyword);
            EnableKeyword(cfg.contrastKeyword);
            EnableKeyword(cfg.warpKeyword);

            ApplyStaticBaseValues();
            ApplyNegative(0f);
            ApplyShineLocation(0f);
        }

        public void Tick(float dtUnscaled)
        {
            if (mat == null) return;

            TickNegative(dtUnscaled);
            TickShine(dtUnscaled);
            ApplyStaticBaseValues();
        }

        public void Dispose()
        {
        }

        private void TickNegative(float dt)
        {
            negativeTimer += dt;

            switch (negativePhase)
            {
                case NegativePhase.Waiting:
                    {
                        ApplyNegative(0f);

                        if (negativeTimer >= cfg.negativeWaitSeconds)
                        {
                            negativeTimer = 0f;
                            negativePhase = NegativePhase.Rising;
                        }

                        break;
                    }

                case NegativePhase.Rising:
                    {
                        float u = (cfg.negativeRiseSeconds <= 0f)
                            ? 1f
                            : Mathf.Clamp01(negativeTimer / cfg.negativeRiseSeconds);

                        ApplyNegative(u);

                        if (negativeTimer >= cfg.negativeRiseSeconds)
                        {
                            negativeTimer = 0f;
                            negativePhase = NegativePhase.Holding;
                            ApplyNegative(1f);
                        }

                        break;
                    }

                case NegativePhase.Holding:
                    {
                        ApplyNegative(1f);

                        if (negativeTimer >= cfg.negativeHoldSeconds)
                        {
                            negativeTimer = 0f;
                            negativePhase = NegativePhase.Falling;
                        }

                        break;
                    }

                case NegativePhase.Falling:
                    {
                        float u = (cfg.negativeFallSeconds <= 0f)
                            ? 1f
                            : Mathf.Clamp01(negativeTimer / cfg.negativeFallSeconds);

                        ApplyNegative(1f - u);

                        if (negativeTimer >= cfg.negativeFallSeconds)
                        {
                            negativeTimer = 0f;
                            negativePhase = NegativePhase.Waiting;
                            ApplyNegative(0f);
                        }

                        break;
                    }
            }
        }

        private void TickShine(float dt)
        {
            if (cfg.shineLoopSeconds <= 0f)
            {
                ApplyShineLocation(1f);
                return;
            }

            shineTimer += dt;

            while (shineTimer >= cfg.shineLoopSeconds)
                shineTimer -= cfg.shineLoopSeconds;

            float location = shineTimer / cfg.shineLoopSeconds;
            ApplyShineLocation(location);
        }

        private void ApplyStaticBaseValues()
        {
            SafeSetColor(cfg.shineColorProp, cfg.shineColor);
            SafeSetFloat(cfg.shineWidthProp, cfg.shineWidth);
            SafeSetFloat(cfg.shineGlowProp, cfg.shineGlow);
            SafeSetFloat(cfg.shineRotateProp, cfg.shineRotate);

            SafeSetFloat(cfg.contrastProp, cfg.contrast);
            SafeSetFloat(cfg.brightnessProp, cfg.brightness);

            SafeSetFloat(cfg.warpStrengthProp, cfg.warpStrength);
            SafeSetFloat(cfg.warpSpeedProp, cfg.warpSpeed);
            SafeSetFloat(cfg.warpScaleProp, cfg.warpScale);
        }

        private void ApplyNegative(float amount)
        {
            SafeSetFloat(cfg.negativeAmountProp, amount);
        }

        private void ApplyShineLocation(float location)
        {
            SafeSetFloat(cfg.shineLocationProp, location);
        }

        private void EnableKeyword(string keyword)
        {
            if (mat == null || string.IsNullOrEmpty(keyword)) return;
            mat.EnableKeyword(keyword);
        }

        private void SafeSetFloat(string prop, float value)
        {
            if (mat == null || string.IsNullOrEmpty(prop)) return;
            if (mat.HasProperty(prop)) mat.SetFloat(prop, value);
        }

        private void SafeSetColor(string prop, Color value)
        {
            if (mat == null || string.IsNullOrEmpty(prop)) return;
            if (mat.HasProperty(prop)) mat.SetColor(prop, value);
        }
    }
}