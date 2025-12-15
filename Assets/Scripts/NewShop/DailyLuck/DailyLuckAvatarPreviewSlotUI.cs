using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DailyLuckAvatarPreviewSlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image previewImage;          // la imagen que cambia
    [SerializeField] private TextMeshProUGUI stateText;   // "En propiedad" / "¡Nuevo!"

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.2f;

    private Coroutine fadeRoutine;

    public void SetInstant(Sprite sprite, string stateLabel)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = null;

        if (previewImage != null)
        {
            previewImage.sprite = sprite;
            previewImage.color = new Color(1, 1, 1, 1);
        }

        if (stateText != null) stateText.text = stateLabel;
    }

    public void SetWithFade(Sprite sprite, string stateLabel)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeSwap(sprite, stateLabel));
    }

    private IEnumerator FadeSwap(Sprite newSprite, string newLabel)
    {
        if (previewImage == null)
        {
            if (stateText != null) stateText.text = newLabel;
            yield break;
        }

        float t = 0f;
        Color c = previewImage.color;

        // Fade out
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(1f, 0f, t / fadeDuration);
            previewImage.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }

        // Swap
        previewImage.sprite = newSprite;
        if (stateText != null) stateText.text = newLabel;

        // Fade in
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(0f, 1f, t / fadeDuration);
            previewImage.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }

        previewImage.color = new Color(c.r, c.g, c.b, 1f);
        fadeRoutine = null;
    }
}
