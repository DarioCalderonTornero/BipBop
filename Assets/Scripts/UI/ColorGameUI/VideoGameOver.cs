using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VideoGameOver : MonoBehaviour
{
    public bool isVideoShow = false;

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
        ColorManager.Instance.OnGameOver += ColorManager_OnGameOver;
    }

    private void ColorManager_OnGameOver(object sender, EventArgs e)
    {
        isVideoShow = false;

        float videoProbability = UnityEngine.Random.Range(0, 10);

        if (videoProbability >= 5)
        {
            ShowAdOffer();
            isVideoShow = true;
        }
    }

    public void ShowAdOffer()
    {
        videoGameOverBackgroundImage.gameObject.SetActive(true);
        playVideoButton.gameObject.SetActive(true);
        playVideoText.gameObject.SetActive(true);

        playVideoAnimator.SetBool("PlayVideoGameOver", true);
    }

    public void HideAdOffer()
    {
        if (playVideoAnimator != null)
            playVideoAnimator.SetBool("PlayVideoGameOver", false);

        videoGameOverBackgroundImage.gameObject.SetActive(false);
        playVideoButton.gameObject.SetActive(false);
        playVideoText.gameObject.SetActive(false);

        isVideoShow = false;
    }

    private void OnClickPlayVideo()
    {
        if (MediationAds.Instance == null)
        {
            Debug.LogWarning("No hay instancia de MediationAds en la escena (singleton).");
            SceneLoader.LoadScene(SceneLoader.Scene.ColorScene);
            return;
        }

        if (MediationAds.Instance.IsAdReady())
        {
            HideAdOffer();
            MediationAds.Instance.ShowRewardedAd(OnAdRewardedRevive);
        }
        else
        {
            Debug.Log("Anuncio no listo, recargando escena directamente.");
            SceneLoader.LoadScene(SceneLoader.Scene.ColorScene);
        }
    }

    private void OnAdRewardedRevive()
    {
        SceneLoader.LoadScene(SceneLoader.Scene.ColorScene);
    }

    private void OnDestroy()
    {
        if (ColorManager.Instance != null)
            ColorManager.Instance.OnGameOver -= ColorManager_OnGameOver;
    }
}
