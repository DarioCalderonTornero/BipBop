using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BouncingShape : MonoBehaviour
{
    [Header("Shape Info")]
    public string shapeName;         // Ejemplo: "Círculo", "Cuadrado", etc.
    public float initialSpeed = 2f;    // Velocidad inicial

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
        // Si la figura se activa por primera vez, le asignamos una velocidad aleatoria
        SetRandomVelocity();
    }

    // Este método se llamará cada vez que el objeto se active
    private void OnEnable()
    {
        // Vuelve a asignar una velocidad aleatoria al activarse
        SetRandomVelocity();
    }

    private void SetRandomVelocity()
    {
        if (rb != null)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            rb.velocity = randomDirection * initialSpeed;
        }
    }

    // Actualiza la velocidad basada en el multiplicador (se llama desde el gestor del modo)
    public void UpdateSpeed(float multiplier)
    {
        if (rb != null && rb.velocity != Vector2.zero)
        {
            rb.velocity = rb.velocity.normalized * initialSpeed * multiplier;
        }
    }

    public void SetNormalColor()
    {
        spriteRenderer.color = normalColor;
    }

    // Cambia el color temporalmente y vuelve a normal después de un tiempo
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

    // Detecta el toque de la figura
    private void OnMouseDown()
    {
        GeometricModeManager.Instance.OnShapeTapped(this);
    }

    // Este método se ejecuta cuando la figura colisiona con otro collider
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Opcional: Si quieres aplicar este comportamiento solo con los bordes, puedes comprobar:
        // if(collision.gameObject.CompareTag("Wall")) { ... }

        // Aplica una pequeña rotación aleatoria a la velocidad para evitar estancamientos
        float randomAngle = Random.Range(-40f, 40f); // Puedes ajustar este rango según necesites
        Vector2 newVelocity = Quaternion.Euler(0, 0, randomAngle) * rb.velocity;
        rb.velocity = newVelocity;
    }
}
