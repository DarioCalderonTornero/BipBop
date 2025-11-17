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

    private Animator myAnimator;
    private bool isCustomMessage = false;

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
            countDownText.text = Mathf.Ceil(GameStates.Instance.GetCountDownTime()).ToString();
        }
    }

    public void Show()
    {
        countDownText.gameObject.SetActive(true);
        myAnimator.SetBool("IsCountDown", true);
        isCustomMessage = false;
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
    }

    public void SetAnimatorFalse()
    {
        myAnimator.SetBool("IsCountDown", false);
    }
}
