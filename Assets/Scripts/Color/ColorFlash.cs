using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ColorFlash : MonoBehaviour
{
    private Graphic graphic; // funciona tanto con Image como TextMeshProUGUI

    private Color originalColor;
    private Coroutine flashRoutine;

    private void Awake()
    {
        graphic = GetComponent<Graphic>();
        if (graphic != null)
            originalColor = graphic.color;
    }

    public void Flash(Color flashColor, float fadeDuration = 0.15f)
    {
        if (graphic == null) return;

        // Si ya está animando, la reiniciamos
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine(flashColor, fadeDuration));
    }

    private IEnumerator FlashRoutine(Color flashColor, float fadeDuration)
    {
        float t = 0f;

        // Fade IN hacia flashColor
        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            graphic.color = Color.Lerp(originalColor, flashColor, t);
            yield return null;
        }

        // Fade OUT de vuelta al original
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            graphic.color = Color.Lerp(flashColor, originalColor, t);
            yield return null;
        }

        graphic.color = originalColor;
        flashRoutine = null;
    }
}
