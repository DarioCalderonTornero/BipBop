using UnityEngine;

public class PlayerBoundsLimiter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayArea2D playArea;

    [Header("Force Field Feel")]
    [Tooltip("Cuánto empuja hacia dentro cuando estás fuera (más = más fuerte).")]
    [SerializeField] private float springStrength = 18f;

    [Tooltip("Amortiguación para que no rebote/tiemble (más = más suave).")]
    [SerializeField] private float damping = 10f;

    [Tooltip("Si te sales mucho, te hace clamp suave para que nunca te pierdas.")]
    [SerializeField] private float hardClampOutsideDistance = 1.2f;

    // velocidad interna del "campo" (no es la del player, es solo para la corrección)
    private Vector2 correctionVelocity;

    private void Awake()
    {
        if (playArea == null)
            playArea = FindObjectOfType<PlayArea2D>(true);
    }

    private void LateUpdate()
    {
        if (playArea == null) return;

        Vector2 pos = transform.position;

        // Dentro: relajamos la corrección para que no arrastre
        if (playArea.Contains(pos))
        {
            // vuelve a 0 poco a poco para que no quede “inercia” rara
            correctionVelocity = Vector2.Lerp(correctionVelocity, Vector2.zero, Time.deltaTime * 12f);
            return;
        }

        // Punto más cercano dentro del área
        Vector2 closestInside = playArea.ClosestPoint(pos);
        Vector2 toInside = (closestInside - pos);

        float outsideDist = toInside.magnitude;

        // Safety: si se fue muy lejos, clamp suave
        if (outsideDist >= hardClampOutsideDistance)
        {
            transform.position = Vector2.Lerp(pos, closestInside, Time.deltaTime * 18f);
            correctionVelocity = Vector2.zero;
            return;
        }

        // === Campo de fuerza tipo muelle (Hooke) ===
        // aceleración = (toInside * springStrength) - (vel * damping)
        Vector2 accel = (toInside * springStrength) - (correctionVelocity * damping);

        correctionVelocity += accel * Time.deltaTime;

        // Aplicar corrección a la posición (suave, con “empuje hacia atrás”)
        transform.position = pos + correctionVelocity * Time.deltaTime;
    }
}
