using System.Collections;
using TMPro;
using UnityEngine;

public class DodgeCountDownUI : MonoBehaviour
{
    public static DodgeCountDownUI Instance { get; private set; }

    public TextMeshProUGUI countDownText;

    [SerializeField] private Animator myAnimator;

    private bool isCustomMessage = false;

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
                countDownText.text = Mathf.Ceil(t).ToString();
        }
    }

    public void Show()
    {
        isCustomMessage = false;

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
            countDownText.text = "GO!";
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
