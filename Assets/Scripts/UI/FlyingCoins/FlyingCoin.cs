using System;
using System.Collections;
using UnityEngine;

public class FlyingCoin : MonoBehaviour
{
    private RectTransform rectTransform;
    private Action onArrive;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// startPos/targetPos: posiciones en pantalla (RectTransform.position).
    /// burstDir: dirección del "disparo".
    /// burstForce: fuerza inicial (px/seg aprox, al ser UI).
    /// burstDuration: cuánto dura el disparo antes de agruparse.
    /// homingDuration: cuánto tarda en llegar al icono.
    /// </summary>
    public void Initialize(
        Vector3 startPos,
        Vector3 targetPos,
        Vector2 burstDir,
        float burstForce,
        float burstDuration,
        float homingDuration,
        Action onArriveCallback)
    {
        onArrive = onArriveCallback;

        StopAllCoroutines();
        rectTransform.position = startPos;

        StartCoroutine(BurstThenHome(startPos, targetPos, burstDir, burstForce, burstDuration, homingDuration));
    }

    private IEnumerator BurstThenHome(
        Vector3 startPos,
        Vector3 targetPos,
        Vector2 burstDir,
        float burstForce,
        float burstDuration,
        float homingDuration)
    {
        // -----------------------
        // 1) BURST (impulso)
        // -----------------------
        Vector3 pos = startPos;
        Vector3 vel = (Vector3)(burstDir.normalized * burstForce);

        float t = 0f;
        while (t < burstDuration)
        {
            t += Time.unscaledDeltaTime;

            // fricción ligera (si quieres aún más “disparo”, baja el 0.90 a 0.93-0.96)
            vel *= Mathf.Pow(0.90f, Time.unscaledDeltaTime * 60f);

            pos += vel * Time.unscaledDeltaTime;
            rectTransform.position = pos;

            yield return null;
        }

        Vector3 burstEnd = rectTransform.position;

        // -----------------------
        // 2) HOMING (curva al icono)
        // -----------------------
        Vector3 toTarget = targetPos - burstEnd;

        // Control point para que curve bonito
        Vector3 control = burstEnd + toTarget * 0.35f;
        control += new Vector3(UnityEngine.Random.Range(-35f, 35f), UnityEngine.Random.Range(70f, 120f), 0f);

        t = 0f;
        while (t < homingDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / homingDuration);

            // easing suave
            float eased = Mathf.SmoothStep(0f, 1f, p);

            rectTransform.position = QuadraticBezier(burstEnd, control, targetPos, eased);
            yield return null;
        }

        rectTransform.position = targetPos;

        onArrive?.Invoke();
        SoundManager.Instance.PlayFlyingCoinSound();
        Destroy(gameObject);
    }

    private static Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float u = 1f - t;
        return (u * u) * a + (2f * u * t) * b + (t * t) * c;
    }
}
