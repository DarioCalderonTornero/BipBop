using UnityEngine;
using static DodgeState;

public class PlayerIntroMover : MonoBehaviour
{
    public float targetOffsetY = 6f;
    public float introDuration = 1.5f;

    private Vector3 baseStartPos;   // ✅ posición fija (la misma siempre)
    private Vector3 startPos;
    private Vector3 finalPos;

    private float introTimer = 0f;
    private bool finishedIntro = false;
    private bool introBegan = false;

    private PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();

        // ✅ Guardar base lo antes posible (antes de que tutorial/transición toque nada)
        baseStartPos = transform.position;
    }

    private void BeginIntro()
    {
        introBegan = true;
        finishedIntro = false;
        introTimer = 0f;

        // ✅ Fuerza siempre la misma posición inicial
        transform.position = baseStartPos;

        // ✅ Y calcula desde esa base
        startPos = baseStartPos;
        finalPos = new Vector3(startPos.x, startPos.y + targetOffsetY, startPos.z);

        if (playerController != null)
        {
            playerController.isIntroMoving = true;
            playerController.SetTrailsActive(true);
            playerController.SetSmokeActive(true);

            // limpiar trails para que se vean bien
            var trs = GetComponentsInChildren<TrailRenderer>(true);
            foreach (var tr in trs)
            {
                if (!tr) continue;
                tr.Clear();
                tr.emitting = true;
                tr.enabled = true;
            }

            // si tu humo son ParticleSystem
            var pss = GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in pss)
            {
                if (!ps) continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true);
            }
        }
    }

    private void EndIntroVFX()
    {
        if (playerController == null) return;
        playerController.isIntroMoving = false;
        playerController.SetTrailsActive(false);
        playerController.SetSmokeActive(false);
    }

    private void Update()
    {
        if (DodgeState.Instance == null) return;

        if (DodgeState.Instance.dodgeGameState != DodgeGameStateEnum.Countdown)
        {
            // solo apagar si ya había empezado
            if (introBegan && !finishedIntro) EndIntroVFX();
            return;
        }

        if (!introBegan)
            BeginIntro();

        if (finishedIntro) return;

        introTimer += Time.deltaTime;
        float t = Mathf.Clamp01(introTimer / introDuration);

        transform.position = Vector3.Lerp(startPos, finalPos, t);

        if (t >= 1f)
        {
            finishedIntro = true;

            if (playerController != null)
            {
                playerController.ResetCruiseDirectionToForward();
                EndIntroVFX();
            }
        }
    }
}
