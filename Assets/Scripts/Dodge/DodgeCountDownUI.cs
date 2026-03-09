using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public class DodgeCountDownUI : MonoBehaviour
{
    public static DodgeCountDownUI Instance { get; private set; }

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

    // ✅ evita que se quede apagado por otros scripts / anim states
    [SerializeField] private bool forceVisibleDuringCountdown = true;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (myAnimator == null)
            myAnimator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (DodgeState.Instance == null) return;

        // ✅ si estamos en countdown y alguien lo apagó, lo reactivamos
        if (forceVisibleDuringCountdown &&
            DodgeState.Instance.dodgeGameState == DodgeState.DodgeGameStateEnum.Countdown)
        {
            if (countDownText != null && !countDownText.gameObject.activeSelf)
                countDownText.gameObject.SetActive(true);
        }

        if (!isCustomMessage)
        {
            float t = DodgeState.Instance.GetCountDownTimer();
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
    }

    public void Show()
    {
        isCustomMessage = false;
        lastSecondPlayed = -1; // Reseteamos el tracker de sonido

        if (countDownText != null)
            countDownText.gameObject.SetActive(true);

        if (myAnimator != null)
            myAnimator.SetBool("IsCountDown", true);
    }

    public void Hide()
    {
        if (countDownText != null)
            countDownText.gameObject.SetActive(false);
    }

    public void ShowMessage(string message)
    {
        isCustomMessage = true;

        if (countDownText != null)
        {
            countDownText.gameObject.SetActive(true);
            countDownText.text = message;
        }
    }

    public void ShowMessage(LocalizedString localizedMessage)
    {
        isCustomMessage = true;

        if (countDownText != null)
        {
            countDownText.gameObject.SetActive(true);
            countDownText.text = localizedMessage.GetLocalizedString();
        }
    }

    public void ShowGo(float duration = 0.7f)
    {
        StartCoroutine(ShowGoRoutine(duration));
    }

    private IEnumerator ShowGoRoutine(float duration)
    {
        isCustomMessage = true;

        if (countDownText != null)
        {
            countDownText.gameObject.SetActive(true);
            countDownText.text = goMessage.GetLocalizedString();
        }

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

        if (countDownText != null)
            countDownText.gameObject.SetActive(false);

        isCustomMessage = false;

        if (DodgeState.Instance != null)
            DodgeState.Instance.StartGameAfterGo();
    }
}