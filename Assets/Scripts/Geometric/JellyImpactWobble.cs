using System.Collections;
using UnityEngine;

public class JellyImpactWobble : MonoBehaviour
{
    [SerializeField] private Transform visual; // child "Visual"
    [Header("Impact")]
    [SerializeField] private float maxSquash = 0.70f;   // 0.70 = bastante aplastado
    [SerializeField] private float maxOffset = 0.08f;   // desplazamiento del visual
    [SerializeField] private float wobbleDuration = 0.35f;
    [SerializeField] private float wobbleFrequency = 18f;
    [SerializeField] private float wobbleDamping = 10f;

    Coroutine routine;
    Vector3 baseScale;
    Vector3 basePos;

    void Awake()
    {
        if (!visual) visual = transform;
        baseScale = visual.localScale;
        basePos = visual.localPosition;
    }

    public void Impact(Vector2 worldNormal, float strength01)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ImpactRoutine(worldNormal, Mathf.Clamp01(strength01)));
    }

    IEnumerator ImpactRoutine(Vector2 worldNormal, float s)
    {
        // normal en espacio local del root
        Vector2 n = transform.InverseTransformDirection(worldNormal).normalized;

        // decidir eje principal (aprox) para squash sin rotar el sprite
        bool horizontalHit = Mathf.Abs(n.x) > Mathf.Abs(n.y);

        float squash = Mathf.Lerp(0.92f, maxSquash, s);     // cuanto más fuerte, más squash
        float stretch = Mathf.Lerp(1.05f, 1.25f, s);
        float offsetAmt = Mathf.Lerp(0.02f, maxOffset, s);

        Vector3 targetScale = baseScale;
        Vector3 targetPos = basePos + (Vector3)(-n * offsetAmt); // “masa” se mueve hacia dentro

        if (horizontalHit)
        {
            targetScale.x = baseScale.x * squash;
            targetScale.y = baseScale.y * stretch;
        }
        else
        {
            targetScale.y = baseScale.y * squash;
            targetScale.x = baseScale.x * stretch;
        }

        // fase 1: golpe rápido
        float hitTime = 0.06f;
        for (float t = 0; t < hitTime; t += Time.deltaTime)
        {
            float k = t / hitTime;
            visual.localScale = Vector3.Lerp(baseScale, targetScale, k);
            visual.localPosition = Vector3.Lerp(basePos, targetPos, k);
            yield return null;
        }

        // fase 2: wobble amortiguado
        float dur = wobbleDuration;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            float decay = Mathf.Exp(-wobbleDamping * t);
            float w = Mathf.Sin(t * wobbleFrequency) * decay;

            // oscilación en el eje contrario para que parezca gel
            Vector3 wobScale = baseScale;
            if (horizontalHit)
            {
                wobScale.x = Mathf.Lerp(baseScale.x, baseScale.x * 0.95f, s) + w * 0.08f;
                wobScale.y = Mathf.Lerp(baseScale.y, baseScale.y * 1.05f, s) - w * 0.06f;
            }
            else
            {
                wobScale.y = Mathf.Lerp(baseScale.y, baseScale.y * 0.95f, s) + w * 0.08f;
                wobScale.x = Mathf.Lerp(baseScale.x, baseScale.x * 1.05f, s) - w * 0.06f;
            }

            visual.localScale = wobScale;
            visual.localPosition = Vector3.Lerp(targetPos, basePos, 1f - decay);
            yield return null;
        }

        visual.localScale = baseScale;
        visual.localPosition = basePos;
        routine = null;
    }
}
