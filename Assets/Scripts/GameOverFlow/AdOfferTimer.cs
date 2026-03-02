using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdOfferTimer : MonoBehaviour
{
    public event Action OnExpired;

    [Header("Visual Fill")]
    [SerializeField] private Image fillImage;
    [SerializeField] private float duration = 5f;

    [Header("Seconds Number")]
    [SerializeField] private TextMeshProUGUI secondsText;           // NUEVO
    [SerializeField] private RectTransform secondsTextTransform;     // NUEVO (para pop)
    [SerializeField] private float popScale = 1.15f;                // NUEVO
    [SerializeField] private float popInTime = 0.07f;               // NUEVO
    [SerializeField] private float popOutTime = 0.12f;              // NUEVO

    private float timer;
    private bool running;

    private int lastWholeSeconds = -1;
    private Coroutine popRoutine;
    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        if (secondsTextTransform != null)
            baseScale = secondsTextTransform.localScale;
    }

    public void Begin()
    {
        timer = duration;
        running = true;

        if (fillImage != null)
            fillImage.fillAmount = 1f;

        // Inicializa texto y fuerza "pop" inicial opcional
        lastWholeSeconds = Mathf.CeilToInt(timer);
        SetSecondsText(lastWholeSeconds, doPop: true);
    }

    public void Stop()
    {
        running = false;
    }

    private void Update()
    {
        if (!running) return;

        // IMPORTANT: unscaled para que funcione aunque el juego esté pausado
        timer -= Time.unscaledDeltaTime;

        if (fillImage != null)
            fillImage.fillAmount = Mathf.Clamp01(timer / duration);

        int wholeSeconds = Mathf.CeilToInt(timer);

        // Cambió el segundo -> actualiza número y "pop"
        if (wholeSeconds != lastWholeSeconds)
        {
            lastWholeSeconds = wholeSeconds;
            SetSecondsText(Mathf.Max(wholeSeconds, 0), doPop: true);
        }

        if (timer <= 0f)
        {
            running = false;

            if (fillImage != null)
                fillImage.fillAmount = 0f;

            SetSecondsText(0, doPop: false);
            OnExpired?.Invoke();
        }
    }

    private void SetSecondsText(int seconds, bool doPop)
    {
        if (secondsText != null)
            secondsText.text = seconds.ToString();

        if (doPop)
            PlayPop();
    }

    private void PlayPop()
    {
        if (secondsTextTransform == null) return;

        if (popRoutine != null)
            StopCoroutine(popRoutine);

        popRoutine = StartCoroutine(PopRoutine());
    }

    private IEnumerator PopRoutine()
    {
        // Escala rápida arriba
        float t = 0f;
        Vector3 target = baseScale * popScale;

        while (t < popInTime)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / popInTime);
            secondsTextTransform.localScale = Vector3.Lerp(baseScale, target, a);
            yield return null;
        }

        // Vuelve a normal
        t = 0f;
        while (t < popOutTime)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / popOutTime);
            secondsTextTransform.localScale = Vector3.Lerp(target, baseScale, a);
            yield return null;
        }

        secondsTextTransform.localScale = baseScale;
        popRoutine = null;
    }
}