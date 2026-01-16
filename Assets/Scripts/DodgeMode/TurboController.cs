using System;
using UnityEngine;

public class TurboController : MonoBehaviour
{
    public enum TurboState
    {
        Idle,        // 0% de carga
        Boosting,    // Manteniendo botón, sube carga
        Cooling,     // Botón suelto, baja carga
        Full,        // 100%
        Exploded     // Explotó
    }

    [Header("References")]
    [SerializeField] private PlayerController player; // arrastra tu PlayerController aquí

    [Header("Turbo Settings")]
    [SerializeField] private float boostMultiplier = 1.8f;   // multiplicador de velocidad al mantener
    [SerializeField] private float fillSeconds = 2.0f;       // tiempo en llenarse (y vaciarse)

    [Tooltip("Si está activo, al llegar al 100% mientras sigues manteniendo, entrará en 'peligro' y explotará tras un delay.")]
    [SerializeField] private bool explodeWhenFullAndHeld = true;

    [Tooltip("Tiempo extra desde que llegas a 100% (manteniendo) hasta explotar.")]
    [SerializeField] private float explodeDelaySeconds = 1.0f;

    [Header("Editor Testing (optional)")]
    [SerializeField] private bool allowKeyboardTestInEditor = true;
    [SerializeField] private KeyCode editorHoldKey = KeyCode.Space;

    // 0..1
    [Range(0f, 1f)]
    [SerializeField] private float charge01 = 0f;

    public TurboState State { get; private set; } = TurboState.Idle;
    public float Charge01 => charge01;
    public bool IsHeld => isHeld;
    public bool IsExploded => State == TurboState.Exploded;

    // UI / FX hooks
    public event Action<float> OnChargeChanged;         // barra normal 0..1
    public event Action<TurboState> OnStateChanged;     // cambio de estado
    public event Action OnExploded;                     // explotó

    // NUEVO: avisos de "peligro" (para UI)
    public event Action OnDangerStarted;                // llegó a 100% y empieza la cuenta atrás
    public event Action<float> OnDangerTick01;          // progreso 0..1 del delay
    public event Action OnDangerCanceled;               // soltó antes de explotar

    private bool isHeld = false;
    private float baseMoveSpeed = -1f;
    private float ratePerSecond; // cuánto sube/baja charge01 por segundo

    // NUEVO: peligro
    private bool isInDanger = false;
    private float dangerTimer = 0f;

    private void Awake()
    {
        ratePerSecond = (fillSeconds <= 0.0001f) ? 999f : (1f / fillSeconds);
    }

    private void Start()
    {
        if (player != null)
            baseMoveSpeed = player.moveSpeed;

        SetState(charge01 <= 0f ? TurboState.Idle : TurboState.Cooling, true);
        ApplySpeedMultiplier();
        OnChargeChanged?.Invoke(charge01);
    }

    private void Update()
    {
        if (State == TurboState.Exploded)
            return;

#if UNITY_EDITOR
        if (allowKeyboardTestInEditor)
        {
            if (Input.GetKeyDown(editorHoldKey)) Hold();
            if (Input.GetKeyUp(editorHoldKey)) Release();
        }
#endif

        float prev = charge01;

        if (isHeld)
        {
            charge01 = Mathf.Clamp01(charge01 + ratePerSecond * Time.deltaTime);
            MeteorCameraShake.Instance.Shake(charge01, 0.02f);

            if (explodeWhenFullAndHeld && charge01 >= 1f)
            {
                charge01 = 1f;

                if (!isInDanger)
                {
                    isInDanger = true;
                    dangerTimer = 0f;
                    OnDangerStarted?.Invoke();
                }

                dangerTimer += Time.deltaTime;

                float t01 = (explodeDelaySeconds <= 0.0001f)
                    ? 1f
                    : Mathf.Clamp01(dangerTimer / explodeDelaySeconds);

                OnDangerTick01?.Invoke(t01);

                SetState(TurboState.Full);

                if (dangerTimer >= explodeDelaySeconds)
                {
                    Explode();
                    return;
                }

                if (!Mathf.Approximately(prev, charge01))
                    OnChargeChanged?.Invoke(charge01);

                ApplySpeedMultiplier();
                return;
            }

            SetState(TurboState.Boosting);
        }
        else
        {
            if (isInDanger)
            {
                isInDanger = false;
                dangerTimer = 0f;
                OnDangerCanceled?.Invoke();
            }

            charge01 = Mathf.Clamp01(charge01 - ratePerSecond * Time.deltaTime);

            if (charge01 <= 0f)
                SetState(TurboState.Idle);
            else if (charge01 >= 1f)
                SetState(TurboState.Full);
            else
                SetState(TurboState.Cooling);
        }

        if (!Mathf.Approximately(prev, charge01))
            OnChargeChanged?.Invoke(charge01);

        ApplySpeedMultiplier();
    }

    /// <summary>
    /// Llamar desde UI (PointerDown).
    /// </summary>
    public void Hold()
    {
        if (State == TurboState.Exploded) return;
        isHeld = true;
    }

    /// <summary>
    /// Llamar desde UI (PointerUp / PointerExit).
    /// </summary>
    public void Release()
    {
        if (State == TurboState.Exploded) return;
        isHeld = false;
    }

    public bool IsHold()
    {
        return isHeld;
    }

    public void ResetTurbo()
    {
        isHeld = false;
        charge01 = 0f;

        // reset peligro
        if (isInDanger)
        {
            isInDanger = false;
            dangerTimer = 0f;
            OnDangerCanceled?.Invoke();
        }

        SetState(TurboState.Idle, true);
        ApplySpeedMultiplier();
        OnChargeChanged?.Invoke(charge01);
    }

    private void ApplySpeedMultiplier()
    {
        if (player == null) return;

        if (baseMoveSpeed < 0f)
            baseMoveSpeed = player.moveSpeed;

        float mult = (isHeld && State != TurboState.Exploded) ? boostMultiplier : 1f;
        player.moveSpeed = baseMoveSpeed * mult;
    }

    private void Explode()
    {
        charge01 = 1f;
        OnChargeChanged?.Invoke(charge01);

        SetState(TurboState.Exploded, true);
        ApplySpeedMultiplier();

        if (DodgeManager.Instance != null)
        {
            DodgeManager.Instance.PlayerHit(null);
        }
        else
        {
            Debug.LogWarning("No hay DodgeManager.Instance. No puedo iniciar el flujo de muerte.");
        }

        OnExploded?.Invoke();
    }

    private void SetState(TurboState newState, bool force = false)
    {
        if (!force && State == newState) return;
        State = newState;
        OnStateChanged?.Invoke(State);
    }
}
