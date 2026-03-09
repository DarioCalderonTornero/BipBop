using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public class CountDownUI : MonoBehaviour
{
    public static CountDownUI Instance { get; private set; }

    public TextMeshProUGUI countDownText;

    [Header("Localization")]
    public LocalizedString readyMessage;  // "Prepárate..." / "Get ready..."
    public LocalizedString goMessage;     // "GO!" / "Go!"

    [Header("Audio")]
    [SerializeField] private AudioClip countdownAudioClip; // Sonido para 3, 2, 1
    [SerializeField] private float countdownPitch = 1.5f;  // Pitch agudo para los números
    [SerializeField] private AudioClip goAudioClip;        // Sonido distinto para el GO!

    private Animator myAnimator;
    private bool isCustomMessage = false;
    private int lastSecondPlayed = -1; // Evita que suene múltiples veces por segundo

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        myAnimator = GetComponent<Animator>();
        Hide();
    }

    private void Update()
    {
        if (!isCustomMessage)
        {
            if (GameStates.Instance == null) return; // Añadido por seguridad

            float t = GameStates.Instance.GetCountDownTime();

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
        countDownText.gameObject.SetActive(true);
        myAnimator.SetBool("IsCountDown", true);
        isCustomMessage = false;
        lastSecondPlayed = -1; // Reseteamos el tracker de sonido
    }

    public void Hide()
    {
        countDownText.gameObject.SetActive(false);
        isCustomMessage = false;
    }

    // Versión genérica, para mantener compatibilidad
    public void ShowMessage(string message)
    {
        isCustomMessage = true;
        countDownText.text = message;
    }

    // (Opcional) Versión genérica pero con LocalizedString
    public void ShowMessage(LocalizedString localizedMessage)
    {
        isCustomMessage = true;
        countDownText.text = localizedMessage.GetLocalizedString();
    }

    public void ShowReadyMessage()
    {
        isCustomMessage = true;
        countDownText.text = readyMessage.GetLocalizedString();
    }

    public void ShowGoMessage()
    {
        isCustomMessage = true;
        countDownText.text = goMessage.GetLocalizedString();

        // --- NUEVO: Sonido final distinto para el GO! ---
        if (goAudioClip != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(goAudioClip, 1f, 1f);
        }
    }

    public void SetAnimatorFalse()
    {
        myAnimator.SetBool("IsCountDown", false);
    }
}