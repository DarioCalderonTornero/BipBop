using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CountDownUI : MonoBehaviour
{
    public static CountDownUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI countDownText;

    private Animator myAnimator;

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
        countDownText.text = Mathf.Ceil(GameStates.Instance.GetCountDownTime()).ToString();
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
}
