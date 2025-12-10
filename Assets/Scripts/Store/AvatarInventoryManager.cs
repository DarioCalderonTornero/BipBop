using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AvatarInventoryManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button saveButton;

    [Header("Grid Layout / Páginas")]
    [SerializeField] private GameObject avatarItemPrefab;
    [SerializeField] private Transform[] pageContainers;    // Page0, Page1, Page2...

    [Header("Catálogo de Avatares")]
    [SerializeField] private AvatarCatalogSO avatarCatalog;

    [Header("Avatar por defecto")]
    [SerializeField] private string defaultAvatarId = "NormalAvatar";

    [Header("Animación")]
    [SerializeField] private float popDuration = 0.25f;
    [SerializeField] private float popScale = 1.1f;
    [SerializeField] private AnimationCurve popCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Detalle selección")]
    [SerializeField] private TextMeshProUGUI selectedAvatarNameText;
    [SerializeField] private TextMeshProUGUI selectedAvatarDescriptionText;

    [Header("Textos descriptivos")]
    [SerializeField] private string defaultOwnedText = "¡Conseguido!";
    [SerializeField] private string defaultStoreText = "Comprable en la tienda.";
    [SerializeField] private string defaultScoreTemplate = "Alcanza {0} puntos para desbloquearlo.";

    private bool isPanelVisible = false;
    private bool isAnimating = false;

    private InventoryAvatarItem selectedAvatarItem = null;

    private void Start()
    {
        if (avatarCatalog == null)
            Debug.LogError("AvatarCatalog no está asignado.");

        if (avatarItemPrefab == null)
            Debug.LogError("avatarItemPrefab no está asignado.");

        if (pageContainers == null || pageContainers.Length == 0)
            Debug.LogError("pageContainers no está asignado.");

        EnsureDefaultAvatarOwnedAndEquipped();

        if (openButton != null) openButton.onClick.AddListener(OpenPanel);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        if (saveButton != null) saveButton.onClick.AddListener(SaveSelectedAvatar);

        LoadAvatarsInPages();

        panel.localScale = Vector3.zero;
        if (saveButton != null)
        {
            saveButton.gameObject.SetActive(true);
            saveButton.interactable = false;
        }

        // Limpiar textos iniciales
        if (selectedAvatarNameText != null) selectedAvatarNameText.text = "";
        if (selectedAvatarDescriptionText != null) selectedAvatarDescriptionText.text = "";
    }

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

        string purchaseKey = "AvatarPurchased_" + defaultAvatarId;
        if (PlayerPrefs.GetInt(purchaseKey, 0) == 0)
        {
            PlayerPrefs.SetInt(purchaseKey, 1);
        }

        if (!PlayerPrefs.HasKey("EquippedAvatarId"))
        {
            PlayerPrefs.SetString("EquippedAvatarId", defaultAvatarId);
        }

        PlayerPrefs.Save();
    }

    // ========================
    //  ABRIR / CERRAR PANEL
    // ========================

    public void OpenPanel()
    {
        if (isAnimating || isPanelVisible) return;

        isPanelVisible = true;
        StartCoroutine(PopPanel(Vector3.zero, Vector3.one * popScale));

        LoadAvatarsInPages();
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

    // ========================
    //  CARGA EN PÁGINAS
    // ========================

    private void LoadAvatarsInPages()
    {
        if (avatarCatalog == null || pageContainers == null || pageContainers.Length == 0)
        {
            Debug.LogError("No se puede cargar avatares. Falta avatarCatalog o pageContainers.");
            return;
        }

        foreach (var page in pageContainers)
        {
            if (page == null) continue;
            foreach (Transform child in page)
                Destroy(child.gameObject);
        }

        var list = avatarCatalog.avatarDataSO;
        if (list == null || list.Count == 0)
            return;

        const int AVATARS_PER_PAGE = 15; // 5x3

        for (int i = 0; i < list.Count; i++)
        {
            var avatarData = list[i];
            if (avatarData == null) continue;

            int pageIndex = i / AVATARS_PER_PAGE;
            if (pageIndex >= pageContainers.Length)
            {
                Debug.LogWarning("[AvatarInventoryManager] Hay más avatares que páginas configuradas.");
                break;
            }

            Transform parentPage = pageContainers[pageIndex];
            if (parentPage == null) continue;

            GameObject avatarItemGO = Instantiate(avatarItemPrefab, parentPage);
            InventoryAvatarItem avatarItem = avatarItemGO.GetComponent<InventoryAvatarItem>();
            avatarItem.Setup(avatarData);
        }
    }

    // ========================
    //  SELECCIÓN / GUARDAR
    // ========================

    public void OnAvatarSelected(InventoryAvatarItem avatarItem)
    {
        var data = avatarItem.GetAvatarData();
        Debug.Log($"Avatar seleccionado: {data.displayName}");

        if (selectedAvatarItem != null && selectedAvatarItem != avatarItem)
            selectedAvatarItem.Deselect();

        selectedAvatarItem = avatarItem;
        selectedAvatarItem.Select();

        // 🔹 Actualizar nombre arriba
        if (selectedAvatarNameText != null)
            selectedAvatarNameText.text = data.displayName;

        // 🔹 Actualizar descripción
        if (selectedAvatarDescriptionText != null)
        {
            if (avatarItem.IsOwned)
            {
                // Ya lo tienes → "¡Conseguido!"
                selectedAvatarDescriptionText.text = defaultOwnedText;
            }
            else
            {
                // No lo tienes → descripción de cómo se desbloquea
                if (!string.IsNullOrEmpty(data.unlockDescription))
                {
                    selectedAvatarDescriptionText.text = data.unlockDescription;
                }
                else if (data.unlockByScore && data.requiredScoreValue > 0)
                {
                    // Fallback por puntuación si no rellenaste el texto
                    selectedAvatarDescriptionText.text =
                        string.Format(defaultScoreTemplate, data.requiredScoreValue);
                }
                else if (data.price > 0)
                {
                    selectedAvatarDescriptionText.text = defaultStoreText;
                }
                else
                {
                    selectedAvatarDescriptionText.text = "";
                }
            }
        }

        // 🔹 Solo puedes guardar si está conseguido
        if (saveButton != null)
            saveButton.interactable = avatarItem.IsOwned;
    }

    private void SaveSelectedAvatar()
    {
        if (selectedAvatarItem != null && selectedAvatarItem.IsOwned)
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

        PlayFab.PlayFabClientAPI.UpdateUserData(
            request,
            result =>
            {
                Debug.Log("Avatar equipados en PlayFab");
                XPUIAnimation menuAvatar = FindFirstObjectByType<XPUIAnimation>();
                if (menuAvatar != null)
                    menuAvatar.LoadCurrentAvatarSprite();
            },
            error =>
            {
                Debug.LogWarning("Error al actualizar avatar en PlayFab: " + error.GenerateErrorReport());
            });
    }
}
