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
    private SpriteRenderer spriteRenderer;
    private Color normalColor;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        normalColor = spriteRenderer.color;
    }

    private void Start()
    {
        SetRandomVelocity();
    }

    private void OnEnable()
    {
        SetRandomVelocity();
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
        GeometricModeManager.Instance.OnShapeTapped(this);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        float randomAngle = Random.Range(-40f, 40f);
        Vector2 newVelocity = Quaternion.Euler(0, 0, randomAngle) * rb.linearVelocity;
        rb.linearVelocity = newVelocity;
    }
}
