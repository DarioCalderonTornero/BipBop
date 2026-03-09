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

    private bool isCustomMessage = false;

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
            countDownText.text = Mathf.Ceil(t).ToString();
        }
    }

    public void Show()
    {
        isCustomMessage = false;
        countDownText.gameObject.SetActive(true);

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