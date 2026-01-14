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

    [Header("Pager")]
    [SerializeField] private AvatarInventoryPager inventoryPager;

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
    [SerializeField] private string noSelectionText = "¡Elige un avatar!";

    private bool isPanelVisible = false;
    private bool isAnimating = false;

    private InventoryAvatarItem selectedAvatarItem = null;

    // Posición original del panel tal y como está en el editor
    private Vector2 originalPanelAnchoredPosition;

    [SerializeField] private TextMeshProUGUI titleText;

    private void Awake()
    {
        if (panel == null)
            panel = GetComponent<RectTransform>();

        originalPanelAnchoredPosition = panel.anchoredPosition;
    }

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

        // Panel cerrado por escala, pero posición original intacta
        panel.localScale = Vector3.zero;

        if (saveButton != null)
        {
            saveButton.gameObject.SetActive(true);
            saveButton.interactable = false;
        }

        // Textos iniciales: nada seleccionado aún
        if (selectedAvatarNameText != null) selectedAvatarNameText.text = "";
        if (selectedAvatarDescriptionText != null) selectedAvatarDescriptionText.text = noSelectionText;
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

        // Reset posición y escala
        panel.anchoredPosition = originalPanelAnchoredPosition;
        panel.localScale = Vector3.zero;

        // Reset selección
        selectedAvatarItem = null;

        if (selectedAvatarNameText != null) selectedAvatarNameText.text = "";
        if (selectedAvatarDescriptionText != null) selectedAvatarDescriptionText.text = noSelectionText;
        if (saveButton != null) saveButton.interactable = false;

        titleText.text = "Inventario de avatares";

        // Cargamos los avatares
        LoadAvatarsInPages();

        // El resto en una corrutina para esperar al layout
        StartCoroutine(OpenPanelRoutine());
    }

    private IEnumerator OpenPanelRoutine()
    {
        // Esperar al final de frame para que el ScrollRect y GridLayoutGroup
        // calculen bien tamaños y posiciones (sobre todo en móvil)
        yield return null;
        Canvas.ForceUpdateCanvases();

        // Forzamos la primera página desde el pager
        if (inventoryPager != null)
        {
            inventoryPager.HardResetToFirstPage();
        }

        // Animación de pop del panel
        StartCoroutine(PopPanel(Vector3.zero, Vector3.one * popScale));
    }

    public void ClosePanel()
    {
        if (isAnimating || !isPanelVisible) return;

        // POP en la X
        if (closeButton != null)
            StartCoroutine(PopButton(closeButton.transform as RectTransform));

        isPanelVisible = false;
        StartCoroutine(PopPanel(Vector3.one * popScale, Vector3.zero));
    }

    // ========================
    //  POP DE BOTONES / PANEL
    // ========================
    private IEnumerator PopButton(RectTransform button)
    {
        if (button == null) yield break;

        Vector3 original = button.localScale;
        Vector3 bigger = original * 1.12f;

        float duration = 0.12f;
        float half = duration * 0.5f;
        float t = 0f;

        // Subida
        while (t < half)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / half);
            button.localScale = Vector3.Lerp(original, bigger, p);
            yield return null;
        }

        // Bajada
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / half);
            button.localScale = Vector3.Lerp(bigger, original, p);
            yield return null;
        }

        button.localScale = original;
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

        // Limpiar páginas
        foreach (var page in pageContainers)
        {
            if (page == null) continue;
            foreach (Transform child in page)
                Destroy(child.gameObject);
        }

        var list = avatarCatalog.avatarDataSO;
        if (list == null || list.Count == 0)
            return;

        const int AVATARS_PER_PAGE = 12; // 5x3

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

        // Rebuild layout para evitar glitches en móvil
        foreach (var page in pageContainers)
        {
            if (page == null) continue;
            var rt = page as RectTransform;
            if (rt != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
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

        if (selectedAvatarNameText != null)
            selectedAvatarNameText.text = data.displayName;

        if (selectedAvatarDescriptionText != null)
        {
            if (avatarItem.IsOwned)
            {
                selectedAvatarDescriptionText.text = defaultOwnedText;
            }
            else
            {
                if (!string.IsNullOrEmpty(data.unlockDescription))
                {
                    selectedAvatarDescriptionText.text = data.unlockDescription;
                }
                else if (data.unlockByScore && data.requiredScoreValue > 0)
                {
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

        if (saveButton != null)
            saveButton.interactable = avatarItem.IsOwned;
    }

    private void SaveSelectedAvatar()
    {
        if (selectedAvatarItem != null && selectedAvatarItem.IsOwned)
        {
            if (saveButton != null)
                StartCoroutine(PopButton(saveButton.transform as RectTransform));

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
            menuAvatar.UpdateOpenButtonAvatar();
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
                menuAvatar.UpdateOpenButtonAvatar();
            },
            error =>
            {
                Debug.LogWarning("Error al actualizar avatar en PlayFab: " + error.GenerateErrorReport());
            });
    }
}
