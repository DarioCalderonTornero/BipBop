using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryAvatarItem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    public Button selectButton;

    [Header("Icono de candado")]
    [SerializeField] private Image lockedIcon;
    // Icono de candado para avatares bloqueados

    [Header("Fondo de selección")]
    [SerializeField] private Image selectionBackground;
    // 👉 Imagen de fondo que se ve SOLO cuando el item está seleccionado

    [Header("Datos")]
    [SerializeField] private AvatarDataSO avatarData;

    private bool isSelected = false;

    [Header("Estado de propiedad")]
    [SerializeField] private Color lockedTint = Color.gray;
    private bool isOwned = false;
    private Color originalColor;

    private FontStyles originalFontStyle;
    private const string DEFAULT_AVATAR_ID = "NormalAvatar";
    public bool IsOwned => isOwned;

    [Header("Animación de pop")]
    [SerializeField] private float popDuration = 0.12f;
    [SerializeField] private float popScale = 1.1f;

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

        // Si no se asignó por Inspector, intentamos buscarla por nombre
        if (selectionBackground == null)
        {
            Transform bg = transform.Find("ImagenFondoSeleccion");
            if (bg != null)
                selectionBackground = bg.GetComponent<Image>();
        }

        string key = "AvatarPurchased_" + avatarData.id;
        bool defaultOwned = avatarData.id == DEFAULT_AVATAR_ID;
        isOwned = defaultOwned || PlayerPrefs.GetInt(key, 0) == 1;

        if (!isOwned && avatarData.unlockByScore && !string.IsNullOrEmpty(avatarData.requiredScoreKey))
        {
            int bestScore = PlayerPrefs.GetInt(avatarData.requiredScoreKey, 0);

            if (bestScore >= avatarData.requiredScoreValue)
            {
                isOwned = true;
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
            }
        }

        ApplyOwnershipVisuals();
        ApplySelectionBackground(false); // al inicio, sin selección

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnSelectClicked);
        }
    }

    public void Select()
    {
        isSelected = true;

        StopAllCoroutines();
        StartCoroutine(PopRoutine());

        ApplySelectionBackground(true);
    }

    public void Deselect()
    {
        isSelected = false;

        StopAllCoroutines();
        transform.localScale = Vector3.one;

        ApplySelectionBackground(false);
    }

    public AvatarDataSO GetAvatarData() => avatarData;

    private void OnSelectClicked()
    {
        AvatarInventoryManager inventoryManager = FindFirstObjectByType<AvatarInventoryManager>();
        if (inventoryManager != null)
            inventoryManager.OnAvatarSelected(this);
    }

    private void ApplyOwnershipVisuals()
    {
        // Imagen gris si no es tuyo
        if (avatarImage != null)
            avatarImage.color = isOwned ? originalColor : lockedTint;

        // Nombre en negrita si está bloqueado
        if (nameText != null)
            nameText.fontStyle = isOwned ? originalFontStyle : FontStyles.Bold;

        // Icono de candado visible SOLO si está bloqueado
        if (lockedIcon != null)
            lockedIcon.gameObject.SetActive(!isOwned);

        // Se puede pulsar siempre (para ver descripción aunque no sea tuyo)
        if (selectButton != null)
            selectButton.interactable = true;
    }

    private void ApplySelectionBackground(bool selected)
    {
        if (selectionBackground == null)
            return;

        selectionBackground.gameObject.SetActive(selected);
    }

    private System.Collections.IEnumerator PopRoutine()
    {
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = Vector3.one * popScale;

        float half = popDuration * 0.5f;
        float t = 0f;

        // Subida
        while (t < half)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / half);
            transform.localScale = Vector3.Lerp(originalScale, targetScale, lerp);
            yield return null;
        }

        // Bajada
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / half);
            transform.localScale = Vector3.Lerp(targetScale, originalScale, lerp);
            yield return null;
        }

        transform.localScale = originalScale;
    }
}
