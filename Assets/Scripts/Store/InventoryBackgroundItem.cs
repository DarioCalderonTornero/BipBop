using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryBackgroundItem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Button selectButton;

    [Header("Icono de candado (si NO está comprado)")]
    [SerializeField] private Image lockedIcon;

    [Header("Selección (imagen de fondo que se activa al seleccionar)")]
    [SerializeField] private Image selectionBgImage;

    [Header("Estado de propiedad")]
    [SerializeField] private Color lockedTint = Color.gray;

    [Header("Animación de pop")]
    [SerializeField] private float popDuration = 0.12f;
    [SerializeField] private float popScale = 1.1f;

    [Header("Default (siempre owned)")]
    [SerializeField] private string defaultBackgroundId = "DefaultBackground";

    private BackgroundDataSO data;
    private bool isOwned;
    private bool isSelected;
    private Color originalColor;

    public bool IsOwned => isOwned;

    public void Setup(BackgroundDataSO backgroundData)
    {
        data = backgroundData;

        if (backgroundImage != null)
        {
            backgroundImage.sprite = data != null ? data.sprite : null;
            originalColor = backgroundImage.color;
        }

        if (nameText != null)
            nameText.text = data != null ? data.name : "—";

        // Owned por PlayerPrefs o por default
        if (data != null)
        {
            bool defaultOwned = data.id == defaultBackgroundId;
            isOwned = defaultOwned || PlayerPrefs.GetInt("Purchased_" + data.id, 0) == 1;
        }
        else
        {
            isOwned = false;
        }

        ApplyOwnershipVisuals();
        SetSelected(false);

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnClick);
        }
    }

    public BackgroundDataSO GetData() => data;

    public void Select()
    {
        isSelected = true;

        StopAllCoroutines();
        StartCoroutine(PopRoutine());

        SetSelected(true);
    }

    public void Deselect()
    {
        isSelected = false;

        StopAllCoroutines();
        transform.localScale = Vector3.one;

        SetSelected(false);
    }

    private void OnClick()
    {
        var manager = FindFirstObjectByType<BackgroundInventoryManager>();
        if (manager != null)
            manager.OnBackgroundSelected(this);
    }

    private void ApplyOwnershipVisuals()
    {
        if (backgroundImage != null)
            backgroundImage.color = isOwned ? originalColor : lockedTint;

        if (lockedIcon != null)
            lockedIcon.gameObject.SetActive(!isOwned);

        // Siempre clickable para ver descripción
        if (selectButton != null)
            selectButton.interactable = true;
    }

    private void SetSelected(bool selected)
    {
        if (selectionBgImage != null)
            selectionBgImage.gameObject.SetActive(selected);
    }

    private IEnumerator PopRoutine()
    {
        Vector3 original = Vector3.one;
        Vector3 bigger = Vector3.one * popScale;

        float half = popDuration * 0.5f;
        float t = 0f;

        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / half);
            transform.localScale = Vector3.Lerp(original, bigger, p);
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / half);
            transform.localScale = Vector3.Lerp(bigger, original, p);
            yield return null;
        }

        transform.localScale = original;
    }
}
