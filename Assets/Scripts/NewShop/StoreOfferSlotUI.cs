using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreOfferSlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Image coinIcon;
    [SerializeField] private Button button;

    [Header("Visual")]
    [SerializeField] private Color ownedTint = Color.gray;

    private Color originalIconColor = Color.white;

    private void Awake()
    {
        if (icon != null)
            originalIconColor = icon.color;
    }

    /// <summary>Item NO comprado</summary>
    public void SetContent(Sprite sprite, string title, int price, System.Action onClick)
    {
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.color = originalIconColor;
            icon.enabled = (sprite != null);
        }

        if (nameText != null)
            nameText.text = title;

        if (priceText != null)
        {
            priceText.text = price > 0 ? price.ToString() : "Gratis";
            priceText.gameObject.SetActive(true);
        }

        if (coinIcon != null)
            coinIcon.gameObject.SetActive(price > 0);

        if (button != null)
        {
            button.interactable = true;
            button.onClick.RemoveAllListeners();
            if (onClick != null)
                button.onClick.AddListener(() => onClick());
        }
    }

    /// <summary>Item YA EN PROPIEDAD</summary>
    public void SetOwned(Sprite sprite)
    {
        if (icon != null)
        {
            icon.sprite = sprite;

            // 🔒 Forzamos gris sólido (alpha = 1)
            Color c = ownedTint;
            c.a = 1f;
            icon.color = c;

            icon.enabled = (sprite != null);
        }

        if (nameText != null)
            nameText.text = "En propiedad";

        if (priceText != null)
            priceText.gameObject.SetActive(false);

        if (coinIcon != null)
            coinIcon.gameObject.SetActive(false);

        if (button != null)
        {
            button.interactable = false;
            button.onClick.RemoveAllListeners();
        }
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }
}
