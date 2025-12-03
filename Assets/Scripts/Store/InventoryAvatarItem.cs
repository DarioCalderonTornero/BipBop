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

    // Configuración inicial del item
    public void Setup(AvatarDataSO avatarData)
    {
        this.avatarData = avatarData;

        if (avatarImage != null)
            avatarImage.sprite = avatarData.sprite;

        if (nameText != null)
            nameText.text = avatarData.displayName;

        // Asociamos el botón de selección con el método que se ejecutará cuando se haga clic
        if (selectButton != null)
        {
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
        AvatarInventoryManager inventoryManager = FindFirstObjectByType<AvatarInventoryManager>();

        // Llamamos al método en el manager para seleccionar o deseleccionar este avatar
        inventoryManager.OnAvatarSelected(this);
    }
}
