using UnityEngine;

public class BoundaryForceField2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private string playerTag = "Player";

    [Header("Instant push (on enter)")]
    [SerializeField] private float enterImpulse = 12f;   // empujón fuerte al entrar
    [SerializeField] private float impulseCooldown = 0.25f; // evita spam si entra/sale rápido

    [Header("Continuous force (while inside)")]
    [SerializeField] private float pushForce = 25f;      // fuerza constante mientras esté dentro
    [SerializeField] private AnimationCurve forceByDepth = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("How to compute 'depth'")]
    [SerializeField] private BoxCollider2D innerSafeArea; // opcional: el collider de PlayerArea (trigger) si quieres medir profundidad real

    private float lastImpulseTime = -999f;

    private void Reset()
    {
        // Auto: intenta coger el collider del propio GO si existe
        if (innerSafeArea == null)
            innerSafeArea = FindObjectOfType<BoxCollider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) return;

        // Impulso SOLO una vez al entrar (y con cooldown)
        if (Time.time - lastImpulseTime < impulseCooldown) return;
        lastImpulseTime = Time.time;

        Vector2 dirToCenter = GetDirectionToCenter(other.transform.position);
        rb.AddForce(dirToCenter * enterImpulse, ForceMode2D.Impulse);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) return;

        Vector2 dirToCenter = GetDirectionToCenter(other.transform.position);

        float depth01 = GetDepth01(other.transform.position); // 0 cerca del borde, 1 muy afuera
        float forceMult = forceByDepth.Evaluate(depth01);

        rb.AddForce(dirToCenter * pushForce * forceMult, ForceMode2D.Force);
    }

    private Vector2 GetDirectionToCenter(Vector3 worldPos)
    {
        Vector3 center = transform.position;
        Vector2 dir = (center - worldPos);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
        return dir.normalized;
    }

    // depth01: 0..1 (si innerSafeArea existe, lo calcula "según lo lejos que estás de volver dentro")
    private float GetDepth01(Vector3 worldPos)
    {
        if (innerSafeArea == null)
            return 1f;

        Bounds b = innerSafeArea.bounds;

        // Clamp dentro del área segura
        float clampedX = Mathf.Clamp(worldPos.x, b.min.x, b.max.x);
        float clampedY = Mathf.Clamp(worldPos.y, b.min.y, b.max.y);
        Vector2 closestInside = new Vector2(clampedX, clampedY);

        float dist = Vector2.Distance(worldPos, closestInside);

        // Escala simple: a 0 dist => 0, a X dist => 1
        // Ajusta este “maxDist” según tu tamaño de zona.
        float maxDist = 1.0f;
        return Mathf.Clamp01(dist / maxDist);
    }
}
