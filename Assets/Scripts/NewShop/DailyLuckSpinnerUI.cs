using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DailyLuckSpinnerUI : MonoBehaviour
{
    [Header("Overlay Panel (fade)")]
    [SerializeField] private GameObject overlayPanel;
    [SerializeField] private CanvasGroup overlayCanvasGroup;
    [SerializeField] private bool blockRaycastsWhileOpen = true;

    [Header("Reward Image")]
    [SerializeField] private Image rewardImage;

    [Header("Result UI")]
    [SerializeField] private CanvasGroup feedbackGroup;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private CanvasGroup confirmButtonGroup;
    [SerializeField] private Button confirmButton;

    [Header("Spin")]
    [SerializeField] private float startInterval = 0.04f;
    [SerializeField] private float endInterval = 0.18f;
    [SerializeField] private float spinDuration = 2.0f;
    [SerializeField] private AnimationCurve slowDownCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Overlay Fade")]
    [SerializeField] private float overlayFadeIn = 0.12f;
    [SerializeField] private float overlayFadeOut = 0.12f;

    [Header("Spin FX (scale + shake)")]
    [SerializeField] private float spinScaleMultiplier = 1.25f;
    [SerializeField] private float scaleUpSpeed = 10f;
    [SerializeField] private float shakeAmount = 10f;
    [SerializeField] private float shakeSpeed = 35f;

    [Header("Stop FX (pop back)")]
    [SerializeField] private float stopPopDuration = 0.10f;
    [SerializeField] private float stopPopOvershoot = 1.08f;

    [Header("After Stop (lift + show result)")]
    [SerializeField] private float liftUpPixels = 40f;
    [SerializeField] private float liftDuration = 0.10f;
    [SerializeField] private float resultFadeIn = 0.12f;

    public bool IsSpinning { get; private set; }
    public bool IsOpen { get; private set; }

    private Coroutine routine;
    private Action onConfirmClosed;

    private Vector3 originalScale;
    private Vector3 originalLocalPos;
    private RectTransform rewardRT;

    private void Awake()
    {
        if (rewardImage != null)
            rewardRT = rewardImage.rectTransform;

        if (rewardRT != null)
        {
            originalScale = rewardRT.localScale;
            originalLocalPos = rewardRT.localPosition;
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(CloseByUser);
        }

        HideOverlayInstant();
    }

    private void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;

        IsSpinning = false;
        IsOpen = false;
        onConfirmClosed = null;

        ResetRewardTransform();
        HideOverlayInstant();
    }

    /// <summary>
    /// feedbackMsg: el texto que quieres mostrar debajo al terminar (ej: "¡Nuevo fondo conseguido!" o "+75 monedas").
    /// onFinished: se llama JUSTO al terminar el spin (para que tu manager resuelva reward ahí).
    /// onClosed: se llama cuando el usuario pulsa "¡Genial!" y se cierra el overlay.
    /// </summary>
    public void PlaySpin(List<Sprite> spinSprites, Sprite finalSprite, string feedbackMsg, Action onFinished, Action onClosed = null)
    {

        if (rewardImage == null)
        {
            onFinished?.Invoke();
            onClosed?.Invoke();
            return;
        }

        if (spinSprites == null || spinSprites.Count == 0 || finalSprite == null)
        {
            rewardImage.sprite = finalSprite;
            onFinished?.Invoke();
            ShowResultUI(feedbackMsg, onClosed);
            return;
        }

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(SpinRoutine(spinSprites, finalSprite, feedbackMsg, onFinished, onClosed));
    }

    private IEnumerator SpinRoutine(List<Sprite> spinSprites, Sprite finalSprite, string feedbackMsg, Action onFinished, Action onClosed)
    {
        IsSpinning = true;
        IsOpen = true;
        onConfirmClosed = onClosed;

        // Overlay visible + fade in
        ShowOverlayInstant();
        ResetResultUIInstant();

        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = 0f;
            SetOverlayRaycasts(true);
            yield return FadeCanvas(overlayCanvasGroup, 0f, 1f, overlayFadeIn);
        }

        ResetRewardTransform();

        float startTime = Time.realtimeSinceStartup;
        float endTime = startTime + Mathf.Max(0.01f, spinDuration);

        int idx = 0;
        float shakeT = 0f;
        Vector3 targetScale = originalScale * spinScaleMultiplier;

        while (Time.realtimeSinceStartup < endTime)
        {
            float p = Mathf.InverseLerp(startTime, endTime, Time.realtimeSinceStartup);
            float eased = slowDownCurve.Evaluate(p);

            float interval = Mathf.Lerp(startInterval, endInterval, eased);
            interval = Mathf.Max(0.01f, interval);

            rewardImage.sprite = spinSprites[idx % spinSprites.Count];
            idx++;

            if (rewardRT != null)
            {
                rewardRT.localScale = Vector3.Lerp(
                    rewardRT.localScale,
                    targetScale,
                    Time.unscaledDeltaTime * scaleUpSpeed
                );

                shakeT += Time.unscaledDeltaTime * shakeSpeed;
                float sx = (Mathf.PerlinNoise(shakeT, 0.1f) - 0.5f) * 2f;
                float sy = (Mathf.PerlinNoise(0.1f, shakeT) - 0.5f) * 2f;
                rewardRT.localPosition = originalLocalPos + new Vector3(sx, sy, 0f) * shakeAmount;
            }

            yield return new WaitForSecondsRealtime(interval);
        }

        // Final sprite
        rewardImage.sprite = finalSprite;

        // Stop shake
        if (rewardRT != null)
            rewardRT.localPosition = originalLocalPos;

        // Pop back
        yield return StopPopToOriginal();

        // Lift up un poquito
        yield return LiftUp();

        IsSpinning = false;
        routine = null;

        // Aquí ya puedes resolver la recompensa (dar fondo / devolver monedas)
        onFinished?.Invoke();

        // Mostrar texto + botón con fade-in
        ShowResultUI(feedbackMsg, onClosed);
    }

    private IEnumerator LiftUp()
    {
        if (rewardRT == null) yield break;
        if (liftDuration <= 0f || Mathf.Approximately(liftUpPixels, 0f)) yield break;

        Vector3 from = originalLocalPos;
        Vector3 to = originalLocalPos + new Vector3(0f, liftUpPixels, 0f);

        float t = 0f;
        while (t < liftDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / liftDuration);
            rewardRT.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, p));
            yield return null;
        }

        rewardRT.localPosition = to;
    }

    private void ShowResultUI(string msg, Action onClosed)
    {
        onConfirmClosed = onClosed;

        if (feedbackText != null)
            feedbackText.text = msg ?? "";

        if (feedbackGroup != null)
            StartCoroutine(FadeCanvas(feedbackGroup, 0f, 1f, resultFadeIn));

        if (confirmButtonGroup != null)
            StartCoroutine(FadeCanvas(confirmButtonGroup, 0f, 1f, resultFadeIn));

        if (confirmButton != null)
            confirmButton.interactable = true;
    }

    private void ResetResultUIInstant()
    {
        if (feedbackGroup != null) { feedbackGroup.alpha = 0f; feedbackGroup.blocksRaycasts = false; feedbackGroup.interactable = false; }
        if (confirmButtonGroup != null) { confirmButtonGroup.alpha = 0f; confirmButtonGroup.blocksRaycasts = false; confirmButtonGroup.interactable = false; }
        if (confirmButton != null) confirmButton.interactable = false;
        if (feedbackText != null) feedbackText.text = "";
    }

    private void CloseByUser()
    {
        if (!IsOpen) return;
        if (IsSpinning) return; // no cerrar mientras gira

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(CloseRoutine());
    }

    private IEnumerator CloseRoutine()
    {
        // Bloquear botón mientras cerramos
        if (confirmButton != null) confirmButton.interactable = false;

        // Fade out result UI rápido (opcional)
        if (feedbackGroup != null) yield return FadeCanvas(feedbackGroup, feedbackGroup.alpha, 0f, 0.08f);
        if (confirmButtonGroup != null) yield return FadeCanvas(confirmButtonGroup, confirmButtonGroup.alpha, 0f, 0.08f);

        // Fade out overlay
        if (overlayCanvasGroup != null)
            yield return FadeCanvas(overlayCanvasGroup, overlayCanvasGroup.alpha, 0f, overlayFadeOut);

        SetOverlayRaycasts(false);
        HideOverlayInstant();

        // Reset reward transform para próxima vez
        ResetRewardTransform();

        IsOpen = false;

        var cb = onConfirmClosed;
        onConfirmClosed = null;
        cb?.Invoke();

        routine = null;
    }

    private IEnumerator StopPopToOriginal()
    {
        if (rewardRT == null) yield break;

        float half = stopPopDuration * 0.5f;
        if (half <= 0f)
        {
            rewardRT.localScale = originalScale;
            yield break;
        }

        Vector3 from = rewardRT.localScale;
        Vector3 overshoot = originalScale * stopPopOvershoot;

        float t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / half);
            rewardRT.localScale = Vector3.Lerp(from, overshoot, p);
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / half);
            rewardRT.localScale = Vector3.Lerp(overshoot, originalScale, p);
            yield return null;
        }

        rewardRT.localScale = originalScale;
    }

    private IEnumerator FadeCanvas(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;

        if (duration <= 0f)
        {
            cg.alpha = to;
            yield break;
        }

        float t = 0f;
        cg.alpha = from;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        cg.alpha = to;

        // Si está visible, habilitamos raycasts (solo para los grupos de UI)
        bool visible = to > 0.9f;
        cg.blocksRaycasts = visible;
        cg.interactable = visible;
    }

    private void ResetRewardTransform()
    {
        if (rewardRT == null) return;
        rewardRT.localScale = originalScale;
        rewardRT.localPosition = originalLocalPos;
    }

    private void SetOverlayRaycasts(bool enabled)
    {
        if (overlayCanvasGroup == null) return;
        if (!blockRaycastsWhileOpen) enabled = false;

        overlayCanvasGroup.blocksRaycasts = enabled;
        overlayCanvasGroup.interactable = enabled;
    }

    private void ShowOverlayInstant()
    {
        if (overlayPanel != null) overlayPanel.SetActive(true);
        if (overlayCanvasGroup != null) overlayCanvasGroup.alpha = 1f;
        SetOverlayRaycasts(true);
    }

    private void HideOverlayInstant()
    {
        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.blocksRaycasts = false;
            overlayCanvasGroup.interactable = false;
        }
        if (overlayPanel != null) overlayPanel.SetActive(false);

        ResetResultUIInstant();
    }
}
