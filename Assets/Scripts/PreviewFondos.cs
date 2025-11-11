using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PreviewFondos : MonoBehaviour
{
    public static PreviewFondos Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject previewPanel;
    [SerializeField] private Image previewImage;
    [SerializeField] private FondoSelector fondoSelector;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private GameObject botonesPantallas;

    // Nuevo: referencia al ScrollRect que controla el desplazamiento lateral
    [SerializeField] private ScrollRect scrollRectLateral;

    private int currentPrice;
    private string currentBackgroundID;
    public Sprite CurrentSelectedSprite { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void ShowPreview(BackgroundDataSO backgroundDataSO)
    {
        CurrentSelectedSprite = backgroundDataSO.sprite;
        currentBackgroundID = backgroundDataSO.id;
        currentPrice = backgroundDataSO.price;

        previewImage.sprite = backgroundDataSO.sprite;
        priceText.text = "Precio: " + backgroundDataSO.price;

        bool purchased = (currentBackgroundID == "DefaultBackground")
                         || (PlayerPrefs.GetInt("Purchased_" + currentBackgroundID, 0) == 1);

        int coins = CurrencyManager.Instance.GetCoins();

        if (!purchased)
        {
            confirmButton.interactable = coins >= currentPrice;
            confirmButton.GetComponentInChildren<TextMeshProUGUI>().text =
                coins >= currentPrice ? "Comprar" : "Monedas insuficientes";
        }
        else
        {
            string equipped = PlayerPrefs.GetString("SelectedBackground", "");
            bool isEquipped = equipped == currentBackgroundID;
            confirmButton.interactable = !isEquipped;
            confirmButton.GetComponentInChildren<TextMeshProUGUI>().text =
                isEquipped ? "Equipado" : "Equipar";
        }

        previewPanel.SetActive(true);
        botonesPantallas.SetActive(false);

        // Desactivar desplazamiento lateral
        if (scrollRectLateral != null)
            scrollRectLateral.enabled = false;
    }

    public void ConfirmBackground()
    {
        bool purchased = PlayerPrefs.GetInt("Purchased_" + currentBackgroundID, 0) == 1;

        if (!purchased)
        {
            int coins = CurrencyManager.Instance.GetCoins();

            if (coins >= currentPrice)
            {
                CurrencyManager.Instance.SpendCoins(currentPrice);
                PlayerPrefs.SetInt("Purchased_" + currentBackgroundID, 1);
                PlayerPrefs.SetString("SelectedBackground", currentBackgroundID);
                PlayerPrefs.Save();
            }
            else
            {
                return;
            }
        }
        else
        {
            PlayerPrefs.SetString("SelectedBackground", currentBackgroundID);
            PlayerPrefs.Save();
        }

        fondoSelector.CambiarFondo(CurrentSelectedSprite);
        ClosePreviewPanel();
    }

    public void CancelPreview()
    {
        ClosePreviewPanel();
    }

    private void ClosePreviewPanel()
    {
        previewPanel.SetActive(false);
        botonesPantallas.SetActive(true);

        // Volver a activar el desplazamiento lateral
        if (scrollRectLateral != null)
            scrollRectLateral.enabled = true;
    }
}
