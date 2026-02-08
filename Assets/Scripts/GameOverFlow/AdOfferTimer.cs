using System;
using UnityEngine;
using UnityEngine.UI;

public class AdOfferTimer : MonoBehaviour
{
    public event Action OnExpired;

    [SerializeField] private Image fillImage;
    [SerializeField] private float duration = 5f;

    private float timer;
    private bool running;

    public void Begin()
    {
        timer = duration;
        running = true;

        if (fillImage != null)
            fillImage.fillAmount = 1f;
    }

    public void Stop()
    {
        running = false;
    }

    private void Update()
    {
        if (!running) return;

        timer -= Time.deltaTime;

        if (fillImage != null)
            fillImage.fillAmount = Mathf.Clamp01(timer / duration);

        if (timer <= 0f)
        {
            running = false;
            OnExpired?.Invoke();
        }
    }
}
