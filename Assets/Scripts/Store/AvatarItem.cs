using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class AvatarItem : MonoBehaviour
{
    [Header("Datos")]
    [SerializeField] private AvatarDataSO avatarData;

    [Header("UI")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;

    [Header("Botones")]
    [SerializeField] private Button selectButton;      // botón de la tarjeta (o el propio botón del avatar)
    [SerializeField] private Button buyButton;         // botón de "Comprar"
    [SerializeField] private Button cancelButton;      // botón "Cancelar"

    [Header("Animación")]
    [SerializeField] private float selectedScale = 1.1f;
    [SerializeField] private float animDuration = 0.12f;

    private Vector3 _originalScale;
    private bool _isSelected = false;
    private bool _isPurchased = false;

    private static AvatarItem currentlySelectedItem = null;  // Para controlar qué avatar está "hecho grande"

    private void Awake()
    {
        _originalScale = transform.localScale;

        // Ocultamos botones al inicio
        if (buyButton != null) buyButton.gameObject.SetActive(false);
        if (cancelButton != null) cancelButton.gameObject.SetActive(false);

        // Listeners
        if (selectButton != null)
            selectButton.onClick.AddListener(OnSelectClicked);

        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);

        // Rellenar UI con datos del SO
        SetupFromData();
    }

    private void SetupFromData()
    {
        if (avatarData == null)
        {
            Debug.LogError($"AvatarItem en {name} no tiene AvatarData asignado.");
            return;
        }

        if (avatarImage != null)
            avatarImage.sprite = avatarData.sprite;

        if (nameText != null)
            nameText.text = avatarData.displayName;

        if (priceText != null)
            priceText.text = avatarData.price + " monedas";

        // Comprobar si ya estaba comprado (PlayerPrefs simple)
        string key = "AvatarPurchased_" + avatarData.id;
        _isPurchased = PlayerPrefs.GetInt(key, 0) == 1;

        UpdateBuyText();  // Actualizamos el texto del botón de compra
    }

    private void UpdateBuyText()
    {
        if (buyButton == null) return;

        var tmp = buyButton.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null) return;

        // Si está comprado, desactivamos el botón de comprar
        if (_isPurchased)
        {
            tmp.text = "Equipar";
            buyButton.gameObject.SetActive(false); // Si ya está comprado, ocultamos el botón de compra
        }
        else
        {
            tmp.text = "Comprar";
            buyButton.gameObject.SetActive(true); // Si no está comprado, mostramos el botón de compra
        }
    }

    // --------- Interacciones ---------

    private void OnSelectClicked()
    {
        Debug.Log($"Avatar seleccionado: {avatarData.displayName}");

        // Si ya está seleccionado, no hacemos nada
        if (_isSelected)
        {
            return;
        }

        // Si hay otro avatar seleccionado, lo colapsamos
        if (currentlySelectedItem != null && currentlySelectedItem != this)
        {
            currentlySelectedItem.Deselect();
        }

        // Hacemos este avatar grande
        _isSelected = true;
        currentlySelectedItem = this;
        StartCoroutine(ScaleRoutine(_originalScale, _originalScale * selectedScale));

        // Mostramos los botones de "Comprar"
        if (!_isPurchased)  // Solo mostramos el botón de compra si no está comprado
        {
            if (buyButton != null) buyButton.gameObject.SetActive(true);
        }
        else
        {
            // Si ya está comprado, no mostrar el botón de "Equipar"
            if (buyButton != null) buyButton.gameObject.SetActive(false);
        }

        if (cancelButton != null) cancelButton.gameObject.SetActive(true);
    }

    private void OnCancelClicked()
    {
        if (!_isSelected) return;

        // Desmarcamos este avatar
        _isSelected = false;
        currentlySelectedItem = null;
        StartCoroutine(ScaleRoutine(transform.localScale, _originalScale));

        // Ocultamos los botones de "Comprar"
        if (buyButton != null) buyButton.gameObject.SetActive(false);
        if (cancelButton != null) cancelButton.gameObject.SetActive(false);
    }

    private void OnBuyClicked()
    {
        if (!_isPurchased)
        {
            // --- SISTEMA REAL DE COMPRA ---
            int currentCoins = CurrencyManager.Instance.GetCoins();

            if (currentCoins >= avatarData.price)
            {
                // Restar monedas
                CurrencyManager.Instance.SpendCoins(avatarData.price);

                // Marcar como comprado
                _isPurchased = true;
                PlayerPrefs.SetInt("AvatarPurchased_" + avatarData.id, 1);
                PlayerPrefs.Save();

                UpdateBuyText();  // Actualizamos el texto del botón para reflejar que ya fue comprado

                Debug.Log($"Avatar comprado: {avatarData.id}");
            }
            else
            {
                Debug.Log("No tienes suficientes monedas.");
                return;
            }
        }
        else
        {
            // Si ya está comprado, no hacemos nada
            Debug.Log("Este avatar ya está comprado.");
        }
    }

    // --------- Animación sencilla de escala ---------

    private IEnumerator ScaleRoutine(Vector3 from, Vector3 to)
    {
        float t = 0f;

        while (t < animDuration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / animDuration);
            transform.localScale = Vector3.Lerp(from, to, lerp);
            yield return null;
        }

        transform.localScale = to;
    }

    // --------- Deselect (desmarcar) ---------

    public void Deselect()
    {
        _isSelected = false;
        currentlySelectedItem = null;
        StartCoroutine(ScaleRoutine(transform.localScale, _originalScale));

        // Ocultamos los botones de "Comprar"
        if (buyButton != null) buyButton.gameObject.SetActive(false);
        if (cancelButton != null) cancelButton.gameObject.SetActive(false);
    }
}
