using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SplashScreenManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI gameTitle;
    public TextMeshProUGUI loadingText;
    public Image progressBarFill;
    public TextMeshProUGUI percentageText;
    public Image fadeImage;

    [Header("Settings")]
    public float titlePulseSpeed = 1.5f;
    public float fakeLoadDuration = 4f;
    public string nextSceneName = "Menu";
    public float fadeDuration = 0.5f;
    public float messageChangeInterval = 1.3f; // tiempo entre frases

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
        // Asegurar fade inicial transparente
        if (fadeImage)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = new Color(0, 0, 0, 0);
        }

        if (loadingText)
            loadingText.text = GetRandomMessage();

        StartCoroutine(AnimateTitle());
        StartCoroutine(FakeLoading());
    }

    private IEnumerator FadeOut()
    {
        fadeImage.gameObject.SetActive(true);
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeImage.color = new Color(0, 0, 0, t / fadeDuration);
            yield return null;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator AnimateTitle()
    {
        Vector3 originalScale = gameTitle.transform.localScale;
        Vector3 targetScale = originalScale * 1.1f;
        float t = 0;

        while (true)
        {
            t += Time.unscaledDeltaTime * titlePulseSpeed;
            float scaleFactor = (Mathf.Sin(t) + 1f) / 2f;
            gameTitle.transform.localScale = Vector3.Lerp(originalScale, targetScale, scaleFactor);
            yield return null;
        }
    }

    private IEnumerator FakeLoading()
    {
        float progress = 0f;
        float nextTarget = Random.Range(0.1f, 0.3f);
        float nextMessageTime = messageChangeInterval;

        while (progress < 1f)
        {
            // Progresión suave hacia la siguiente meta
            progress = Mathf.MoveTowards(progress, nextTarget, Time.deltaTime * 0.3f);
            UpdateProgressUI(progress);

            // Cambiar frase de estado
            if (Time.timeSinceLevelLoad >= nextMessageTime)
            {
                loadingText.text = GetRandomMessage();
                nextMessageTime += messageChangeInterval;
            }

            // Cuando llega al target, crear un nuevo “salto” (pequeño escalón)
            if (Mathf.Abs(progress - nextTarget) < 0.001f)
            {
                yield return new WaitForSeconds(Random.Range(0.1f, 0.4f)); // pausa natural
                nextTarget += Random.Range(0.1f, 0.25f);
                nextTarget = Mathf.Min(nextTarget, 1f); // no pasar del 100%
            }

            yield return null;
        }

        // Pequeña pausa antes del fade
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(FadeOut());
    }

    private void UpdateProgressUI(float progress)
    {
        if (progressBarFill)
            progressBarFill.fillAmount = progress;

        if (percentageText)
            percentageText.text = Mathf.RoundToInt(progress * 100f) + "%";
    }

    private string GetRandomMessage()
    {
        return loadingMessages[Random.Range(0, loadingMessages.Length)];
    }
}
