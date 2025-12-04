using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryAvatarItem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    public Button selectButton;  // Agregamos el botón de selección

    [Header("Datos")]
    [SerializeField] private AvatarDataSO avatarData;   // Datos del avatar

    private bool isSelected = false;  // Estado de si está seleccionado o no

    [Header("Estado de propiedad")]
    [SerializeField] private Color lockedTint = Color.gray; // Color para avatares no comprados
    private bool isOwned = false;
    private Color originalColor;

    // Configuración inicial del item
    public void Setup(AvatarDataSO avatarData)
    {
        this.avatarData = avatarData;

        if (avatarImage != null)
        {
            avatarImage.sprite = avatarData.sprite;
            originalColor = avatarImage.color;
        }

        if (nameText != null)
            nameText.text = avatarData.displayName;

        // Comprobar propiedad
        string key = "AvatarPurchased_" + avatarData.id;
        isOwned = PlayerPrefs.GetInt(key, 0) == 1;

        ApplyOwnershipVisuals();

        // Listener de selección
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnSelectClicked);
        }
    }

    // Seleccionar el avatar (escala el item y cambia el estado a seleccionado)
    // Seleccionar el avatar (escala el item y cambia el estado a seleccionado)
    public void Select()
    {
        isSelected = true;
        transform.localScale = Vector3.one * 1.1f;  // Aumentar tamaño para indicar selección
    }

    // Deseleccionar el avatar (restaurar el tamaño original)
    public void Deselect()
    {
        isSelected = false;
        transform.localScale = Vector3.one;  // Restaurar tamaño original
    }

    // Obtener los datos del avatar
    public AvatarDataSO GetAvatarData()
    {
        return avatarData;
    }

    // Acción cuando se hace clic en el botón para seleccionar el avatar
    private void OnSelectClicked()
    {
        if (!isOwned)
            return;

        AvatarInventoryManager inventoryManager = FindFirstObjectByType<AvatarInventoryManager>();
        inventoryManager.OnAvatarSelected(this);
    }

    private void ApplyOwnershipVisuals()
    {
        if (avatarImage != null)
            avatarImage.color = isOwned ? originalColor : lockedTint;

        if (selectButton != null)
            selectButton.interactable = isOwned;
    }
}
