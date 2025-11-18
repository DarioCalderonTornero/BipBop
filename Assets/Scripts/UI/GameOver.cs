using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOver : MonoBehaviour
{
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Image backGround;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI gameOverText;

    [SerializeField] private Animator myanimator;

    [Header("Transition")]
    [SerializeField] private string mainMenuSceneName = "Menu";         // nombre EXACTO de la escena del menú
    [SerializeField] private LocalizedString mainMenuTransitionLabel;   // "Menú" / "Main Menu"

    private void Awake()
    {
        // Retry: puedes dejarlo directo o también hacerlo con transición, como quieras.
        retryButton.onClick.AddListener(() =>
        {
            // Si quieres usar transición también aquí:
            // if (TransitionScript.Instance != null)
            // {
            //     TransitionScript.Instance.TransitionToScene("GameScene", "BipBop");
            // }
            // else
            // {
            //     SceneLoader.LoadScene(SceneLoader.Scene.GameScene);
            // }

            SceneLoader.LoadScene(SceneLoader.Scene.GameScene);
        });

        mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    private void Start()
    {
        myanimator = GetComponent<Animator>();
        LogicaJuego.Instance.OnGameOver += LogicaPuntos_OnGameOver;
    }

    private void LogicaPuntos_OnGameOver(object sender, System.EventArgs e)
    {
        Debug.Log("Show");
        myanimator.SetBool("IsGameOver", true);
    }

    private void OnMainMenuClicked()
    {
        // 1) Si tenemos TransitionScript en la escena, usamos la animación
        if (TransitionScript.Instance != null)
        {
            // Texto centrado que irá en la transición
            string label = mainMenuTransitionLabel.GetLocalizedString();
            // Ej: "Menú" / "Main Menu"

            TransitionScript.Instance.TransitionToScene(mainMenuSceneName, label);
        }
        else
        {
            // 2) Fallback por si no existe TransitionScript en esta escena (para que no casque)
            SceneLoader.LoadScene(SceneLoader.Scene.Menu);
        }
    }

}
