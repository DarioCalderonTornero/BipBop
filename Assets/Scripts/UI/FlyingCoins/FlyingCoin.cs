using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingCoin : MonoBehaviour
{
    private RectTransform rectTransform;
    private Vector3 startPos;
    private Vector3 scatterTarget;
    private Vector3 finalTarget;
    private float scatterDuration = 0.8f;
    private float flyDuration = 0.8f;
    private System.Action onArrive;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(Vector3 center, Vector3 target, float scatterRadius, System.Action onArriveCallback)
    {
        startPos = center;
        finalTarget = target;
        onArrive = onArriveCallback;

        rectTransform.position = startPos;

        // posición aleatoria a la que "explota"
        Vector2 randomDir = Random.insideUnitCircle.normalized * Random.Range(scatterRadius * 0.5f, scatterRadius);
        scatterTarget = startPos + (Vector3)randomDir;

        StartCoroutine(ScatterThenFly());
    }

    private IEnumerator ScatterThenFly()
    {
        // Fase 1: dispersión
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / scatterDuration;
            rectTransform.position = Vector3.Lerp(startPos, scatterTarget, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }

        yield return new WaitForSeconds(0.4f); // pausa breve

        // Fase 2: vuelo al objetivo
        Vector3 scatterStart = rectTransform.position;
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / flyDuration;
            rectTransform.position = Vector3.Lerp(scatterStart, finalTarget, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }

        onArrive?.Invoke();
        Destroy(gameObject);
    }
}
