using System.Collections;
using UnityEngine;

public class ColorOptionButtonFX : MonoBehaviour
{
    [SerializeField] private float popScale = 1.15f;
    [SerializeField] private float popDuration = 0.12f;

    [SerializeField] private float shakeScaleAmount = 0.05f; // 5% de escala
    [SerializeField] private float shakeDuration = 0.15f;

    RectTransform rt;
    Vector3 baseScale;

    Coroutine popRoutine;
    Coroutine shakeRoutine;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        baseScale = rt.localScale;
    }

    public void PlayPop()
    {
        if (popRoutine != null) StopCoroutine(popRoutine);
        popRoutine = StartCoroutine(PopRoutine());
    }

    public void PlayShake()
    {
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    IEnumerator PopRoutine()
    {
        float half = popDuration * 0.5f;
        float t = 0f;

        while (t < half)
        {
            float k = t / half;
            rt.localScale = Vector3.Lerp(baseScale, baseScale * popScale, k);
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            float k = t / half;
            rt.localScale = Vector3.Lerp(baseScale * popScale, baseScale, k);
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        rt.localScale = baseScale;
        popRoutine = null;
    }

    IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float k = elapsed / shakeDuration;
            float damper = 1f - k;

            float randX = (Random.value * 2f - 1f) * shakeScaleAmount * damper;
            float randY = (Random.value * 2f - 1f) * shakeScaleAmount * damper;

            rt.localScale = baseScale + new Vector3(randX, randY, 0f);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        rt.localScale = baseScale;
        shakeRoutine = null;
    }
}
