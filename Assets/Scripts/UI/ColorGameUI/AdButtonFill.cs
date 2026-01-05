using System;
using UnityEngine;
using UnityEngine.UI;

public class AdButtonFill : MonoBehaviour
{
    public event EventHandler OnHideOffer;

    [SerializeField] private Image fillImage;
    [SerializeField] private float duration = 5f;

    [SerializeField] VideoGameOver videoGameOver;

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
            OnHideOffer?.Invoke(this, EventArgs.Empty);
            videoGameOver.HideAdOffer();
        }
    }
}
