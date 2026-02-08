using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ColorVideoGameOver : MonoBehaviour
{
    /*
    [SerializeField] private Image videoGameOverBackgroundImage;
    [SerializeField] private Button playVideoButton;
    [SerializeField] private TextMeshProUGUI playVideoText;
    [SerializeField] private Animator playVideoAnimator;

    private void Awake()
    {
        HideAdOffer();

        playVideoButton.onClick.RemoveAllListeners();
        playVideoButton.onClick.AddListener(OnClickPlayVideo);
    }

    private void Start()
    {
        ColorManager.Instance.OnVideo += ColorManager_OnVideo;
    }

    private void ColorManager_OnVideo(object sender, EventArgs e)
    {
        ShowAdOffer();
    }

    public void ShowAdOffer()
    {
        videoGameOverBackgroundImage.gameObject.SetActive(true);
        playVideoButton.gameObject.SetActive(true);
        playVideoText.gameObject.SetActive(true);

        if (playVideoAnimator != null)
            playVideoAnimator.SetBool("PlayVideoGameOver", true);
    }

    public void HideAdOffer()
    {
        if (playVideoAnimator != null)
            playVideoAnimator.SetBool("PlayVideoGameOver", false);

        videoGameOverBackgroundImage.gameObject.SetActive(false);
        playVideoButton.gameObject.SetActive(false);
        playVideoText.gameObject.SetActive(false);
    }

    private void OnClickPlayVideo()
    {
        if (MediationAds.Instance == null)
        {
            Debug.LogWarning("No hay instancia de MediationAds.");
            ColorManager.Instance.SetDeathType(ColorManager.DeathType.GameOver);
            return;
        }

        if (MediationAds.Instance.IsAdReady())
        {
            HideAdOffer();
            MediationAds.Instance.ShowRewardedAd(OnAdRewardedRevive);
        }
        else
        {
            Debug.Log("Anuncio no listo.");
            ColorManager.Instance.SetDeathType(ColorManager.DeathType.GameOver);
        }
    }

    private void OnAdRewardedRevive()
    {
        ColorManager.Instance.StartReviveCountdown();
    }

    private void OnDestroy()
    {
        if (ColorManager.Instance != null)
            ColorManager.Instance.OnVideo -= ColorManager_OnVideo;
    }
    */
}
