using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SplashScreenManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI gameTitle;
    [SerializeField] private TextMeshProUGUI subtitleText;   // ← segundo texto
    [SerializeField] private Image progressBarFill;
    [SerializeField] private TextMeshProUGUI percentageText;
    [SerializeField] private TextMeshProUGUI loadingText;    // frases tipo "Cargando..."
    [SerializeField] private CanvasGroup loadingGroup;       // grupo de barra + % + texto

    [Header("Intro Animation")]
    [SerializeField] private float popDuration = 0.35f;
    [SerializeField] private float popOvershoot = 1.15f;
    [SerializeField] private float delayBetweenTexts = 0.4f;     // ⬅️ AQUÍ AJUSTAS LA ESPERA
    [SerializeField] private float delayBeforeLoadingUI = 0.3f;  // espera antes de mostrar barra

    [Header("Loading / Fade")]
    [SerializeField] private float fakeLoadDuration = 4f;
    [SerializeField] private string nextSceneName = "Menu";
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private Image fadeImage;

    private string[] loadingMessages =
    {
        "Cargando recursos...",
        "Inicializando entorno...",
        "Preparando sonidos...",
        "Cargando fondos...",
        "Optimizando shaders...",
        "Ajustando luces...",
        "Casi listo..."
    };

    private void Start()
    {
        // Ocultamos elementos al inicio
        if (loadingGroup != null) loadingGroup.alpha = 0f;
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = new Color(0, 0, 0, 0);
        }

        // Empieza la secuencia completa
        StartCoroutine(SplashSequence());
    }

    private IEnumerator SplashSequence()
    {
        // Ocultar textos al principio
        if (gameTitle != null) gameTitle.gameObject.SetActive(false);
        if (subtitleText != null) subtitleText.gameObject.SetActive(false);

        // 1) Animar título
        if (gameTitle != null)
            yield return StartCoroutine(AnimatePop(gameTitle));

        // 2) Espera configurable entre primer y segundo texto
        yield return new WaitForSeconds(delayBetweenTexts);

        // 3) Animar subtítulo
        if (subtitleText != null)
            yield return StartCoroutine(AnimatePop(subtitleText));

        // 4) Pequeña espera antes de mostrar barra de carga
        yield return new WaitForSeconds(delayBeforeLoadingUI);

        // 5) Fade-in de barra + porcentaje + texto de estado
        if (loadingGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(loadingGroup, 0f, 1f, 0.4f));
        }

        if (loadingText != null)
            loadingText.text = GetRandomMessage();

        // 6) Empezar la carga falsa
        StartCoroutine(FakeLoading());
    }

    private IEnumerator AnimatePop(TextMeshProUGUI target)
    {
        target.gameObject.SetActive(true);

        // Escala original definida en el editor (NO la tocamos permanentemente)
        Vector3 originalScale = target.transform.localScale;
        Vector3 startScale = originalScale * 0.1f;         // lejos/pequeño
        Vector3 overshootScale = originalScale * popOvershoot;

        float expandTime = popDuration * 0.7f;
        float settleTime = popDuration * 0.3f;

        // Empezar pequeño
        target.transform.localScale = startScale;

        // 1) Expandir hasta overshoot
        float t = 0f;
        while (t < expandTime)
        {
            t += Time.deltaTime;
            float lerp = t / expandTime;
            target.transform.localScale =
                Vector3.Lerp(startScale, overshootScale, Mathf.SmoothStep(0, 1, lerp));
            yield return null;
        }

        // 2) Volver a su escala original (la del editor)
        t = 0f;
        while (t < settleTime)
        {
            t += Time.deltaTime;
            float lerp = t / settleTime;
            target.transform.localScale =
                Vector3.Lerp(overshootScale, originalScale, Mathf.SmoothStep(0, 1, lerp));
            yield return null;
        }

        target.transform.localScale = originalScale;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float t = 0f;
        cg.alpha = from;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            cg.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        cg.alpha = to;
    }

    // El FakeLoading y FadeOut los puedes dejar tal como los tenías
    private IEnumerator FakeLoading()
    {
        float progress = 0f;
        float nextTarget = Random.Range(0.1f, 0.3f);

        while (progress < 1f)
        {
            progress = Mathf.MoveTowards(progress, nextTarget, Time.deltaTime * 0.3f);
            UpdateProgressUI(progress);

            if (loadingText != null && Random.value < 0.01f)
                loadingText.text = GetRandomMessage();

            if (Mathf.Abs(progress - nextTarget) < 0.001f)
            {
                yield return new WaitForSeconds(Random.Range(0.1f, 0.4f));
                nextTarget += Random.Range(0.1f, 0.25f);
                nextTarget = Mathf.Min(nextTarget, 1f);
            }

            yield return null;
        }

        yield return new WaitForSeconds(0.5f);
        StartCoroutine(FadeOut());
    }

    private void UpdateProgressUI(float progress)
    {
        if (progressBarFill != null)
            progressBarFill.fillAmount = progress;

        if (percentageText != null)
            percentageText.text = Mathf.RoundToInt(progress * 100f) + "%";
    }

    private IEnumerator FadeOut()
    {
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            if (fadeImage != null)
                fadeImage.color = new Color(0, 0, 0, t / fadeDuration);
            yield return null;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private string GetRandomMessage()
    {
        return loadingMessages[Random.Range(0, loadingMessages.Length)];
    }
}
