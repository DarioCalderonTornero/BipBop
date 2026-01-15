using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SplashScreenManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image firstImage;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private LoadingBarScrollFill loadingBar;
    [SerializeField] private TextMeshProUGUI percentageText;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private CanvasGroup loadingGroup;

    [Header("Intro Animation")]
    [SerializeField] private float popDuration = 0.35f;
    [SerializeField] private float popOvershoot = 1.15f;
    [SerializeField] private float delayBetweenTexts = 0.25f;
    [SerializeField] private float delayBeforeLoadingUI = 0.25f;

    [Header("Loading / Fade")]
    [SerializeField] private string nextSceneName = "Menu";

    private string[] loadingMessages =
    {
        "Cargando recursos...",
        "Inicializando entorno...",
        "Preparando sonidos...",
        "Cargando fondos...",
        "Optimizando shaders...",
        "Casi listo..."
    };

    private void Start()
    {
        // Estado inicial
        if (firstImage != null) firstImage.gameObject.SetActive(false);
        if (subtitleText != null) subtitleText.gameObject.SetActive(false);

        if (loadingGroup != null) loadingGroup.alpha = 0f;

        // ✅ Barra y % vacíos desde el inicio
        if (loadingBar != null) loadingBar.SetFill01(0f);
        if (percentageText != null) percentageText.text = "0%";

        StartCoroutine(SplashSequence());
    }

    private IEnumerator SplashSequence()
    {
        // 1) Pop imagen
        if (firstImage != null)
            yield return StartCoroutine(AnimatePop(firstImage.rectTransform));

        // 2) Pop subtitle
        yield return new WaitForSeconds(delayBetweenTexts);

        if (subtitleText != null)
            yield return StartCoroutine(AnimatePop(subtitleText.rectTransform));

        // 3) Espera antes del loading
        yield return new WaitForSeconds(delayBeforeLoadingUI);

        // ✅ asegúrate de que justo ANTES del fade sigue vacío
        if (loadingBar != null) loadingBar.SetFill01(0f);
        if (percentageText != null) percentageText.text = "0%";

        // 4) Fade-in del grupo de carga
        if (loadingGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(loadingGroup, 0f, 1f, 0.4f));

        if (loadingText != null)
            loadingText.text = GetRandomMessage();

        // 5) Empieza el fake loading (ahora sí se verá subir)
        yield return StartCoroutine(FakeLoading());

        // extra y fade out
        yield return new WaitForSeconds(0.2f); 
        SceneManager.LoadScene(nextSceneName);
    }

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
    }

    private void UpdateProgressUI(float progress)
    {
        if (loadingBar != null)
            loadingBar.SetFill01(progress);

        if (percentageText != null)
            percentageText.text = Mathf.RoundToInt(progress * 100f) + "%";
    }

    private IEnumerator AnimatePop(RectTransform target)
    {
        target.gameObject.SetActive(true);

        Vector3 originalScale = target.localScale;
        Vector3 startScale = originalScale * 0.1f;
        Vector3 overshootScale = originalScale * popOvershoot;

        float expandTime = popDuration * 0.7f;
        float settleTime = popDuration * 0.3f;

        target.localScale = startScale;

        float t = 0f;
        while (t < expandTime)
        {
            t += Time.deltaTime;
            float lerp = t / expandTime;
            target.localScale = Vector3.Lerp(startScale, overshootScale, Mathf.SmoothStep(0, 1, lerp));
            yield return null;
        }

        t = 0f;
        while (t < settleTime)
        {
            t += Time.deltaTime;
            float lerp = t / settleTime;
            target.localScale = Vector3.Lerp(overshootScale, originalScale, Mathf.SmoothStep(0, 1, lerp));
            yield return null;
        }

        target.localScale = originalScale;
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

    private string GetRandomMessage()
    {
        return loadingMessages[Random.Range(0, loadingMessages.Length)];
    }
}
