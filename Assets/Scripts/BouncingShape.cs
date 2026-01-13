using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

public class BouncingShape : MonoBehaviour
{
    [Header("Shape Info")]
    public LocalizedString shapeName;     // 👈 Ahora LocalizedString
    public float initialSpeed = 2f;

    private Rigidbody2D rb;
    private Color normalColor;

    [Header("UI Icon")]
    [SerializeField] private Sprite uiIcon;

    private JellyFXTrailSparkles fx;

    [SerializeField] private float tapSquishScale = 0.85f;
    [SerializeField] private float tapSquishDuration = 0.08f;
    private Coroutine squishRoutine;

    [Header("Sprites (Visual)")]
    [SerializeField] private SpriteRenderer spriteRenderer; // el del child Visual
    [SerializeField] private Sprite happySprite;
    [SerializeField] private Sprite scaredSprite;
    [SerializeField] private float scaredTime = 0.15f;

    [SerializeField] private float scaredOnHitCooldown = 0.12f;
    private float lastScaredTime = -999f;

    private Coroutine scaredRoutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        normalColor = spriteRenderer.color;

        fx = GetComponent<JellyFXTrailSparkles>();
    }

    private void Start()
    {
        SetRandomVelocity();
    }

    private void OnEnable()
    {
        SetRandomVelocity();
    }

    public Sprite GetUIIcon()
    {
        // Si no asignas icono, usa el sprite actual como fallback
        if (uiIcon != null) return uiIcon;
        return spriteRenderer != null ? spriteRenderer.sprite : null;
    }

    public void PlayTapSquish()
    {
        if (squishRoutine != null) StopCoroutine(squishRoutine);
        squishRoutine = StartCoroutine(SquishRoutine());
    }

    private IEnumerator SquishRoutine()
    {
        Vector3 baseScale = Vector3.one; // si escalas tus jellys, usa transform.localScale como base
        Vector3 squish = baseScale * tapSquishScale;

        float half = tapSquishDuration * 0.5f;
        float t = 0f;

        while (t < half)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / half);
            transform.localScale = Vector3.Lerp(baseScale, squish, k);
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / half);
            transform.localScale = Vector3.Lerp(squish, baseScale, k);
            yield return null;
        }

        transform.localScale = baseScale;
        squishRoutine = null;
    }

    private void SetRandomVelocity()
    {
        if (rb != null)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            rb.linearVelocity = randomDirection * initialSpeed;
        }
    }

    public void UpdateSpeed(float multiplier)
    {
        if (rb != null && rb.linearVelocity != Vector2.zero)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * initialSpeed * multiplier;
        }
    }

    public void SetNormalColor()
    {
        spriteRenderer.color = normalColor;
    }

    public void TemporarilyChangeColor(Color newColor, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(ChangeColorRoutine(newColor, duration));
    }

    private IEnumerator ChangeColorRoutine(Color newColor, float duration)
    {
        spriteRenderer.color = newColor;
        yield return new WaitForSeconds(duration);
        spriteRenderer.color = normalColor;
    }

    private void OnMouseDown()
    {
        fx?.BurstTapWide();
        GeometricModeManager.Instance.OnShapeTapped(this);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayScaredSprite();

        var wobble = GetComponent<JellyImpactWobble>();
        if (wobble != null)
        {
            Vector2 normal = collision.GetContact(0).normal;
            float strength = Mathf.Clamp01(collision.relativeVelocity.magnitude / 8f);
            wobble.Impact(normal, strength);
        }

        // tu random angle si quieres mantenerlo (opcional)
        float randomAngle = Random.Range(-40f, 40f);
        Vector2 newVelocity = Quaternion.Euler(0, 0, randomAngle) * rb.linearVelocity;
        rb.linearVelocity = newVelocity;
    }

    public void RandomizeDirection()
    {
        if (rb == null) return;

        // Mantener la velocidad actual si se está moviendo,
        // si no, usar la velocidad inicial.
        float speed = rb.linearVelocity.magnitude;
        if (speed <= 0.01f)
            speed = initialSpeed;

        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        rb.linearVelocity = randomDirection * speed;
    }

    public void ReverseDirection()
    {
        if (rb == null) return;

        // Si por lo que sea está casi parado, reasignamos
        if (rb.linearVelocity.sqrMagnitude < 0.0001f)
        {
            SetRandomVelocity();
            return;
        }

        rb.linearVelocity = -rb.linearVelocity;
    }

    public void PlayScaredSprite()
    {
        if (!spriteRenderer || scaredSprite == null) return;

        // cooldown para evitar spam en rebotes seguidos
        if (Time.time - lastScaredTime < scaredOnHitCooldown) return;
        lastScaredTime = Time.time;

        if (scaredRoutine != null) StopCoroutine(scaredRoutine);
        scaredRoutine = StartCoroutine(ScaredRoutine());
    }

    private IEnumerator ScaredRoutine()
    {
        var original = happySprite != null ? happySprite : spriteRenderer.sprite;

        spriteRenderer.sprite = scaredSprite;
        yield return new WaitForSeconds(scaredTime);
        spriteRenderer.sprite = original;

        scaredRoutine = null;
    }
}
