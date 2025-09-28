using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PreviewFondos : MonoBehaviour
{
    public static PreviewFondos Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject previewPanel; // Panel de preview (desactivado por defecto)
    [SerializeField] private Image previewImage;      // Imagen que muestra la preview del fondo
    [SerializeField] private FondoSelector fondoSelector; // Script para aplicar el fondo en la escena
    [SerializeField] private TextMeshProUGUI priceText; // Texto que muestra el precio en el panel
    [SerializeField] private Button confirmButton;      // Botón para comprar/equipar
    [SerializeField] private GameObject botonesPantallas; // Contenedor de los botones del scroll view

    // Variables para el fondo seleccionado en el preview
    private Sprite selectedSprite;
    private int currentPrice;
    private string currentBackgroundID; // Identificador único del fondo

    public Sprite CurrentSelectedSprite { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Se llama desde el botón de cada fondo en la tienda.
    /// </summary>
    public void ShowPreview(BackgroundDataSO backgroundDataSO)
    {
        CurrentSelectedSprite = backgroundDataSO.sprite;
        currentBackgroundID = backgroundDataSO.id;
        currentPrice = backgroundDataSO.price;

        previewImage.sprite = backgroundDataSO.sprite;
        priceText.text = "Precio: " + backgroundDataSO.price + " monedas";

        // Si es el fondo predeterminado, lo marcamos como comprado.
        bool purchased = (currentBackgroundID == "DefaultBackground")
                         || (PlayerPrefs.GetInt("Purchased_" + currentBackgroundID, 0) == 1);

        int coins = CurrencyManager.Instance.GetCoins();

        if (!purchased)
        {
            if (coins >= currentPrice)
            {
                confirmButton.interactable = true;
                confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "Comprar";
            }
            else
            {
                confirmButton.interactable = false;
                confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "Monedas insuficientes";
            }
        }
        else
        {
            string equipped = PlayerPrefs.GetString("SelectedBackground", "");
            if (equipped == currentBackgroundID)
            {
                confirmButton.interactable = false;
                confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "Equipado";
            }
            else
            {
                confirmButton.interactable = true;
                confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "Equipar";
            }
        }

        previewPanel.SetActive(true);
        botonesPantallas.SetActive(false);
    }

    /// <summary>
    /// Se llama al presionar el botón de confirmar en el panel de preview.
    /// </summary>
    public void ConfirmBackground()
    {
        // Comprueba si el fondo ya se compró
        bool purchased = PlayerPrefs.GetInt("Purchased_" + currentBackgroundID, 0) == 1;

        if (!purchased)
        {
            int coins = CurrencyManager.Instance.GetCoins();

            if (coins >= currentPrice)
            {
                // Gastar monedas con CurrencyManager (esto actualiza UI y PlayerPrefs)
                CurrencyManager.Instance.SpendCoins(currentPrice);
                Debug.Log(currentPrice);

                // Marca el fondo como comprado y lo equipa
                PlayerPrefs.SetInt("Purchased_" + currentBackgroundID, 1);
                PlayerPrefs.SetString("SelectedBackground", currentBackgroundID);
                PlayerPrefs.Save();
            }
            else
            {
                // Esto no debería suceder, ya que el botón debería estar deshabilitado
                return;
            }
        }
        else
        {
            // Si ya se ha comprado, simplemente equipa el fondo
            PlayerPrefs.SetString("SelectedBackground", currentBackgroundID);
            PlayerPrefs.Save();
        }

        // Aplica el fondo mediante el script FondoSelector
        fondoSelector.CambiarFondo(CurrentSelectedSprite);
        previewPanel.SetActive(false);
        botonesPantallas.SetActive(true);
    }

    /// <summary>
    /// Se llama al presionar el botón de cancelar en el panel de preview.
    /// </summary>
    public void CancelPreview()
    {
        previewPanel.SetActive(false);
        botonesPantallas.SetActive(true);
    }
}
