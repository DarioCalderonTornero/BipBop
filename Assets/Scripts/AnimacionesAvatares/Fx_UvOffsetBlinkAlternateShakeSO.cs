using UnityEngine;

[CreateAssetMenu(menuName = "Avatars/FX/UV Offset + Alternate Shake (Waits)")]
public class Fx_UvOffsetAlternateShakeWaitSO : AvatarFxSO
{
    [Header("Property names (reales del shader)")]
    public string offsetUvXProp = "_OffsetUvX";
    public string offsetUvYProp = "_OffsetUvY";
    public string shakeUvSpeedProp = "_ShakeUvSpeed";

    [Header("Timings")]
    public float startDelaySeconds = 3f;      // SOLO al inicio
    public float offsetOutSeconds = 1f;       // 0 -> +/-mag
    public float offsetBackSeconds = 1f;      // (flip instantáneo) -> 0
    public float waitWithShakeOnSeconds = 1f; // tras llegar a 0 (primer offset) activa shake y espera
    public float waitWithShakeOffSeconds = 1f;// tras llegar a 0 (offset con shake) apaga shake y espera

    [Header("Optional shake fade out")]
    public float shakeFadeOutSeconds = 0f;    // 0 = apagar instantáneo; >0 = bajar a 0 progresivo

    [Header("Values")]
    public float offsetMagnitude = 1f;        // usa +/-
    public float shakeOnSpeed = 6f;
    public float shakeOffSpeed = 0f;

    [Header("Random")]
    public bool randomAxis = true;            // X o Y
    public bool randomSign = true;            // + o -
    public enum FixedAxis { X, Y }
    public FixedAxis fixedAxis = FixedAxis.X;
    public int fixedSign = 1;                 // 1 o -1

    [Header("Easing")]
    public bool smooth = true;

    public override IAvatarFxRuntime CreateRuntime() => new Runtime(this);

    private class Runtime : IAvatarFxRuntime
    {
        private readonly Fx_UvOffsetAlternateShakeWaitSO cfg;
        private Material mat;

        private enum Phase
        {
            StartWait,
            OffsetOut,
            OffsetBack,
            WaitShakeOn,
            WaitShakeOff,
            ShakeFadeOut
        }

        private Phase phase;
        private float timer;
        private float duration;

        private bool shakeOn;

        // Offset actual (siempre 0 en esperas)
        private bool useX;
        private float target;      // +mag o -mag
        private float offsetFrom;
        private float offsetTo;

        // Shake fade
        private float shakeFrom;
        private float shakeTo;

        public Runtime(Fx_UvOffsetAlternateShakeWaitSO cfg) => this.cfg = cfg;

        public void Init(Material mat)
        {
            this.mat = mat;

            shakeOn = false;
            ApplyAll(0f, 0f, cfg.shakeOffSpeed);

            Enter(Phase.StartWait);
        }

        public void Tick(float dtUnscaled)
        {
            if (mat == null) return;

            timer += dtUnscaled;

            float u = (duration <= 0f) ? 1f : Mathf.Clamp01(timer / duration);
            float t = cfg.smooth ? Smooth01(u) : u;

            float x = 0f, y = 0f;

            // Offset anim
            if (phase == Phase.OffsetOut || phase == Phase.OffsetBack)
            {
                float v = Mathf.Lerp(offsetFrom, offsetTo, t);
                if (useX) { x = v; y = 0f; }
                else { y = v; x = 0f; }
            }

            // Shake value
            float shake = shakeOn ? cfg.shakeOnSpeed : cfg.shakeOffSpeed;
            if (phase == Phase.ShakeFadeOut)
                shake = Mathf.Lerp(shakeFrom, shakeTo, t);

            ApplyAll(x, y, shake);

            if (timer >= duration)
            {
                // Fin de fase -> valores exactos y transición
                switch (phase)
                {
                    case Phase.StartWait:
                        // empieza primer offset SIN shake
                        shakeOn = false;
                        ApplyAll(0f, 0f, cfg.shakeOffSpeed);
                        StartOffsetOut();
                        break;

                    case Phase.OffsetOut:
                        // Llegó a target: flip instantáneo al contrario y vuelve a 0
                        StartOffsetBackWithInstantFlip();
                        break;

                    case Phase.OffsetBack:
                        // Ya está en 0
                        ApplyAll(0f, 0f, shakeOn ? cfg.shakeOnSpeed : cfg.shakeOffSpeed);

                        if (!shakeOn)
                        {
                            // tras offset SIN shake: activar shake y esperar 1s
                            shakeOn = true;
                            ApplyAll(0f, 0f, cfg.shakeOnSpeed);
                            Enter(Phase.WaitShakeOn);
                        }
                        else
                        {
                            // tras offset CON shake: apagar shake y esperar 1s
                            if (cfg.shakeFadeOutSeconds > 0f)
                            {
                                shakeFrom = cfg.shakeOnSpeed;
                                shakeTo = cfg.shakeOffSpeed;
                                Enter(Phase.ShakeFadeOut);
                            }
                            else
                            {
                                shakeOn = false;
                                ApplyAll(0f, 0f, cfg.shakeOffSpeed);
                                Enter(Phase.WaitShakeOff);
                            }
                        }
                        break;

                    case Phase.WaitShakeOn:
                        // offset CON shake
                        StartOffsetOut();
                        break;

                    case Phase.ShakeFadeOut:
                        // al terminar fade, shake ya está apagado
                        shakeOn = false;
                        ApplyAll(0f, 0f, cfg.shakeOffSpeed);
                        Enter(Phase.WaitShakeOff);
                        break;

                    case Phase.WaitShakeOff:
                        // offset SIN shake
                        StartOffsetOut();
                        break;
                }
            }
        }

        private void StartOffsetOut()
        {
            PickRandomAxisAndSign();

            phase = Phase.OffsetOut;
            timer = 0f;
            duration = Mathf.Max(0f, cfg.offsetOutSeconds);

            offsetFrom = 0f;
            offsetTo = target;
        }

        private void StartOffsetBackWithInstantFlip()
        {
            // Flip instantáneo (sin “parpadeo” visible si se aplica antes de render)
            float flipped = -target;

            // Aplicar flip instantáneo YA
            if (useX) ApplyAll(flipped, 0f, shakeOn ? cfg.shakeOnSpeed : cfg.shakeOffSpeed);
            else ApplyAll(0f, flipped, shakeOn ? cfg.shakeOnSpeed : cfg.shakeOffSpeed);

            phase = Phase.OffsetBack;
            timer = 0f;
            duration = Mathf.Max(0f, cfg.offsetBackSeconds);

            offsetFrom = flipped;
            offsetTo = 0f;
        }

        private void Enter(Phase p)
        {
            phase = p;
            timer = 0f;

            switch (p)
            {
                case Phase.StartWait:
                    duration = Mathf.Max(0f, cfg.startDelaySeconds);
                    break;

                case Phase.WaitShakeOn:
                    duration = Mathf.Max(0f, cfg.waitWithShakeOnSeconds);
                    break;

                case Phase.WaitShakeOff:
                    duration = Mathf.Max(0f, cfg.waitWithShakeOffSeconds);
                    break;

                case Phase.ShakeFadeOut:
                    duration = Mathf.Max(0f, cfg.shakeFadeOutSeconds);
                    break;

                default:
                    duration = 0f;
                    break;
            }
        }

        private void PickRandomAxisAndSign()
        {
            useX = cfg.randomAxis
                ? (Random.value < 0.5f)
                : (cfg.fixedAxis == FixedAxis.X);

            int sign = cfg.randomSign
                ? (Random.value < 0.5f ? -1 : 1)
                : (cfg.fixedSign >= 0 ? 1 : -1);

            float mag = Mathf.Abs(cfg.offsetMagnitude);
            target = sign * mag;
        }

        private void ApplyAll(float x, float y, float shake)
        {
            SafeSetFloat(cfg.offsetUvXProp, x);
            SafeSetFloat(cfg.offsetUvYProp, y);
            SafeSetFloat(cfg.shakeUvSpeedProp, shake);
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
