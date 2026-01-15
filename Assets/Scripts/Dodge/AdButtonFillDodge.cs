using System;
using UnityEngine;
using UnityEngine.UI;

public class AdButtonFillDodge : MonoBehaviour
{
    public event EventHandler OnDodgeHideOffer;

    [SerializeField] private Image fillImage;
    [SerializeField] private float duration = 5f;

    [SerializeField] DodgeVideoGameOver dodgeVideoGameOver;

    private float timer;

    private void OnEnable()
    {
        timer = duration;
        fillImage.fillAmount = 1f;
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        fillImage.fillAmount = timer / duration;

        if (timer <= 0f)
        {

            OnDodgeHideOffer?.Invoke(this, EventArgs.Empty);
            dodgeVideoGameOver.HideAdOffer();
        }
    }
}
