using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ColorGameOverUI : MonoBehaviour
{

    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Image backGround;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI gameOverText;

    [SerializeField] private Animator myanimator;

    [SerializeField] private VideoGameOver videoGameOver;
    [SerializeField] private AdButtonFill adButtonFill;

    private void Awake()
    {
        //Hide();
        retryButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.ColorScene);
        });

        mainMenuButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.Menu);
        });
    }

    void Start()
    {
        myanimator = GetComponent<Animator>();
        ColorManager.Instance.OnGameOver += ColorManager_OnGameOver;
        adButtonFill.OnHideOffer += AdButtonFill_OnHideOffer;
    }

    private void AdButtonFill_OnHideOffer(object sender, System.EventArgs e)
    {
        coinText.text = "Coins: " + ColorGamePuntos.Instance.GetScore();
        myanimator.SetBool("IsGameOver", true);
    }

    private void ColorManager_OnGameOver(object sender, System.EventArgs e)
    {
        StartCoroutine(DecideShowGameOverNextFrame());
    }

    private IEnumerator DecideShowGameOverNextFrame()
    {
        yield return null;

        if (videoGameOver != null && videoGameOver.isVideoShow)
            yield break;

        ShowGameOver();
    }


    private void ShowGameOver()
    {
        coinText.text = "Coins: " + ColorGamePuntos.Instance.GetScore();
        myanimator.SetBool("IsGameOver", true);
    }

    private void OnDestroy()
    {
        ColorManager.Instance.OnGameOver -= ColorManager_OnGameOver;
    }
}

