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

        playVideoButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.ColorScene);
        });
    }

    private void Start()
    {
        ColorManager.Instance.OnGameOver += ColorManager_OnGameOver;
    }

    private void ColorManager_OnGameOver(object sender, System.EventArgs e)
    {
        float videoProbability = Random.Range(0, 10);

        Debug.Log(videoProbability);

        if (videoProbability > 5)
        {
            videoGameOverBackgroundImage.gameObject.SetActive(true);
            playVideoButton.gameObject.SetActive(true);
            playVideoText.gameObject.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        ColorManager.Instance.OnGameOver -= ColorManager_OnGameOver;
    }
}
