using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    public Image fadeImage; // Imagen para el efecto de fade
    public float fadeDuration = 1.0f; // Duración de la transición
    public string targetScene = "GameScene"; // Nombre de la escena objetivo
    public RectTransform[] excludedUIElements; // Elementos de UI a excluir del toque

    private bool isTransitioning = false;

    private void Update()
    {
        if (Input.touchCount > 0 && !isTransitioning)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                // Verificar si el toque está fuera de los elementos excluidos
                if (!IsTouchOverExcludedUI(touch))
                {
                    StartCoroutine(FadeOutAndLoadScene());
                }
            }
        }
    }

    private bool IsTouchOverExcludedUI(Touch touch)
    {
        Vector2 touchPosition = touch.position;

        foreach (RectTransform rectTransform in excludedUIElements)
        {
            // Convertir la posición del toque a las coordenadas locales del rectTransform
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                touchPosition,
                Camera.main,
                out localPoint
            );

            // Comprobar si el punto está dentro del rectángulo del rectTransform
            if (rectTransform.rect.Contains(localPoint))
            {
                return true; // Está tocando un elemento excluido
            }
        }

        return false; // No está tocando ningún elemento excluido
    }

    private IEnumerator FadeOutAndLoadScene()
    {
        isTransitioning = true;

        // Fade out: pantalla a negro
        yield return StartCoroutine(Fade(0f, 1f));

        // Cargar la nueva escena
        SceneManager.LoadScene(targetScene);

        // Esperar un frame para que la escena cargue
        yield return null;

        // Fade in: pantalla de negro a visible
        yield return StartCoroutine(Fade(1f, 0f));
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float timer = 0f;

        // Progresión de alfa
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, timer / fadeDuration);

            SetImageAlpha(alpha);

            yield return null;
        }

        SetImageAlpha(endAlpha);
    }

    private void SetImageAlpha(float alpha)
    {
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
        }
    }
}
