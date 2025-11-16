using TMPro;
using UnityEngine;

public class ColorCountDownUI : MonoBehaviour
{
  public static ColorCountDownUI Instance { get; private set; }

  public TextMeshProUGUI countDownText;

  private Animator myAnimator;

  private bool isCustomMessage = false;

  private void Awake()
  {
    Instance = this;
  }
  private void Start()
  {
    myAnimator = GetComponent<Animator>();
  }

  private void Update()
  {
      if (!isCustomMessage)
      {
          countDownText.text = Mathf.Ceil(ColorGameState.Instance.GetCountDownTimer()).ToString();
      }
  }

  public void Show()
  {
      countDownText.gameObject.SetActive(true);
      myAnimator.SetBool("IsCountDown", true);
  }

  public void Hide()
  {
      countDownText.gameObject.SetActive(false);
  }

  public void ShowMessage(string message)
  {
      isCustomMessage = true;
      countDownText.text = message;
  }

  public void SetAnimatorFalse()
  {
      myAnimator.SetBool("IsCountDown", false);
  }
}
