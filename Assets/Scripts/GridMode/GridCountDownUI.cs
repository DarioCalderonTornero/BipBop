using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.Localization;

public class GridCountDownUI : MonoBehaviour
{
    public static GridCountDownUI Instance { get; private set; }

    public TextMeshProUGUI countDownText;

    [SerializeField] private Animator myAnimator;

    [Header("Localization")]
    public LocalizedString goMessage;

    [Header("Audio")]
    [SerializeField] private AudioClip countdownAudioClip; // Sonido para 3, 2, 1
    [SerializeField] private float countdownPitch = 1.5f;  // Pitch agudo para los números
    [SerializeField] private AudioClip goAudioClip;        // Sonido distinto para el GO!

    private bool isCustomMessage = false;
    private int lastSecondPlayed = -1; // Evita que suene múltiples veces por segundo

    // ✅ NUEVO: mantiene el texto visible durante Countdown aunque alguien lo apague
    private bool forceVisibleDuringCountdown = false;

    private void Awake()
    {
        Instance = this;
        if (myAnimator == null) myAnimator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (GridState.Instance == null) return;

        // ✅ Garantiza visibilidad durante Countdown
        if (forceVisibleDuringCountdown &&
            GridState.Instance.gridGameState == GridState.GridGameStateEnum.Countdown)
        {
            if (!countDownText.gameObject.activeSelf)
                countDownText.gameObject.SetActive(true);

            if (myAnimator != null)
                myAnimator.SetBool("IsCountDown", true);
        }

        if (isCustomMessage) return;

        float t = GridState.Instance.GetCountDownTimer();
        if (t > 0f)
        {
            int currentSecond = Mathf.CeilToInt(t);
            countDownText.text = currentSecond.ToString();

            // --- NUEVO: Magia de Audio ---
            if (currentSecond != lastSecondPlayed && currentSecond > 0)
            {
                lastSecondPlayed = currentSecond;
                if (countdownAudioClip != null && SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySound(countdownAudioClip, 1f, countdownPitch);
                }
            }
        }
    }

    public void Show()
    {
        isCustomMessage = false;
        forceVisibleDuringCountdown = true;
        countDownText.gameObject.SetActive(true);
        lastSecondPlayed = -1; // Reseteamos el tracker de sonido

        if (myAnimator != null)
            myAnimator.SetBool("IsCountDown", true);
    }

    public void Hide()
    {
        forceVisibleDuringCountdown = false;
        countDownText.gameObject.SetActive(false);
        isCustomMessage = false;
    }

    public void ShowMessage(string message)
    {
        isCustomMessage = true;
        countDownText.gameObject.SetActive(true);
        countDownText.text = message;
    }

    public void ShowMessage(LocalizedString localizedMessage)
    {
        isCustomMessage = true;
        countDownText.gameObject.SetActive(true);
        countDownText.text = localizedMessage.GetLocalizedString();
    }

    public void ShowGo(float duration = 0.7f)
    {
        StartCoroutine(ShowGoRoutine(duration));
    }

    private IEnumerator ShowGoRoutine(float duration)
    {
        isCustomMessage = true;
        forceVisibleDuringCountdown = false;

        countDownText.gameObject.SetActive(true);
        countDownText.text = goMessage.GetLocalizedString();

        // --- NUEVO: Sonido final distinto para el GO! ---
        if (goAudioClip != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(goAudioClip, 1f, 1f);
        }

        yield return new WaitForSeconds(duration);

        if (myAnimator != null)
        {
            myAnimator.SetBool("IsCountDown", false);
            myAnimator.SetBool("CountDownFinish", true);
        }

        countDownText.gameObject.SetActive(false);
        isCustomMessage = false;

        if (GridState.Instance != null)
            GridState.Instance.StartGameAfterGo();
    }
}