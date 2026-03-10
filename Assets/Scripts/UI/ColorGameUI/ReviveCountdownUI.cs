using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public class ReviveCountdownUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject root;            // Panel entero (para activar/desactivar)
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private Animator animator;

    [Header("Timing")]
    [SerializeField] private float numberDuration = 0.6f; // 3,2,1
    [SerializeField] private float goDuration = 0.7f;     // GO un pelín más largo
    [SerializeField] private string triggerName = "Pop";  // Trigger del Animator

    [Header("Audio")]
    [SerializeField] private AudioClip countdownAudioClip; // Sonido para 3, 2, 1
    [SerializeField] private float countdownPitch = 1.5f;  // "Más pitch" para hacer el tick más agudo
    [SerializeField] private AudioClip goAudioClip;        // NUEVO: Sonido distinto para el GO!

    [Header("Localization")]
    [SerializeField] private LocalizedString goMessage;

    private Coroutine routine;

    private void Awake()
    {
        if (root != null) root.SetActive(false);

        // Recomendado para que funcione aunque el juego esté "pausado"
        if (animator != null)
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    public void Play(Action onFinished)
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(CountdownRoutine(onFinished));
    }

    private IEnumerator CountdownRoutine(Action onFinished)
    {
        if (root != null) root.SetActive(true);

        // 3, 2, 1 (Usamos el clip normal con el pitch alto)
        yield return PlayToken("3", numberDuration, countdownAudioClip, countdownPitch);
        yield return PlayToken("2", numberDuration, countdownAudioClip, countdownPitch);
        yield return PlayToken("1", numberDuration, countdownAudioClip, countdownPitch);

        // GO (Usamos el clip nuevo con pitch normal = 1f)
        yield return PlayToken(goMessage.GetLocalizedString(), goDuration, goAudioClip, 1f);

        if (root != null) root.SetActive(false);

        routine = null;
        onFinished?.Invoke();
    }

    // NUEVO: Ahora le pasamos el audio y el pitch específico que queremos que suene
    private IEnumerator PlayToken(string token, float duration, AudioClip clipToPlay, float pitch)
    {
        if (countdownText != null)
            countdownText.text = token;

        // Disparamos SIEMPRE la misma animación
        if (animator != null && !string.IsNullOrEmpty(triggerName))
            animator.SetTrigger(triggerName);

        // Reproducimos el sonido que nos hayan pasado por parámetro
        if (clipToPlay != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(clipToPlay, 1f, pitch);
        }

        // Espera en tiempo real (independiente de Time.timeScale)
        yield return new WaitForSecondsRealtime(duration);
    }
}