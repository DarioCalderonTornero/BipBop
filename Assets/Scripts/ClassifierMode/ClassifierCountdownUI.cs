using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public class ClassifierCountdownUI : MonoBehaviour
{
    public static ClassifierCountdownUI Instance { get; private set; }

    public TextMeshProUGUI countDownText;

    [SerializeField] private Animator myAnimator;

    [Header("Localization")]
    public LocalizedString goMessage;

    [Header("Audio")]
    [SerializeField] private AudioClip countdownAudioClip; // Sonido para 3, 2, 1
    [SerializeField] private float countdownPitch = 1.5f;  // Pitch agudo para los números
    [SerializeField] private AudioClip goAudioClip;        // Sonido distinto para el GO!

    private bool isCustomMessage = false;
    private int lastSecondPlayed = -1; // Para controlar que el sonido solo suene 1 vez por segundo

    private void Awake()
    {
        Instance = this;

        if (myAnimator == null)
            myAnimator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (isCustomMessage) return;

        if (ClassifierState.Instance == null) return;

        float t = ClassifierState.Instance.GetCountDownTimer();

        if (t > 0f)
        {
            int currentSecond = Mathf.CeilToInt(t);
            countDownText.text = currentSecond.ToString();

            // --- NUEVO: Magia de Audio ---
            // Solo reproducimos si es un número nuevo que no ha sonado todavía
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
        countDownText.gameObject.SetActive(true);
        lastSecondPlayed = -1; // Reseteamos el tracker de sonido al mostrar

        if (myAnimator != null)
            myAnimator.SetBool("IsCountDown", true);
    }

    public void Hide()
    {
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

        countDownText.gameObject.SetActive(true);
        countDownText.text = goMessage.GetLocalizedString();

        // --- NUEVO: Sonido final distinto para el GO! ---
        if (goAudioClip != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(goAudioClip, 1f, 1f); // Pitch normal a 1f
        }

        yield return new WaitForSeconds(duration);

        if (myAnimator != null)
        {
            myAnimator.SetBool("IsCountDown", false);
            myAnimator.SetBool("CountDownFinish", true);
        }

        countDownText.gameObject.SetActive(false);
        isCustomMessage = false;

        if (ClassifierState.Instance != null)
            ClassifierState.Instance.StartGameAfterGo();
    }
}