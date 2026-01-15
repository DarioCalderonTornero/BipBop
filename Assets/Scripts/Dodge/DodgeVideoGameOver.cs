using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DodgeVideoGameOver : MonoBehaviour
{
    [SerializeField] private Image videoGameOverBackgroundImage;
    [SerializeField] private Button playVideoButton;
    [SerializeField] private TextMeshProUGUI playVideoText;
    [SerializeField] private Animator playVideoAnimator;

    private void Awake()
    {
        HideAdOffer();

        if (playVideoButton != null)
        {
            playVideoButton.onClick.RemoveAllListeners();
            playVideoButton.onClick.AddListener(OnClickPlayVideo);
        }
    }

    private void Start()
    {
        if (DodgeManager.Instance != null)
            DodgeManager.Instance.OnVideo += DodgeManager_OnVideo;
    }

    private void DodgeManager_OnVideo(object sender, EventArgs e)
    {
        ShowAdOffer();
    }

    public void ShowAdOffer()
    {
        if (videoGameOverBackgroundImage != null) videoGameOverBackgroundImage.gameObject.SetActive(true);
        if (playVideoButton != null) playVideoButton.gameObject.SetActive(true);
        if (playVideoText != null) playVideoText.gameObject.SetActive(true);

        if (playVideoAnimator != null)
            playVideoAnimator.SetBool("PlayVideoGameOver", true);
    }

    public void HideAdOffer()
    {
        if (playVideoAnimator != null)
            playVideoAnimator.SetBool("PlayVideoGameOver", false);

        if (videoGameOverBackgroundImage != null) videoGameOverBackgroundImage.gameObject.SetActive(false);
        if (playVideoButton != null) playVideoButton.gameObject.SetActive(false);
        if (playVideoText != null) playVideoText.gameObject.SetActive(false);
    }

    private void OnClickPlayVideo()
    {
        // Caso 1: no hay ads -> GameOver directo
        if (MediationAds.Instance == null)
        {
            Debug.LogWarning("No hay instancia de MediationAds.");
            if (DodgeManager.Instance != null)
                DodgeManager.Instance.SetDeathType(DodgeManager.DeathType.GameOver);
            return;
        }

        // Caso 2: ad listo -> lo mostramos y al reward revive
        if (MediationAds.Instance.IsAdReady())
        {
            HideAdOffer();
            MediationAds.Instance.ShowRewardedAd(OnAdRewardedRevive);
        }
        // Caso 3: ad no listo -> GameOver directo
        else
        {
            Debug.Log("Anuncio no listo.");
            if (DodgeManager.Instance != null)
                DodgeManager.Instance.SetDeathType(DodgeManager.DeathType.GameOver);
        }
    }

    private void OnAdRewardedRevive()
    {
        if (DodgeManager.Instance != null)
            DodgeManager.Instance.StartReviveCountdown();
    }

    private void OnDestroy()
    {
        if (DodgeManager.Instance != null)
            DodgeManager.Instance.OnVideo -= DodgeManager_OnVideo;
    }
}
