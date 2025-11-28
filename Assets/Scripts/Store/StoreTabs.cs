using UnityEngine;
using UnityEngine.UI;

public class StoreTabs : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject backgroundPanel;
    [SerializeField] private GameObject avatarPanel;

    [Header("Buttons")]
    [SerializeField] private Button fondosButton;
    [SerializeField] private Button avataresButton;

    private void Awake()
    {
        // Asignar listeners
        if (fondosButton != null)
            fondosButton.onClick.AddListener(ShowBackgrounds);

        if (avataresButton != null)
            avataresButton.onClick.AddListener(ShowAvatars);
    }

    private void Start()
    {
        // Al entrar en la tienda, mostramos fondos por defecto
        ShowBackgrounds();
    }

    private void ShowBackgrounds()
    {
        if (backgroundPanel != null) backgroundPanel.SetActive(true);
        if (avatarPanel != null) avatarPanel.SetActive(false);
    }

    private void ShowAvatars()
    {
        if (backgroundPanel != null) backgroundPanel.SetActive(false);
        if (avatarPanel != null) avatarPanel.SetActive(true);
    }
}
