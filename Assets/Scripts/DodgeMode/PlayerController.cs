using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;          // esto ahora es tu “crucero”
    public float rotationSpeed = 360f;

    [Header("Sprites")]
    public SpriteRenderer spriteRenderer;
    public Sprite idleSprite;
    public Sprite movingSprite;

    [Header("Trails")]
    public TrailRenderer trailLeft;
    public TrailRenderer trailRight;

    [Header("Input")]
    [Tooltip("Deadzone del joystick (0..1).")]
    [SerializeField] private float inputDeadzone = 0.15f;

    [HideInInspector] public bool isIntroMoving = false;

    [Header("Smoke")]
    public ParticleSystem smokeLeft;
    public ParticleSystem smokeRight;

    // Dirección actual (en mundo) hacia la que “apunta” la nave (joystick)
    private Vector3 desiredDirWorld = Vector3.up;

    private void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        SetTrailsActive(false);
        SetSmokeActive(false);

        desiredDirWorld = transform.up;
    }

    private void Update()
    {
        bool isPlaying =
            DodgeState.Instance != null &&
            DodgeState.Instance.dodgeGameState == DodgeState.DodgeGameStateEnum.Playing;

        if (!isPlaying)
        {
            bool visualMoving = isIntroMoving;
            UpdateVisuals(visualMoving);
            return;
        }

        Vector2 move = Vector2.zero;
        if (InputManager.Instance != null)
            move = InputManager.Instance.GetDodgePlayerMovement();

        if (move.sqrMagnitude >= inputDeadzone * inputDeadzone)
        {
            desiredDirWorld = new Vector3(move.x, move.y, 0f).normalized;
        }

        float angle = Mathf.Atan2(desiredDirWorld.y, desiredDirWorld.x) * Mathf.Rad2Deg;
        Quaternion targetRot = Quaternion.AngleAxis(angle - 90f, Vector3.forward);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            rotationSpeed * Time.deltaTime
        );

        transform.position += transform.up * moveSpeed * Time.deltaTime;

        UpdateVisuals(true);
    }

    private void UpdateVisuals(bool moving)
    {
        if (spriteRenderer != null)
            spriteRenderer.sprite = moving ? movingSprite : idleSprite;

        SetTrailsActive(moving);
        SetSmokeActive(moving);
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

    public void ResetCruiseDirectionToForward()
    {
        desiredDirWorld = transform.up;
    }
}
