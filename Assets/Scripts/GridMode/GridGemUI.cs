using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GridGemUI : MonoBehaviour
{
    [Header("Bag")]
    [SerializeField] private RectTransform bagRect;
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeStrength = 10f; // píxeles

    [Header("Popup +1")]
    [SerializeField] private TextMeshProUGUI plusOneText; // objeto de UI al lado del saco
    [SerializeField] private float popupDuration = 0.6f;
    [SerializeField] private float popupMoveY = 40f;

    Vector3 bagOriginalPos;
    Coroutine shakeRoutine;
    Coroutine popupRoutine;

    void Awake()
    {
        if (bagRect != null)
            bagOriginalPos = bagRect.anchoredPosition;

        if (plusOneText != null)
        {
            var c = plusOneText.color;
            c.a = 0f;
            plusOneText.color = c;
        }
    }

    public void PlayGemCollected()
    {
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        if (popupRoutine != null) StopCoroutine(popupRoutine);

        shakeRoutine = StartCoroutine(ShakeBag());
        popupRoutine = StartCoroutine(PlayPlusOne());
    }

    IEnumerator ShakeBag()
    {
        if (bagRect == null) yield break;

        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float t = elapsed / shakeDuration;

            // pequeño shake aleatorio alrededor de la posición original
            float offsetX = Random.Range(-1f, 1f) * shakeStrength;
            float offsetY = Random.Range(-1f, 1f) * shakeStrength;

            bagRect.anchoredPosition = bagOriginalPos + new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        bagRect.anchoredPosition = bagOriginalPos;
    }

    IEnumerator PlayPlusOne()
    {
        if (plusOneText == null) yield break;

        // estado inicial
        Vector3 startPos = plusOneText.rectTransform.anchoredPosition;
        Vector3 endPos = startPos + new Vector3(0f, popupMoveY, 0f);

        float elapsed = 0f;

        while (elapsed < popupDuration)
        {
            float t = elapsed / popupDuration;

            // mover hacia arriba
            plusOneText.rectTransform.anchoredPosition = Vector3.Lerp(startPos, endPos, t);

            // fade in/out suave (0 -> 1 -> 0)
            float alpha = t <= 0.5f ? t * 2f : (1f - t) * 2f;
            Color c = plusOneText.color;
            c.a = alpha;
            plusOneText.color = c;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // reset
        plusOneText.rectTransform.anchoredPosition = startPos;
        Color cEnd = plusOneText.color;
        cEnd.a = 0f;
        plusOneText.color = cEnd;
    }
}
