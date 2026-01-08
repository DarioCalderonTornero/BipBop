using System.Collections;
using UnityEngine;

public class MeteorCameraShake : MonoBehaviour
{
    public static MeteorCameraShake Instance;

    public float defaultDuration = 0.15f;
    public float defaultStrength = 0.3f;

    private Vector3 originalPos;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        Instance = this;
        originalPos = transform.localPosition;
    }

    public void Shake(float duration = -1f, float strength = -1f)
    {
        if (duration <= 0f) duration = defaultDuration;
        if (strength <= 0f) strength = defaultStrength;

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeCoroutine(duration, strength));
    }

    private IEnumerator ShakeCoroutine(float duration, float strength)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // para que funcione igual en cámara lenta

            Vector2 offset = Random.insideUnitCircle * strength;
            transform.localPosition = originalPos + new Vector3(offset.x, offset.y, 0f);

            yield return null;
        }

        transform.localPosition = originalPos;
        shakeRoutine = null;
    }
}
