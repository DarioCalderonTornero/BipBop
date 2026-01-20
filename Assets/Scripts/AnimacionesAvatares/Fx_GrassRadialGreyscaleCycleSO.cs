using UnityEngine;

[CreateAssetMenu(menuName = "Avatars/FX/GrassRadial + Greyscale Cycle")]
public class Fx_GrassRadialGreyscaleCycleSO : AvatarFxSO
{
    [Header("Property names (reales del shader)")]
    public string grassRadialBlendProp = "_GrassRadialBend";
    public string greyscaleBlendProp = "_GreyscaleBlend";

    [Header("Values")]
    [Range(0f, 1f)] public float grassMin = 0f;
    [Range(0f, 1f)] public float grassMax = 1f;
    [Range(0f, 1f)] public float greyMin = 0f;
    [Range(0f, 1f)] public float greyMax = 1f;

    [Header("Timings (seconds)")]
    public float startNormalSeconds = 2f; // 2s normal
    public float grassUp1Seconds = 1f; // grass 0->1
    public float greyUpSeconds = 1f; // grey 0->1 (con grass=1)
    public float grassDown1Seconds = 1f; // grass 1->0 (grey se queda en 1)

    public float betweenCyclesSeconds = 5f; // 5s espera (grass=0, grey=1)

    public float grassUp2Seconds = 1f; // grass 0->1
    public float greyDownSeconds = 1f; // grey 1->0 (con grass=1)
    public float grassDown2Seconds = 1f; // grass 1->0

    [Header("Easing")]
    public bool smooth = true;

    public override IAvatarFxRuntime CreateRuntime() => new Runtime(this);

    private class Runtime : IAvatarFxRuntime
    {
        private readonly Fx_GrassRadialGreyscaleCycleSO cfg;
        private Material mat;

        private enum Phase
        {
            WaitStart,
            GrassUp1,
            GreyUp,
            GrassDown1,
            WaitBetween,
            GrassUp2,
            GreyDown,
            GrassDown2
        }

        private Phase phase;
        private float timer;
        private float phaseDuration;

        private float grassCurrent;
        private float greyCurrent;

        private bool animGrass;
        private bool animGrey;

        private float grassFrom, grassTo;
        private float greyFrom, greyTo;

        public Runtime(Fx_GrassRadialGreyscaleCycleSO cfg) => this.cfg = cfg;

        public void Init(Material mat)
        {
            this.mat = mat;

            grassCurrent = cfg.grassMin;
            greyCurrent = cfg.greyMin;

            ApplyValues(grassCurrent, greyCurrent);
            Enter(Phase.WaitStart);
        }

        public void Tick(float dtUnscaled)
        {
            if (mat == null) return;

            timer += dtUnscaled;

            float u = (phaseDuration <= 0f) ? 1f : Mathf.Clamp01(timer / phaseDuration);
            float t = cfg.smooth ? Smooth01(u) : u;

            float grass = grassCurrent;
            float grey = greyCurrent;

            if (animGrass) grass = Mathf.Lerp(grassFrom, grassTo, t);
            if (animGrey) grey = Mathf.Lerp(greyFrom, greyTo, t);

            ApplyValues(grass, grey);

            if (timer >= phaseDuration)
            {
                // Fijar valores finales exactos
                if (animGrass) grassCurrent = grassTo;
                if (animGrey) greyCurrent = greyTo;

                ApplyValues(grassCurrent, greyCurrent);

                // Siguiente fase
                switch (phase)
                {
                    case Phase.WaitStart: Enter(Phase.GrassUp1); break;
                    case Phase.GrassUp1: Enter(Phase.GreyUp); break;
                    case Phase.GreyUp: Enter(Phase.GrassDown1); break;
                    case Phase.GrassDown1: Enter(Phase.WaitBetween); break;
                    case Phase.WaitBetween: Enter(Phase.GrassUp2); break;
                    case Phase.GrassUp2: Enter(Phase.GreyDown); break;
                    case Phase.GreyDown: Enter(Phase.GrassDown2); break;
                    case Phase.GrassDown2: Enter(Phase.WaitStart); break;
                }
            }
        }

        private void Enter(Phase next)
        {
            phase = next;
            timer = 0f;

            animGrass = false;
            animGrey = false;

            grassFrom = grassTo = grassCurrent;
            greyFrom = greyTo = greyCurrent;

            switch (next)
            {
                case Phase.WaitStart:
                    phaseDuration = Mathf.Max(0f, cfg.startNormalSeconds);
                    break;

                case Phase.GrassUp1:
                    animGrass = true;
                    grassFrom = grassCurrent;
                    grassTo = cfg.grassMax;
                    phaseDuration = Mathf.Max(0f, cfg.grassUp1Seconds);
                    break;

                case Phase.GreyUp:
                    animGrey = true;
                    greyFrom = greyCurrent;
                    greyTo = cfg.greyMax;
                    phaseDuration = Mathf.Max(0f, cfg.greyUpSeconds);
                    break;

                case Phase.GrassDown1:
                    animGrass = true;
                    grassFrom = grassCurrent;
                    grassTo = cfg.grassMin;
                    phaseDuration = Mathf.Max(0f, cfg.grassDown1Seconds);
                    break;

                case Phase.WaitBetween:
                    phaseDuration = Mathf.Max(0f, cfg.betweenCyclesSeconds);
                    break;

                case Phase.GrassUp2:
                    animGrass = true;
                    grassFrom = grassCurrent;
                    grassTo = cfg.grassMax;
                    phaseDuration = Mathf.Max(0f, cfg.grassUp2Seconds);
                    break;

                case Phase.GreyDown:
                    animGrey = true;
                    greyFrom = greyCurrent;
                    greyTo = cfg.greyMin;
                    phaseDuration = Mathf.Max(0f, cfg.greyDownSeconds);
                    break;

                case Phase.GrassDown2:
                    animGrass = true;
                    grassFrom = grassCurrent;
                    grassTo = cfg.grassMin;
                    phaseDuration = Mathf.Max(0f, cfg.grassDown2Seconds);
                    break;

                default:
                    phaseDuration = 0f;
                    break;
            }
        }

        private void ApplyValues(float grass, float grey)
        {
            SafeSetFloat(cfg.grassRadialBlendProp, grass);
            SafeSetFloat(cfg.greyscaleBlendProp, grey);
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
