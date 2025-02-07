using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BouncingShape : MonoBehaviour
{
    [Header("Shape Info")]
    public string shapeName;         
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
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        rb.velocity = randomDirection * initialSpeed;
    }

    public void UpdateSpeed(float multiplier)
    {
        rb.velocity = rb.velocity.normalized * initialSpeed * multiplier;
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
        float randomAngle = Random.Range(-20f, 20f);
        Vector2 newVelocity = Quaternion.Euler(0, 0, randomAngle) * rb.velocity;
        rb.velocity = newVelocity;
    }
}
