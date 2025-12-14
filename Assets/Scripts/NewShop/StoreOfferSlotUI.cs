using System;
using System.Collections;
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

    [Header("Buttons")]
    [Tooltip("Botón grande del slot (seleccionar / alternar).")]
    [SerializeField] private Button slotButton;

    [Tooltip("Botón de comprar (se muestra al seleccionar).")]
    [SerializeField] private Button buyButton;

    [Header("Owned UI")]
    [Tooltip("Imagen/label que indica 'Propiedad' (solo cuando ya lo tienes).")]
    [SerializeField] private GameObject ownedBadge;

    [Header("Visual")]
    [SerializeField] private Color ownedTint = Color.gray;

    [Header("Pop (select)")]
    [SerializeField] private RectTransform popTarget;   // normalmente el root del slot
    [SerializeField] private float popInScale = 0.96f;  // hacia dentro
    [SerializeField] private float popDuration = 0.10f;

    private Action onBuy;
    private int currentPrice;
    private string currentTitle;

    private bool isOwned;
    private bool isSelected;

    private Color originalIconColor = Color.white;
    private Vector3 originalScale = Vector3.one;
    private Coroutine popRoutine;

    private void Awake()
    {
        if (icon != null) originalIconColor = icon.color;
        if (popTarget == null) popTarget = transform as RectTransform;
        if (popTarget != null) originalScale = popTarget.localScale;

        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(OnSlotClicked);
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClicked);
        }

        ForceHideAll();
    }

    // -------------------------
    // Public API
    // -------------------------

    /// <summary>Configura el slot como OFERTA comprable.</summary>
    public void SetContent(Sprite sprite, string title, int price, Action onBuyCallback)
    {
        isOwned = false;
        isSelected = false;

        onBuy = onBuyCallback;
        currentPrice = price;
        currentTitle = title;

        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = (sprite != null);
            icon.color = originalIconColor;
        }

        if (nameText != null)
        {
            nameText.text = title ?? "";
            nameText.gameObject.SetActive(true);
        }

        ApplySelectionVisuals(selected: false);
        SetOwnedBadge(false);

        if (slotButton != null) slotButton.interactable = true;
    }

    /// <summary>Configura el slot como YA EN PROPIEDAD (gris + badge, sin textos).</summary>
    public void SetOwned(Sprite sprite)
    {
        isOwned = true;
        isSelected = false;
        onBuy = null;

        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = (sprite != null);

            Color c = ownedTint;
            c.a = 1f;            // gris sólido, no transparente
            icon.color = c;
        }

        // 👇 En propiedad: ocultar nombre + precio + moneda + buy
        if (nameText != null) nameText.gameObject.SetActive(false);
        if (priceText != null) priceText.gameObject.SetActive(false);
        if (coinIcon != null) coinIcon.gameObject.SetActive(false);
        if (buyButton != null) buyButton.gameObject.SetActive(false);

        SetOwnedBadge(true);

        // (si quieres que al tocar haga preview, pon esto a true)
        if (slotButton != null) slotButton.interactable = false;

        // Reset pop
        if (popTarget != null) popTarget.localScale = originalScale;
    }

    public void MarkAsOwnedAfterPurchase(Sprite sprite)
    {
        SetOwned(sprite);
    }

    public void SetInteractable(bool interactable)
    {
        if (slotButton != null) slotButton.interactable = interactable && !isOwned;
        if (buyButton != null) buyButton.interactable = interactable && !isOwned;
    }

    // -------------------------
    // Clicks
    // -------------------------

    private void OnSlotClicked()
    {
        if (isOwned) return;

        // 👇 Toggle selección
        isSelected = !isSelected;

        ApplySelectionVisuals(isSelected);

        // Pop solo al seleccionar (no al deseleccionar)
        if (isSelected)
            PlayPopIn();
    }

    private void OnBuyClicked()
    {
        if (isOwned) return;
        onBuy?.Invoke();
    }

    // -------------------------
    // Visual state helpers
    // -------------------------

    private void ApplySelectionVisuals(bool selected)
    {
        // En seleccionado: ocultar precio/moneda y mostrar botón comprar
        if (buyButton != null)
            buyButton.gameObject.SetActive(selected);

        if (priceText != null)
        {
            priceText.text = currentPrice > 0 ? currentPrice.ToString() : "Gratis";
            priceText.gameObject.SetActive(!selected);
        }

        if (coinIcon != null)
            coinIcon.gameObject.SetActive(!selected && currentPrice > 0);

        // Nombre solo se oculta si está en propiedad (aquí NO)
        if (nameText != null && !isOwned)
        {
            nameText.text = currentTitle ?? "";
            nameText.gameObject.SetActive(true);
        }

        // Por si venimos de un estado anterior raro
        SetOwnedBadge(false);

        // Reset escala al deseleccionar
        if (!selected && popTarget != null)
            popTarget.localScale = originalScale;
    }

    private void SetOwnedBadge(bool active)
    {
        if (ownedBadge != null)
            ownedBadge.SetActive(active);
    }

    private void ForceHideAll()
    {
        if (buyButton != null) buyButton.gameObject.SetActive(false);
        if (priceText != null) priceText.gameObject.SetActive(false);
        if (coinIcon != null) coinIcon.gameObject.SetActive(false);
        if (ownedBadge != null) ownedBadge.SetActive(false);
    }

    // -------------------------
    // Pop
    // -------------------------

    private void PlayPopIn()
    {
        if (popTarget == null) return;

        if (popRoutine != null) StopCoroutine(popRoutine);
        popRoutine = StartCoroutine(PopInRoutine());
    }

    private IEnumerator PopInRoutine()
    {
        float half = popDuration * 0.5f;
        if (half <= 0f) yield break;

        Vector3 from = originalScale;
        Vector3 to = originalScale * popInScale;

        float t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / half);
            popTarget.localScale = Vector3.Lerp(from, to, p);
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / half);
            popTarget.localScale = Vector3.Lerp(to, from, p);
            yield return null;
        }

        popTarget.localScale = originalScale;
        popRoutine = null;
    }
}
