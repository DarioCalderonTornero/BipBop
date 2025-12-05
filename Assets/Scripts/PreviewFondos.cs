using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
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

    [SerializeField] private ScrollRect scrollRectLateral;

    [Header("Localization")]
    public LocalizedString priceLabel;               // Smart String: "Precio: {0}" / "Price: {0}"
    public LocalizedString buyText;                  // "Comprar" / "Buy"
    public LocalizedString insufficientCoinsText;    // "Monedas insuficientes" / "Not enough coins"
    public LocalizedString equippedText;             // "Equipado" / "Equipped"
    public LocalizedString equipText;                // "Equipar" / "Equip"

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

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    public void ShowPreview(BackgroundDataSO backgroundDataSO)
    {
        CurrentSelectedSprite = backgroundDataSO.sprite;
        currentBackgroundID = backgroundDataSO.id;
        currentPrice = backgroundDataSO.price;

        previewImage.sprite = backgroundDataSO.sprite;

        // 1) Activamos primero el panel
        previewPanel.SetActive(true);
        botonesPantallas.SetActive(false);

        if (scrollRectLateral != null)
            scrollRectLateral.enabled = false;

        // 2) AHORA refrescamos textos
        RefreshTexts();
    }

    private void RefreshTexts()
    {
        if (priceText == null || confirmButton == null) return;

        // Precio localizado
        priceText.text = priceLabel.GetLocalizedString(currentPrice);

        bool purchased = (currentBackgroundID == "DefaultBackground")
                         || (PlayerPrefs.GetInt("Purchased_" + currentBackgroundID, 0) == 1);

        int coins = CurrencyManager.Instance.GetCoins();
        var buttonLabel = confirmButton.GetComponentInChildren<TextMeshProUGUI>();

        if (!purchased)
        {
            confirmButton.interactable = coins >= currentPrice;
            buttonLabel.text = coins >= currentPrice
                ? buyText.GetLocalizedString()
                : insufficientCoinsText.GetLocalizedString();
        }
        else
        {
            string equipped = PlayerPrefs.GetString("SelectedBackground", "");
            bool isEquipped = equipped == currentBackgroundID;
            confirmButton.interactable = !isEquipped;
            buttonLabel.text = isEquipped
                ? equippedText.GetLocalizedString()
                : equipText.GetLocalizedString();
        }
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

                // 🔹 SUBIR COMPRA A PLAYFAB
                UploadBackgroundPurchaseToPlayFab(currentBackgroundID);
            }
            else
            {
                return; // El botón ya indica "insuficientes"
            }
        }
        else
        {
            PlayerPrefs.SetString("SelectedBackground", currentBackgroundID);
            PlayerPrefs.Save();

            // 🔹 SUBIR EQUIPADO A PLAYFAB
            UploadBackgroundEquipToPlayFab(currentBackgroundID);
        }

        fondoSelector.CambiarFondo(CurrentSelectedSprite);
        ClosePreviewPanel();
    }

    private void UploadBackgroundPurchaseToPlayFab(string backgroundId)
    {
        var request = new PlayFab.ClientModels.UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
        {
            { "Purchased_" + backgroundId, "1" },
            { "SelectedBackground", backgroundId } // ya que al comprar también se equipa
        },
            Permission = PlayFab.ClientModels.UserDataPermission.Public
        };

        PlayFab.PlayFabClientAPI.UpdateUserData(
            request,
            result => Debug.Log("Fondo comprado subido a PlayFab: " + backgroundId),
            error => Debug.LogWarning("Error al subir compra de fondo: " + error.GenerateErrorReport())
        );
    }

    private void UploadBackgroundEquipToPlayFab(string backgroundId)
    {
        var request = new PlayFab.ClientModels.UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
        {
            { "SelectedBackground", backgroundId }
        },
            Permission = PlayFab.ClientModels.UserDataPermission.Public
        };

        PlayFab.PlayFabClientAPI.UpdateUserData(
            request,
            result => Debug.Log("Fondo equipado subido a PlayFab: " + backgroundId),
            error => Debug.LogWarning("Error al subir fondo equipado: " + error.GenerateErrorReport())
        );
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

    private void OnLocaleChanged(Locale newLocale)
    {
        // Si cambias el idioma con el panel abierto, refrescamos los textos
        RefreshTexts();
    }
}
