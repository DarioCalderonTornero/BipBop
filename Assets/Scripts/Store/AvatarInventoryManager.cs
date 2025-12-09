using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AvatarInventoryManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform panel;   // Panel a mostrar/ocultar
    [SerializeField] private Button openButton;     // Botón para abrir el panel
    [SerializeField] private Button closeButton;    // Botón para cerrar el panel
    [SerializeField] private Button saveButton;     // Botón para guardar la selección

    [Header("Grid Layout")]
    [SerializeField] private GameObject avatarItemPrefab;  // Prefab para cada item de avatar
    [SerializeField] private Transform contentPanel;       // Panel donde se instanciarán los avatares

    [Header("Catalogo de Avatares")]
    [SerializeField] private AvatarCatalogSO avatarCatalog;   // El catálogo de avatares

    [Header("Avatar por defecto")]
    [SerializeField] private string defaultAvatarId = "NormalAvatar"; // 👈 ID del avatar que SIEMPRE estará comprado

    [Header("Animación")]
    [SerializeField] private float popDuration = 0.25f;
    [SerializeField] private float popScale = 1.1f;
    [SerializeField] private AnimationCurve popCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool isPanelVisible = false;
    private bool isAnimating = false;

    private InventoryAvatarItem selectedAvatarItem = null;

    private void Start()
    {
        // Comprobamos referencias
        if (avatarCatalog == null)
        {
            Debug.LogError("AvatarCatalog no está asignado.");
        }

        if (contentPanel == null)
        {
            Debug.LogError("contentPanel no está asignado.");
        }

        if (avatarItemPrefab == null)
        {
            Debug.LogError("avatarItemPrefab no está asignado.");
        }

        // 🔹 MUY IMPORTANTE: asegurar que el avatar por defecto está comprado y equipado
        EnsureDefaultAvatarOwnedAndEquipped();

        // Listeners
        openButton.onClick.AddListener(OpenPanel);
        closeButton.onClick.AddListener(ClosePanel);
        saveButton.onClick.AddListener(SaveSelectedAvatar);

        // Cargamos los avatares en el panel
        LoadAvatarsInPanel();

        // Panel cerrado
        panel.localScale = Vector3.zero;

        saveButton.gameObject.SetActive(true);
        saveButton.interactable = false;
    }

    /// <summary>
    /// Se asegura de que el avatar por defecto:
    /// - existe en el catálogo
    /// - está marcado como comprado en PlayerPrefs
    /// - esté equipado la PRIMERA vez que entras (si no hay EquippedAvatarId aún)
    /// </summary>
    private void EnsureDefaultAvatarOwnedAndEquipped()
    {
        if (string.IsNullOrEmpty(defaultAvatarId) || avatarCatalog == null)
            return;

        bool existsInCatalog = avatarCatalog.avatarDataSO.Exists(a => a != null && a.id == defaultAvatarId);
        if (!existsInCatalog)
        {
            Debug.LogWarning("[AvatarInventoryManager] defaultAvatarId no existe en el catálogo: " + defaultAvatarId);
            return;
        }

        // Marcar como comprado
        string purchaseKey = "AvatarPurchased_" + defaultAvatarId;
        if (PlayerPrefs.GetInt(purchaseKey, 0) == 0)
        {
            PlayerPrefs.SetInt(purchaseKey, 1);
        }

        // Si el jugador aún no tiene ningún avatar equipado, ponemos este por defecto
        if (!PlayerPrefs.HasKey("EquippedAvatarId"))
        {
            PlayerPrefs.SetString("EquippedAvatarId", defaultAvatarId);
        }

        PlayerPrefs.Save();
    }

    public void OpenPanel()
    {
        if (isAnimating || isPanelVisible) return;

        isPanelVisible = true;
        StartCoroutine(PopPanel(Vector3.zero, Vector3.one * popScale));

        LoadAvatarsInPanel();
    }

    public void ClosePanel()
    {
        if (isAnimating || !isPanelVisible) return;

        isPanelVisible = false;
        StartCoroutine(PopPanel(Vector3.one * popScale, Vector3.zero));
    }

    private IEnumerator PopPanel(Vector3 from, Vector3 to)
    {
        isAnimating = true;
        float t = 0f;

        while (t < popDuration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / popDuration);
            float eased = popCurve.Evaluate(lerp);

            panel.localScale = Vector3.Lerp(from, to, eased);
            yield return null;
        }

        panel.localScale = to;
        isAnimating = false;
    }

    private void LoadAvatarsInPanel()
    {
        if (contentPanel == null || avatarCatalog == null)
        {
            Debug.LogError("No se puede cargar avatares. contentPanel o avatarCatalog no están asignados.");
            return;
        }

        foreach (Transform child in contentPanel)
            Destroy(child.gameObject);

        foreach (var avatarData in avatarCatalog.avatarDataSO)
        {
            if (avatarData == null) continue;

            GameObject avatarItemGO = Instantiate(avatarItemPrefab, contentPanel);
            InventoryAvatarItem avatarItem = avatarItemGO.GetComponent<InventoryAvatarItem>();
            avatarItem.Setup(avatarData);
        }
    }

    // Se llama cuando se selecciona un avatar
    public void OnAvatarSelected(InventoryAvatarItem avatarItem)
    {
        Debug.Log($"Avatar seleccionado: {avatarItem.GetAvatarData().displayName}");

        if (selectedAvatarItem == avatarItem)
            return;

        if (selectedAvatarItem != null)
            selectedAvatarItem.Deselect();

        selectedAvatarItem = avatarItem;
        selectedAvatarItem.Select();
        saveButton.interactable = true;
    }

    private void SaveSelectedAvatar()
    {
        if (selectedAvatarItem != null)
        {
            AvatarDataSO selectedAvatarData = selectedAvatarItem.GetAvatarData();
            EquipAvatar(selectedAvatarData);
            ClosePanel();

            StartCoroutine(WaitAndUpdateAvatar());
        }
    }

    private IEnumerator WaitAndUpdateAvatar()
    {
        yield return new WaitForSeconds(1);

        XPUIAnimation menuAvatar = FindFirstObjectByType<XPUIAnimation>();
        if (menuAvatar != null)
        {
            menuAvatar.LoadCurrentAvatarSprite();
        }

        LeaderboardUI.Instance.RefreshCurrentLeaderboard();
    }

    private void EquipAvatar(AvatarDataSO avatarData)
    {
        PlayerPrefs.SetString("EquippedAvatarId", avatarData.id);
        PlayerPrefs.Save();

        var request = new PlayFab.ClientModels.UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "EquippedAvatarIdPublic", avatarData.id }
            },
            Permission = PlayFab.ClientModels.UserDataPermission.Public
        };

        PlayFab.PlayFabClientAPI.UpdateUserData(request, result =>
        {
            Debug.Log("Avatar equipados en PlayFab");

            XPUIAnimation menuAvatar = FindFirstObjectByType<XPUIAnimation>();
            if (menuAvatar != null)
                menuAvatar.LoadCurrentAvatarSprite();
        }, error =>
        {
            Debug.LogWarning("Error al actualizar avatar en PlayFab: " + error.GenerateErrorReport());
        });
    }
}
