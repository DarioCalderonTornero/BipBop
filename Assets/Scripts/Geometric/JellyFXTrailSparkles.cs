using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class JellyFXTrailSparkles : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private ParticleSystem sparkles;

    [Header("FX Color (set by you)")]
    [SerializeField] private Color fxColor = Color.white;   // <-- tú lo asignas

    [Header("Speed -> FX")]
    [SerializeField] private float minSpeedToShow = 0.5f;
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float maxRateOverDistance = 28f;

    [Header("Bursts")]
    [SerializeField] private int tapBurst = 14;
    [SerializeField] private int hitBurst = 8;

    private Rigidbody2D rb;
    private ParticleSystem.EmissionModule em;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!trail) trail = GetComponentInChildren<TrailRenderer>(true);
        if (!sparkles) sparkles = GetComponentInChildren<ParticleSystem>(true);

        if (sparkles != null) em = sparkles.emission;

        ApplyColor(fxColor);
    }

    private void OnEnable()
    {
        if (trail) trail.Clear();
        if (sparkles) sparkles.Clear();

        ApplyColor(fxColor);
    }

    private void Update()
    {
        if (sparkles == null || rb == null) return;

        float speed = rb.linearVelocity.magnitude;
        float t = Mathf.InverseLerp(minSpeedToShow, maxSpeed, speed);

        em.rateOverDistance = new ParticleSystem.MinMaxCurve(Mathf.Lerp(0f, maxRateOverDistance, t));
    }

    // Llamable desde fuera cuando tú quieras
    public void SetFXColor(Color c)
    {
        fxColor = c;
        ApplyColor(fxColor);
    }

    private void ApplyColor(Color c)
    {
        if (trail)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                new[] { new GradientAlphaKey(0.55f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            trail.colorGradient = g;
        }

        if (sparkles)
        {
            var main = sparkles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(c.r, c.g, c.b, 0.85f),
                new Color(c.r, c.g, c.b, 0.25f)
            );
        }
    }

    public void BurstTapWide(float minSpeed = 3.5f, float maxSpeed = 6.0f, int count = 10)
    {
        if (!sparkles) return;

        // Asegura que se emite en el sitio correcto
        sparkles.transform.position = transform.position;

        var ep = new ParticleSystem.EmitParams();
        ep.startColor = new Color(fxColor.r, fxColor.g, fxColor.b, 1f);

        for (int i = 0; i < count; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;

            // MÁS LEJOS = más velocidad y algo más de vida
            ep.velocity = dir * Random.Range(minSpeed, maxSpeed);
            ep.startLifetime = Random.Range(0.35f, 0.60f);
            ep.startSize = Random.Range(0.10f, 0.16f);

            sparkles.Emit(ep, 1);
        }
    }

    public void BurstHit()
    {
        if (!sparkles) return;

        sparkles.transform.position = transform.position;

        var ep = new ParticleSystem.EmitParams();
        ep.startColor = new Color(fxColor.r, fxColor.g, fxColor.b, 1f);

        for (int i = 0; i < hitBurst; i++)
        {
            ep.velocity = (Vector3)(Random.insideUnitCircle.normalized * Random.Range(0.6f, 1.6f));
            ep.startSize = Random.Range(0.08f, 0.14f);
            ep.startLifetime = Random.Range(0.20f, 0.35f);
            sparkles.Emit(ep, 1);
        }
    }

    private void OnCollisionEnter2D(Collision2D c) => BurstHit();
}
