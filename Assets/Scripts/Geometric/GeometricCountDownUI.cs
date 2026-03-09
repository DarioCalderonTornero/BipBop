using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public class GeometricCountDownUI : MonoBehaviour
{
    public static GeometricCountDownUI Instance { get; private set; }

    public TextMeshProUGUI countDownText;

    [SerializeField] private Animator myAnimator;

    [Header("Localization")]
    public LocalizedString goMessage;   // "GO!" / "Go!" / lo que corresponda

    [Header("Audio")]
    [SerializeField] private AudioClip countdownAudioClip; // Sonido para 3, 2, 1
    [SerializeField] private float countdownPitch = 1.5f;  // Pitch agudo para los números
    [SerializeField] private AudioClip goAudioClip;        // Sonido distinto para el GO!

    private bool isCustomMessage = false;
    private int lastSecondPlayed = -1; // Para evitar que el sonido se repita cada frame

    private void Awake()
    {
        Instance = this;

        // Importante: disponible antes de Start()
        if (myAnimator == null)
            myAnimator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (isCustomMessage) return;

        // Importante: evitar null si el State aún no está listo
        if (GeometricState.Instance == null) return;

        float t = GeometricState.Instance.GetCountDownTimer();

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

        if (GeometricState.Instance != null)
            GeometricState.Instance.StartGameAfterGo();
    }
}