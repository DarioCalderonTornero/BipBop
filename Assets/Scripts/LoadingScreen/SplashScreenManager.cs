using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

#if UNITY_ANDROID && !UNITY_EDITOR
using Google.Play.AppUpdate;
using Google.Play.Common;
#endif

public class SplashScreenManager : MonoBehaviour
{
    [Header("Fake Loading Duration")]
    [SerializeField] private float fakeLoadingDuration = 4f;

    [Header("Fake Loading Randomness")]
    [SerializeField] private float fakeLoadingMoveSpeed = 0.35f;
    [SerializeField] private float minPauseBetweenJumps = 0.10f;
    [SerializeField] private float maxPauseBetweenJumps = 0.40f;
    [SerializeField] private float minJumpAmount = 0.08f;
    [SerializeField] private float maxJumpAmount = 0.22f;
    [SerializeField] private float maxLeadOverTime = 0.18f;  
    [SerializeField] private float maxLagBehindTime = 0.08f;

    [Header("UI References")]
    [SerializeField] private Image firstImage;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [SerializeField] private RectTransform loadingSpinner;
    [SerializeField] private CanvasGroup spinnerGroup;

    [SerializeField] private TextMeshProUGUI percentageText;
    [SerializeField] private CanvasGroup percentageGroup;

    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private CanvasGroup loadingTextGroup;

    [Header("Tips UI")]
    [SerializeField] private CanvasGroup tipsGroup;
    [SerializeField] private TextMeshProUGUI tapForTipText;
    [SerializeField] private TextMeshProUGUI tipText;

    [Header("Tips Localization")]
    [SerializeField] private LocalizedString tapForTipLocalizedText;   // "Toca para consejo"
    [SerializeField] private LocalizedString[] tipMessages;            // consejos

    [Header("Tips Animation")]
    [SerializeField] private float tipPopDuration = 0.18f;
    [SerializeField] private float tipPopOvershoot = 1.08f;

    [Header("Spinner")]
    [SerializeField] private float spinnerSpeed = 180f;
    [SerializeField] private bool rotateClockwise = true;

    [Header("Intro Animation")]
    [SerializeField] private float popDuration = 0.35f;
    [SerializeField] private float popOvershoot = 1.15f;
    [SerializeField] private float delayBetweenTexts = 0.25f;
    [SerializeField] private float delayBeforeLoadingUI = 0.25f;
    [SerializeField] private float loadingUIFadeDuration = 0.4f;

    [Header("Update Gate (Panel obligatorio)")]
    [SerializeField] private GameObject updatePanel;
    [SerializeField] private RectTransform updatePanelRoot;
    [SerializeField] private Button openStoreButton;
    [SerializeField] private bool allowIfCheckFails = true;

    [Header("Pop (Código)")]
    [SerializeField] private float updatePopDuration = 0.28f;
    [SerializeField] private float updatePopOvershoot = 1.12f;
    [SerializeField] private float updatePopStartScale = 0.85f;

    [Header("Pulse (Texto update)")]
    [SerializeField] private TextMeshProUGUI updateHintText;
    [SerializeField] private float pulseScaleAmount = 0.06f;
    [SerializeField] private float pulsePeriod = 1.2f;

    private Coroutine pulseRoutine;
    private Coroutine tipPopRoutine;
    private bool spinnerActive = false;
    private bool tipsInputEnabled = false;

    [Header("Localization (Splash)")]
    [SerializeField] private LocalizedString checkingUpdatesText;
    [SerializeField] private LocalizedString[] loadingMessages;

    private string[] cachedLoadingMessages;
    private string[] cachedTipMessages;
    private int currentTipIndex = -1;

    [Header("Loading / Fade")]
    [SerializeField] private string nextSceneName = "Menu";

    private Vector3 tipTextBaseScale;

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
        if (firstImage != null) firstImage.gameObject.SetActive(false);
        if (subtitleText != null) subtitleText.gameObject.SetActive(false);

        if (tipText != null)
            tipTextBaseScale = tipText.rectTransform.localScale;

        SetCanvasGroupAlpha(spinnerGroup, 0f);
        SetCanvasGroupAlpha(percentageGroup, 0f);
        SetCanvasGroupAlpha(loadingTextGroup, 0f);
        SetCanvasGroupAlpha(tipsGroup, 0f);

        if (loadingSpinner != null)
            loadingSpinner.localRotation = Quaternion.identity;

        if (percentageText != null)
            percentageText.text = "0%";

        if (tapForTipText != null)
            tapForTipText.text = "";

        if (tipText != null)
            tipText.text = "";

        StartCoroutine(InitLocalizationThenRun());
    }

    private void Update()
    {
        if (spinnerActive && loadingSpinner != null)
        {
            float direction = rotateClockwise ? -1f : 1f;
            loadingSpinner.Rotate(0f, 0f, spinnerSpeed * direction * Time.deltaTime);
        }

        if (tipsInputEnabled && WasScreenTapped())
        {
            ShowNextTip();
        }
    }

    private bool WasScreenTapped()
    {
#if UNITY_EDITOR
        return Input.GetMouseButtonDown(0);
#else
        return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
#endif
    }

    private IEnumerator InitLocalizationThenRun()
    {
        yield return LocalizationSettings.InitializationOperation;

        cachedLoadingMessages = new string[loadingMessages.Length];
        for (int i = 0; i < loadingMessages.Length; i++)
        {
            var op = loadingMessages[i].GetLocalizedStringAsync();
            yield return op;
            cachedLoadingMessages[i] = op.Result;
        }

        if (tapForTipLocalizedText != null)
        {
            var tapOp = tapForTipLocalizedText.GetLocalizedStringAsync();
            yield return tapOp;

            if (tapForTipText != null)
                tapForTipText.text = tapOp.Result;
        }

        cachedTipMessages = new string[tipMessages.Length];
        for (int i = 0; i < tipMessages.Length; i++)
        {
            var op = tipMessages[i].GetLocalizedStringAsync();
            yield return op;
            cachedTipMessages[i] = op.Result;
        }

        SetInitialTip();

        StartCoroutine(SplashSequence());
    }

    private void SetInitialTip()
    {
        if (tipText == null || cachedTipMessages == null || cachedTipMessages.Length == 0)
            return;

        currentTipIndex = Random.Range(0, cachedTipMessages.Length);
        tipText.text = cachedTipMessages[currentTipIndex];
    }

    private IEnumerator SplashSequence()
    {
        if (firstImage != null)
            yield return StartCoroutine(AnimatePop(firstImage.rectTransform));

        yield return new WaitForSeconds(delayBetweenTexts);

        if (subtitleText != null)
            yield return StartCoroutine(AnimatePop(subtitleText.rectTransform));

        yield return new WaitForSeconds(delayBeforeLoadingUI);

        if (loadingSpinner != null)
            loadingSpinner.localRotation = Quaternion.identity;

        if (percentageText != null)
            percentageText.text = "0%";

        yield return StartCoroutine(FadeInLoadingUI());

        if (loadingText != null)
        {
            var op = checkingUpdatesText.GetLocalizedStringAsync();
            yield return op;
            loadingText.text = op.Result;
        }

        bool updateNeeded = false;
        yield return StartCoroutine(CheckForUpdate(result => updateNeeded = result));

        if (updateNeeded)
        {
            ShowUpdatePanelPop();
            yield break;
        }

        if (loadingText != null)
            loadingText.text = GetRandomMessage();

        spinnerActive = true;
        tipsInputEnabled = true;

        yield return StartCoroutine(FakeLoading());

        tipsInputEnabled = false;
        spinnerActive = false;

        yield return new WaitForSeconds(0.2f);
        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator FadeInLoadingUI()
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / loadingUIFadeDuration;
            float a = Mathf.Lerp(0f, 1f, t);

            SetCanvasGroupAlpha(spinnerGroup, a);
            SetCanvasGroupAlpha(percentageGroup, a);
            SetCanvasGroupAlpha(loadingTextGroup, a);
            SetCanvasGroupAlpha(tipsGroup, a);

            yield return null;
        }

        SetCanvasGroupAlpha(spinnerGroup, 1f);
        SetCanvasGroupAlpha(percentageGroup, 1f);
        SetCanvasGroupAlpha(loadingTextGroup, 1f);
        SetCanvasGroupAlpha(tipsGroup, 1f);
    }

    private void ShowNextTip()
    {
        if (tipText == null || cachedTipMessages == null || cachedTipMessages.Length == 0)
            return;

        int nextIndex = currentTipIndex;

        if (cachedTipMessages.Length == 1)
        {
            nextIndex = 0;
        }
        else
        {
            while (nextIndex == currentTipIndex)
                nextIndex = Random.Range(0, cachedTipMessages.Length);
        }

        if (tipPopRoutine != null)
        {
            StopCoroutine(tipPopRoutine);
            tipPopRoutine = null;
        }

        tipText.rectTransform.localScale = tipTextBaseScale;

        currentTipIndex = nextIndex;
        tipText.text = cachedTipMessages[currentTipIndex];

        tipPopRoutine = StartCoroutine(AnimateTipPop(tipText.rectTransform));
    }

    private IEnumerator AnimateTipPop(RectTransform target)
    {
        if (target == null) yield break;

        Vector3 originalScale = tipTextBaseScale;
        Vector3 overshootScale = originalScale * tipPopOvershoot;

        float expandTime = tipPopDuration * 0.6f;
        float settleTime = tipPopDuration * 0.4f;

        target.localScale = originalScale;

        float t = 0f;
        while (t < expandTime)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / expandTime);
            float eased = Mathf.SmoothStep(0f, 1f, lerp);
            target.localScale = Vector3.LerpUnclamped(originalScale, overshootScale, eased);
            yield return null;
        }

        t = 0f;
        while (t < settleTime)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / settleTime);
            float eased = Mathf.SmoothStep(0f, 1f, lerp);
            target.localScale = Vector3.LerpUnclamped(overshootScale, originalScale, eased);
            yield return null;
        }

        target.localScale = originalScale;
        tipPopRoutine = null;
    }

    private IEnumerator CheckForUpdate(System.Action<bool> onResult)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var infoOp = appUpdateManager.GetAppUpdateInfo();
        yield return infoOp;

        if (!infoOp.IsSuccessful)
        {
            Debug.LogWarning($"[UpdateGate] GetAppUpdateInfo error: {infoOp.Error}");
            onResult?.Invoke(!allowIfCheckFails);
            yield break;
        }

        var info = infoOp.GetResult();

        bool updateAvailable = info.UpdateAvailability == UpdateAvailability.UpdateAvailable;
        bool updateInProgress = info.UpdateAvailability == UpdateAvailability.DeveloperTriggeredUpdateInProgress;

        onResult?.Invoke(updateAvailable || updateInProgress);
        yield break;
#else
        onResult?.Invoke(false);
        yield break;
#endif
    }

    private void ShowUpdatePanelPop()
    {
        spinnerActive = false;
        tipsInputEnabled = false;

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
            t += Time.unscaledDeltaTime;
            float s = 1f + amount * Mathf.Sin(t * (2f * Mathf.PI / period));
            target.localScale = baseScale * s;
            yield return null;
        }
    }

    private IEnumerator PopRect(RectTransform target, float duration, float overshoot, float startScale)
    {
        target.localScale = Vector3.one * startScale;

        float expandTime = duration * 0.7f;
        float settleTime = duration * 0.3f;

        Vector3 from = Vector3.one * startScale;
        Vector3 toOvershoot = Vector3.one * overshoot;
        Vector3 toFinal = Vector3.one;

        float t = 0f;
        while (t < expandTime)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / expandTime);
            float eased = Mathf.SmoothStep(0f, 1f, lerp);
            target.localScale = Vector3.LerpUnclamped(from, toOvershoot, eased);
            yield return null;
        }

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
        float duration = Mathf.Max(0.1f, fakeLoadingDuration);

        float elapsed = 0f;
        float progress = 0f;

        float nextTarget = Random.Range(0.08f, 0.18f);
        float pauseTimer = 0f;
        bool waitingForNextJump = false;

        UpdateProgressUI(progress);

        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;

            float timeProgress = Mathf.Clamp01(elapsed / duration);

            // Límites para que nunca termine demasiado pronto
            float minAllowed = Mathf.Clamp01(timeProgress - maxLagBehindTime);
            float maxAllowed = (timeProgress < 0.95f)
                ? Mathf.Clamp01(timeProgress + maxLeadOverTime)
                : 1f;

            if (waitingForNextJump)
            {
                pauseTimer -= dt;

                if (pauseTimer <= 0f)
                {
                    waitingForNextJump = false;

                    float jumpAmount = Random.Range(minJumpAmount, maxJumpAmount);
                    float softCap = Mathf.Min(maxAllowed, 0.99f);

                    nextTarget = Mathf.Min(progress + jumpAmount, softCap);

                    // Asegura que siempre haya algo de avance visible
                    if (nextTarget <= progress + 0.005f)
                        nextTarget = Mathf.Min(progress + 0.02f, softCap);
                }
            }
            else
            {
                progress = Mathf.MoveTowards(progress, nextTarget, fakeLoadingMoveSpeed * dt);

                if (Mathf.Abs(progress - nextTarget) < 0.0001f)
                {
                    waitingForNextJump = true;
                    pauseTimer = Random.Range(minPauseBetweenJumps, maxPauseBetweenJumps);
                }
            }

            // Corrige el progreso para respetar la duración total
            progress = Mathf.Clamp(progress, minAllowed, maxAllowed);

            // Cambia el texto de carga aleatoriamente
            if (loadingText != null && Random.value < 0.01f)
                loadingText.text = GetRandomMessage();

            UpdateProgressUI(progress);
            yield return null;
        }

        // Remate final exacto
        progress = 1f;
        UpdateProgressUI(progress);
    }

    private void UpdateProgressUI(float progress)
    {
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

    private void SetCanvasGroupAlpha(CanvasGroup cg, float alpha)
    {
        if (cg == null) return;
        cg.alpha = alpha;
    }

    private void OnDisable()
    {
        spinnerActive = false;
        tipsInputEnabled = false;

        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }

        if (tipPopRoutine != null)
        {
            StopCoroutine(tipPopRoutine);
            tipPopRoutine = null;
        }
    }

    private string GetRandomMessage()
    {
        if (cachedLoadingMessages == null || cachedLoadingMessages.Length == 0)
            return "";

        return cachedLoadingMessages[Random.Range(0, cachedLoadingMessages.Length)];
    }
}