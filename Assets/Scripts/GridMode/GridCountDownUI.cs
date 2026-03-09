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

    private bool isCustomMessage = false;

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
            countDownText.text = Mathf.Ceil(t).ToString();
    }

    public void Show()
    {
        isCustomMessage = false;
        forceVisibleDuringCountdown = true;
        countDownText.gameObject.SetActive(true);

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