using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryAvatarItem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    public Button selectButton;

    [Header("Datos")]
    [SerializeField] private AvatarDataSO avatarData;

    private bool isSelected = false;

    [Header("Estado de propiedad")]
    [SerializeField] private Color lockedTint = Color.gray;
    private bool isOwned = false;
    private Color originalColor;

    // Para recuperar el estilo original de la fuente
    private FontStyles originalFontStyle;

    // 👇 ID del avatar que SIEMPRE está comprado
    private const string DEFAULT_AVATAR_ID = "NormalAvatar";

    // 👉 Propiedad pública para que el manager sepa si está conseguido
    public bool IsOwned => isOwned;

    public void Setup(AvatarDataSO avatarData)
    {
        this.avatarData = avatarData;

        if (avatarImage != null)
        {
            avatarImage.sprite = avatarData.sprite;
            originalColor = avatarImage.color;
        }

        if (nameText != null)
        {
            nameText.text = avatarData.displayName;
            originalFontStyle = nameText.fontStyle;
        }

        // 🔹 Propiedad base: el avatar por defecto SIEMPRE está owned
        string key = "AvatarPurchased_" + avatarData.id;
        bool defaultOwned = avatarData.id == DEFAULT_AVATAR_ID;

        isOwned = defaultOwned || PlayerPrefs.GetInt(key, 0) == 1;

        // 🔹 Si NO está comprado pero es de tipo "por puntuación", miramos récords
        if (!isOwned && avatarData.unlockByScore && !string.IsNullOrEmpty(avatarData.requiredScoreKey))
        {
            int bestScore = PlayerPrefs.GetInt(avatarData.requiredScoreKey, 0);

            if (bestScore >= avatarData.requiredScoreValue)
            {
                // Lo desbloqueamos de verdad y lo persistimos
                isOwned = true;
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
            }
        }

        ApplyOwnershipVisuals();

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnSelectClicked);
        }
    }

    public void Select()
    {
        isSelected = true;
        transform.localScale = Vector3.one * 1.1f;
    }

    public void Deselect()
    {
        isSelected = false;
        transform.localScale = Vector3.one;
    }

    public AvatarDataSO GetAvatarData()
    {
        return avatarData;
    }

    private void OnSelectClicked()
    {
        // ❌ YA NO hacemos early-return si no es owned.
        // Queremos poder seleccionarlo para mostrar descripción.

        AvatarInventoryManager inventoryManager = FindFirstObjectByType<AvatarInventoryManager>();
        if (inventoryManager != null)
            inventoryManager.OnAvatarSelected(this);
    }

    private void ApplyOwnershipVisuals()
    {
        // Imagen gris si no es tuyo
        if (avatarImage != null)
            avatarImage.color = isOwned ? originalColor : lockedTint;

        // Nombre en negrita si NO lo tienes, estilo original si sí
        if (nameText != null)
            nameText.fontStyle = isOwned ? originalFontStyle : FontStyles.Bold;

        // ⛔ Ahora dejamos que siempre se pueda pulsar, aunque esté bloqueado
        if (selectButton != null)
            selectButton.interactable = true;
    }
}
