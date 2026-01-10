using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.ParticleSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 360f;
    public float stopDistance = 0.1f;

    [Header("Sprites")]
    public SpriteRenderer spriteRenderer; // hijo visual o el propio
    public Sprite idleSprite;
    public Sprite movingSprite;

    [Header("Trails")]
    public TrailRenderer trailLeft;
    public TrailRenderer trailRight;

    public float minInputDistance = 0.35f;

    [HideInInspector] public bool isIntroMoving = false;
    [HideInInspector] public bool forceTrails = false;

    private Vector3 targetPosition;

    [Header("Smoke")]
    public ParticleSystem smokeLeft;
    public ParticleSystem smokeRight;

    private void Start()
    {
        targetPosition = transform.position;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        SetTrailsActive(false);
    }

    private void Update()
    {
        bool isPlaying = DodgeState.Instance != null &&
                         DodgeState.Instance.dodgeGameState == DodgeState.DodgeGameStateEnum.Playing;

        // 1) SOLO leer input si estamos en Playing
        //    (CAMBIO MÍNIMO: en vez de touch/mouse, leemos del InputManager -> stick Vector2)
        if (isPlaying)
        {
            Vector2 move = Vector2.zero;

            if (InputManager.Instance != null)
                move = InputManager.Instance.GetDodgePlayerMovement();

            // Si hay input suficiente, convertimos esa dirección a un "targetPosition"
            // para mantener exactamente la misma lógica de rotación + avance hacia target.
            if (move.sqrMagnitude > 0.0001f)
            {
                // Direction -> world
                Vector3 dir = new Vector3(move.x, move.y, 0f).normalized;

                // Un target suficientemente lejos para que siempre "estemos moviendo"
                // (mantenemos minInputDistance como referencia de escala)
                float targetDistance = Mathf.Max(minInputDistance, stopDistance * 2f);

                Vector3 desiredTarget = transform.position + dir * targetDistance;

                if (Vector3.Distance(desiredTarget, transform.position) > minInputDistance)
                    targetPosition = desiredTarget;
            }
        }

        // 2) LÓGICA DE VISUAL Y MOVIMIENTO (se ejecuta también en intro)
        Vector3 toTarget = targetPosition - transform.position;
        toTarget.z = 0f;
        float dist = toTarget.magnitude;

        bool isMoving = dist > stopDistance;

        // Durante la intro queremos que parezca que se mueve (sprite + trails),
        // aunque el movimiento real lo hace PlayerIntroMover
        if (isIntroMoving && !isPlaying)
            isMoving = true;

        // Sprite según movimiento
        if (spriteRenderer != null)
            spriteRenderer.sprite = isMoving ? movingSprite : idleSprite;

        // Trails según movimiento
        SetTrailsActive(isMoving);
        SetSmokeActive(isMoving);

        // Si NO estamos en Playing, aquí terminamos (intro no usa este movimiento)
        if (!isPlaying)
            return;

        // A partir de aquí solo vale para Playing
        if (!isMoving)
        {
            transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
            return;
        }

        float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        Quaternion targetRot = Quaternion.AngleAxis(angle - 90f, Vector3.forward);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            rotationSpeed * Time.deltaTime
        );

        transform.position += transform.up * moveSpeed * Time.deltaTime;
    }

    public void SetTrailsActive(bool active)
    {
        if (trailLeft != null)
        {
            trailLeft.emitting = active;
            if (!active) trailLeft.Clear();
        }

        if (trailRight != null)
        {
            trailRight.emitting = active;
            if (!active) trailRight.Clear();
        }
    }

    public void SetSmokeActive(bool active)
    {
        if (smokeLeft != null)
        {
            var em = smokeLeft.emission;
            em.enabled = active;
            if (!active) smokeLeft.Clear();
        }

        if (smokeRight != null)
        {
            var em = smokeRight.emission;
            em.enabled = active;
            if (!active) smokeRight.Clear();
        }
    }

    public void ResetTargetToCurrentPosition()
    {
        targetPosition = transform.position;
    }
}
