using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    public static Timer Instance {  get; private set; }

    [SerializeField] private Image timerImage;

    private void Awake()
    {
        Instance = this;
        timerImage.fillAmount = 0;
        //Hide();
    }

    private void Update()
    {
        timerImage.fillAmount = GameStates.Instance.GetGameTimerUI();
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
