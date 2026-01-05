using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VideoGameOver : MonoBehaviour
{
    [SerializeField] private Image videoGameOverBackgroundImage;
    [SerializeField] private Button playVideoButton;
    [SerializeField] private TextMeshProUGUI playVideoText;

    private void Awake()
    {
        videoGameOverBackgroundImage.gameObject.SetActive(false);
        playVideoButton.gameObject.SetActive(false);
        playVideoText.gameObject.SetActive(false);

        playVideoButton.onClick.RemoveAllListeners();
        playVideoButton.onClick.AddListener(OnClickPlayVideo);
    }

    private void Start()
    {
        ColorManager.Instance.OnGameOver += ColorManager_OnGameOver;
    }

    private void ColorManager_OnGameOver(object sender, System.EventArgs e)
    {
        // Aquí solo muestras el panel cuando tu otro script lo decida (50% lo controlas fuera)
        videoGameOverBackgroundImage.gameObject.SetActive(true);
        playVideoButton.gameObject.SetActive(true);
        playVideoText.gameObject.SetActive(true);
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
            // Ocultar panel mientras se ve el anuncio
            videoGameOverBackgroundImage.gameObject.SetActive(false);
            playVideoButton.gameObject.SetActive(false);
            playVideoText.gameObject.SetActive(false);

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
        // Acción al terminar el anuncio (revivir/recargar)
        SceneLoader.LoadScene(SceneLoader.Scene.ColorScene);
    }

    private void OnDestroy()
    {
        ColorManager.Instance.OnGameOver -= ColorManager_OnGameOver;
    }
}
