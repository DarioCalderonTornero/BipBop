using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.Localization;

public class ColorCountDownUI : MonoBehaviour
{
    public static ColorCountDownUI Instance { get; private set; }

    public TextMeshProUGUI countDownText;

    private Animator myAnimator;
    private bool isCustomMessage = false;

    [Header("Localization")]
    public LocalizedString goMessage;

    [Header("Audio")]
    [SerializeField] private AudioClip countdownAudioClip; // Sonido para 3, 2, 1
    [SerializeField] private float countdownPitch = 1.5f;  // Pitch agudo para los números
    [SerializeField] private AudioClip goAudioClip;        // Sonido distinto para el GO!

    private int lastSecondPlayed = -1; // Evita que suene múltiples veces por segundo

    private void Awake()
    {
        Instance = this;

        // ✅ disponible antes de Start()
        myAnimator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (isCustomMessage) return;
        if (ColorGameState.Instance == null) return;

        float t = ColorGameState.Instance.GetCountDownTimer();

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
        countDownText.gameObject.SetActive(true);
        lastSecondPlayed = -1; // Reseteamos el tracker de sonido

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

        if (ColorGameState.Instance != null)
            ColorGameState.Instance.StartGameAfterGo();
    }
}