using System;
using System.Collections;
using TMPro;
using UnityEngine;

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

        // 3,2,1
        yield return PlayToken("3", numberDuration);
        yield return PlayToken("2", numberDuration);
        yield return PlayToken("1", numberDuration);

        // GO
        yield return PlayToken("GO!", goDuration);

        if (root != null) root.SetActive(false);

        routine = null;
        onFinished?.Invoke();
    }

    private IEnumerator PlayToken(string token, float duration)
    {
        if (countdownText != null)
            countdownText.text = token;

        // Disparamos SIEMPRE la misma animación
        if (animator != null && !string.IsNullOrEmpty(triggerName))
            animator.SetTrigger(triggerName);

        // Espera en tiempo real (independiente de Time.timeScale)
        yield return new WaitForSecondsRealtime(duration);
    }
}
