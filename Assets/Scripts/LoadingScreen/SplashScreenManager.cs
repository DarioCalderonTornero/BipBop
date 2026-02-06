using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

#if UNITY_ANDROID && !UNITY_EDITOR
using Google.Play.AppUpdate;
using Google.Play.Common;
#endif

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

    [Header("Update Gate (Panel obligatorio)")]
    [SerializeField] private GameObject updatePanel;        // Desactivado por defecto
    [SerializeField] private RectTransform updatePanelRoot;  // El RectTransform que quieres “popear” (panel o hijo)
    [SerializeField] private Button openStoreButton;
    [SerializeField] private bool allowIfCheckFails = true;

    [Header("Pop (Código)")]
    [SerializeField] private float updatePopDuration = 0.28f;
    [SerializeField] private float updatePopOvershoot = 1.12f;
    [SerializeField] private float updatePopStartScale = 0.85f;

    [Header("Pulse (Texto update)")]
    [SerializeField] private TextMeshProUGUI updateHintText;   // el TMP que dice "Actualiza para continuar"
    [SerializeField] private float pulseScaleAmount = 0.06f;   // 6% aprox
    [SerializeField] private float pulsePeriod = 1.2f;         // segundos por ciclo (lento)

    private Coroutine pulseRoutine;



    private string[] loadingMessages =
    {
        "Cargando recursos...",
        "Inicializando entorno...",
        "Preparando sonidos...",
        "Cargando fondos...",
        "Optimizando shaders...",
        "Casi listo..."
    };

    [Header("Loading / Fade")]
    [SerializeField] private string nextSceneName = "Menu";

#if UNITY_ANDROID && !UNITY_EDITOR
    private AppUpdateManager appUpdateManager;
#endif

    private void Awake()
    {
        if (updatePanel != null)
            updatePanel.SetActive(false);

        if (openStoreButton != null)
            openStoreButton.onClick.AddListener(OpenPlayStorePage);

#if UNITY_ANDROID && !UNITY_EDITOR
        appUpdateManager = new AppUpdateManager();
#endif
    }

    private void Start()
    {
        // Estado inicial
        if (firstImage != null) firstImage.gameObject.SetActive(false);
        if (subtitleText != null) subtitleText.gameObject.SetActive(false);
        if (loadingGroup != null) loadingGroup.alpha = 0f;

        // Barra y % vacíos desde el inicio
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

        // Asegura vacío antes del fade
        if (loadingBar != null) loadingBar.SetFill01(0f);
        if (percentageText != null) percentageText.text = "0%";

        // 4) Fade-in del grupo de carga
        if (loadingGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(loadingGroup, 0f, 1f, 0.4f));

        // ✅ Mientras comprobamos updates, texto fijo y NO avanza la barra
        if (loadingText != null)
            loadingText.text = "Comprobando actualizaciones...";

        // 5) Gate de actualización (bloquea carga hasta decidir)
        bool updateNeeded = false;
        yield return StartCoroutine(CheckForUpdate(result => updateNeeded = result));

        if (updateNeeded)
        {
            // No cargamos nada: enseñamos panel y paramos aquí.
            ShowUpdatePanelPop();
            yield break;
        }

        // 6) Si no hay update, ya empieza el loading normal
        if (loadingText != null)
            loadingText.text = GetRandomMessage();

        yield return StartCoroutine(FakeLoading());

        yield return new WaitForSeconds(0.2f);
        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator CheckForUpdate(System.Action<bool> onResult)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var infoOp = appUpdateManager.GetAppUpdateInfo();
        yield return infoOp;

        if (!infoOp.IsSuccessful)
        {
            Debug.LogWarning($"[UpdateGate] GetAppUpdateInfo error: {infoOp.Error}");
            onResult?.Invoke(!allowIfCheckFails); // si NO permites fallo => fuerzas panel
            yield break;
        }

        var info = infoOp.GetResult();

        bool updateAvailable = info.UpdateAvailability == UpdateAvailability.UpdateAvailable;
        bool updateInProgress = info.UpdateAvailability == UpdateAvailability.DeveloperTriggeredUpdateInProgress;

        onResult?.Invoke(updateAvailable || updateInProgress);
        yield break;
#else
        // Editor / otras plataformas: no bloqueamos
        onResult?.Invoke(false);
        yield break;
#endif
    }

    private void ShowUpdatePanelPop()
    {
        if (updatePanel == null) return;

        updatePanel.SetActive(true);

        if (updateHintText != null && pulseRoutine == null)
            pulseRoutine = StartCoroutine(PulseText(updateHintText.rectTransform, pulseScaleAmount, pulsePeriod));

        if (updatePanelRoot != null)
            StartCoroutine(PopRect(updatePanelRoot, updatePopDuration, updatePopOvershoot, updatePopStartScale));
    }

    private IEnumerator PulseText(RectTransform target, float amount, float period)
    {
        Vector3 baseScale = target.localScale;
        float t = 0f;

        while (true)
        {
            t += Time.unscaledDeltaTime; // para que siga aunque pauses Time.timeScale
            float s = 1f + amount * Mathf.Sin(t * (2f * Mathf.PI / period));
            target.localScale = baseScale * s;
            yield return null;
        }
    }

    private IEnumerator PopRect(RectTransform target, float duration, float overshoot, float startScale)
    {
        // Estado inicial
        target.localScale = Vector3.one * startScale;

        float expandTime = duration * 0.7f;
        float settleTime = duration * 0.3f;

        Vector3 from = Vector3.one * startScale;
        Vector3 toOvershoot = Vector3.one * overshoot;
        Vector3 toFinal = Vector3.one;

        // Expande
        float t = 0f;
        while (t < expandTime)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / expandTime);
            float eased = Mathf.SmoothStep(0f, 1f, lerp);
            target.localScale = Vector3.LerpUnclamped(from, toOvershoot, eased);
            yield return null;
        }

        // Asienta
        t = 0f;
        while (t < settleTime)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / settleTime);
            float eased = Mathf.SmoothStep(0f, 1f, lerp);
            target.localScale = Vector3.LerpUnclamped(toOvershoot, toFinal, eased);
            yield return null;
        }

        target.localScale = Vector3.one;
    }

    private static void OpenPlayStorePage()
    {
        string pkg = Application.identifier;

#if UNITY_ANDROID && !UNITY_EDITOR
        Application.OpenURL("market://details?id=" + pkg);
#else
        Application.OpenURL("https://play.google.com/store/apps/details?id=" + pkg);
#endif
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

    private void OnDisable()
    {
        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }
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
