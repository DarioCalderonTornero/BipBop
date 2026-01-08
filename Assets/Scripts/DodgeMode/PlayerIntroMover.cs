using UnityEngine;
using static DodgeState;

public class PlayerIntroMover : MonoBehaviour
{
    public float targetOffsetY = 6f;    // cuánto debe subir desde donde la coloques
    public float introDuration = 1.5f;  // tiempo en subir

    private Vector3 startPos;
    private Vector3 finalPos;
    private float introTimer = 0f;
    private bool finishedIntro = false;

    private PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void Start()
    {
        // Tú colocas la nave abajo en la escena.
        startPos = transform.position;
        // El centro será su Y actual + offset
        finalPos = new Vector3(startPos.x, startPos.y + targetOffsetY, startPos.z);

        if (playerController != null)
        {
            playerController.isIntroMoving = true;      // sprite de movimiento
            playerController.SetTrailsActive(true);     // trails ON durante la subida
            playerController.SetSmokeActive(true);
        }
    }

    private void Update()
    {
        if (DodgeState.Instance == null)
            return;

        // Solo animar durante Countdown
        if (DodgeState.Instance.dodgeGameState != DodgeGameStateEnum.Countdown)
        {
            // Si por lo que sea salimos del estado, apagamos intro
            if (playerController != null && !finishedIntro)
            {
                playerController.isIntroMoving = false;
                playerController.SetTrailsActive(false);
                playerController.SetSmokeActive(false);
            }
            return;
        }

        if (finishedIntro)
            return;

        introTimer += Time.deltaTime;
        float t = Mathf.Clamp01(introTimer / introDuration);

        transform.position = Vector3.Lerp(startPos, finalPos, t);

        if (t >= 1f)
        {
            finishedIntro = true;

            if (playerController != null)
            {
                // Fijar el target del PlayerController a la posición actual
                playerController.ResetTargetToCurrentPosition();

                // Pasar a sprite quieto y apagar trails
                playerController.isIntroMoving = false;
                playerController.SetTrailsActive(false);
            }
        }
    }
}
