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
    [SerializeField] private Button buyEquipButton;    // botón que dice "Comprar" o "Equipar"
    [SerializeField] private Button cancelButton;      // botón "Cancelar"

    [Header("Animación")]
    [SerializeField] private float selectedScale = 1.1f;
    [SerializeField] private float animDuration = 0.12f;

    private Vector3 _originalScale;
    private bool _isSelected = false;
    private bool _isPurchased = false;

    private void Awake()
    {
        _originalScale = transform.localScale;

        // Ocultamos botones al inicio
        if (buyEquipButton != null) buyEquipButton.gameObject.SetActive(false);
        if (cancelButton != null) cancelButton.gameObject.SetActive(false);

        // Listeners
        if (selectButton != null)
            selectButton.onClick.AddListener(OnSelectClicked);

        if (buyEquipButton != null)
            buyEquipButton.onClick.AddListener(OnBuyEquipClicked);

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

        UpdateBuyEquipText();
    }

    private void UpdateBuyEquipText()
    {
        if (buyEquipButton == null) return;

        var tmp = buyEquipButton.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null) return;

        tmp.text = _isPurchased ? "Equipar" : "Comprar";
    }

    // --------- Interacciones ---------

    private void OnSelectClicked()
    {
        if (_isSelected)
        {
            // Si ya está seleccionado, no hacemos nada (o podrías replegarlo)
            return;
        }

        _isSelected = true;
        StartCoroutine(ScaleRoutine(_originalScale, _originalScale * selectedScale));

        if (buyEquipButton != null) buyEquipButton.gameObject.SetActive(true);
        if (cancelButton != null) cancelButton.gameObject.SetActive(true);
    }

    private void OnCancelClicked()
    {
        if (!_isSelected) return;

        _isSelected = false;
        StartCoroutine(ScaleRoutine(transform.localScale, _originalScale));

        if (buyEquipButton != null) buyEquipButton.gameObject.SetActive(false);
        if (cancelButton != null) cancelButton.gameObject.SetActive(false);
    }

    private void OnBuyEquipClicked()
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

                UpdateBuyEquipText();
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
            // --- EQUIPAR ---
            Debug.Log($"Equipar avatar: {avatarData.id}");

            // 1) Guardar localmente
            PlayerPrefs.SetString("EquippedAvatarId", avatarData.id);
            PlayerPrefs.Save();

            // 2) Subir a PlayFab como UserData para que otros vean tu avatar en el ranking
            var request = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string>
        {
            { "EquippedAvatarId", avatarData.id }
        }
            };

            PlayFabClientAPI.UpdateUserData(
                request,
                result => { Debug.Log("EquippedAvatarId actualizado en PlayFab"); },
                error => { Debug.LogWarning("Error al actualizar EquippedAvatarId: " + error.GenerateErrorReport()); }
            );

            // 3) Actualizar avatar del menú (misma escena)
            MainMenuAvatar menuAvatar = FindObjectOfType<MainMenuAvatar>();
            if (menuAvatar != null)
                menuAvatar.LoadEquippedAvatar();

            OnCancelClicked(); // opcional: cerrar selección
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
}
