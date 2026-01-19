using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundInventoryManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button saveButton;

    [Header("Grid Layout / Páginas")]
    [SerializeField] private GameObject backgroundItemPrefab;
    [SerializeField] private Transform[] pageContainers; // Page0, Page1, Page2...

    [Header("Catálogo de Fondos")]
    [SerializeField] private BackgroundCatalogSO backgroundCatalog;

    [Header("Fondo por defecto (siempre owned)")]
    [SerializeField] private string defaultBackgroundId = "DefaultBackground";

    [Header("Pager")]
    [SerializeField] private AvatarInventoryPager inventoryPager;

    [Header("Animación")]
    [SerializeField] private float popDuration = 0.25f;
    [SerializeField] private float popScale = 1.1f;
    [SerializeField] private AnimationCurve popCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Detalle selección")]
    [SerializeField] private TextMeshProUGUI selectedNameText;
    [SerializeField] private TextMeshProUGUI selectedDescriptionText;

    [Header("Textos descriptivos")]
    [SerializeField] private string defaultOwnedText = "¡Conseguido!";
    [SerializeField] private string defaultStoreText = "Comprable en la tienda.";
    [SerializeField] private string noSelectionText = "¡Elige un fondo!";

    private bool isPanelVisible;
    private bool isAnimating;

    private InventoryBackgroundItem selectedItem;

    private Vector2 originalPanelAnchoredPosition;

    [SerializeField] private Image equippedBackgroundPreview;

    [SerializeField] private TextMeshProUGUI titleText;

    private void Awake()
    {
        if (panel == null)
            panel = GetComponent<RectTransform>();

        originalPanelAnchoredPosition = panel.anchoredPosition;

        if (openButton != null)
        {
            openButton.onClick.RemoveAllListeners();
            openButton.onClick.AddListener(OpenPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePanel);
        }

        if (saveButton != null)
        {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(SaveSelectedBackground);
        }
    }

    private void Start()
    {
        EnsureDefaultBackgroundOwnedAndEquipped();

        LoadBackgroundsInPages();

        UpdateEquippedPreview();

        panel.localScale = Vector3.zero;

        if (saveButton != null)
            saveButton.interactable = false;

        if (selectedNameText != null) selectedNameText.text = "";
        if (selectedDescriptionText != null) selectedDescriptionText.text = noSelectionText;
    }

    private void EnsureDefaultBackgroundOwnedAndEquipped()
    {
        if (backgroundCatalog == null || string.IsNullOrEmpty(defaultBackgroundId))
            return;

        // (Opcional) comprobar que existe en el catálogo
        bool exists = backgroundCatalog.backgroundDataSO.Exists(b => b != null && b.id == defaultBackgroundId);
        if (!exists)
        {
            Debug.LogWarning("[BackgroundInventoryManager] defaultBackgroundId no existe en el catálogo: " + defaultBackgroundId);
            return;
        }

        // 1) Marcarlo como comprado siempre
        string purchaseKey = "Purchased_" + defaultBackgroundId;
        if (PlayerPrefs.GetInt(purchaseKey, 0) == 0)
            PlayerPrefs.SetInt(purchaseKey, 1);

        // 2) Equiparlo en cuentas nuevas (si no hay nada seleccionado o lo seleccionado es inválido)
        string selected = PlayerPrefs.GetString("SelectedBackground", "");

        bool hasValidSelection = false;
        if (!string.IsNullOrEmpty(selected))
        {
            hasValidSelection = backgroundCatalog.backgroundDataSO.Exists(b => b != null && b.id == selected);
        }

        if (!hasValidSelection)
            PlayerPrefs.SetString("SelectedBackground", defaultBackgroundId);

        PlayerPrefs.Save();
    }

    // ========================
    //  ABRIR / CERRAR
    // ========================

    private void UpdateEquippedPreview()
    {
        if (equippedBackgroundPreview == null || backgroundCatalog == null)
            return;

        string equippedId = PlayerPrefs.GetString("SelectedBackground", "");

        var data = backgroundCatalog.backgroundDataSO
            .Find(b => b != null && b.id == equippedId);

        if (data != null && data.sprite != null)
        {
            equippedBackgroundPreview.sprite = data.sprite;
            equippedBackgroundPreview.color = Color.white;
        }
    }
    public void OpenPanel()
    {
        if (isAnimating || isPanelVisible) return;

        isPanelVisible = true;

        panel.anchoredPosition = originalPanelAnchoredPosition;
        panel.localScale = Vector3.zero;

        selectedItem = null;

        if (selectedNameText != null) selectedNameText.text = "";
        if (selectedDescriptionText != null) selectedDescriptionText.text = noSelectionText;
        if (saveButton != null) saveButton.interactable = false;

        titleText.text = "Inventario de fondos";

        LoadBackgroundsInPages();

        StartCoroutine(OpenPanelRoutine());
    }

    private IEnumerator OpenPanelRoutine()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        if (inventoryPager != null)
            inventoryPager.HardResetToFirstPage();

        yield return StartCoroutine(PopPanel(Vector3.zero, Vector3.one * popScale));
    }

    public void ClosePanel()
    {
        if (isAnimating || !isPanelVisible) return;

        if (closeButton != null)
            StartCoroutine(PopButton(closeButton.transform as RectTransform));

        isPanelVisible = false;
        StartCoroutine(PopPanel(Vector3.one * popScale, Vector3.zero));
    }

    private IEnumerator PopButton(RectTransform button)
    {
        if (button == null) yield break;

        Vector3 original = button.localScale;
        Vector3 bigger = original * 1.12f;

        float duration = 0.12f;
        float half = duration * 0.5f;
        float t = 0f;

        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / half);
            button.localScale = Vector3.Lerp(original, bigger, p);
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
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
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / popDuration);
            float e = popCurve.Evaluate(p);
            panel.localScale = Vector3.Lerp(from, to, e);
            yield return null;
        }

        panel.localScale = to;
        isAnimating = false;
    }

    // ========================
    //  CARGA EN PÁGINAS
    // ========================

    private void LoadBackgroundsInPages()
    {
        if (backgroundCatalog == null || pageContainers == null || pageContainers.Length == 0 || backgroundItemPrefab == null)
        {
            Debug.LogError("[BackgroundInventoryManager] Faltan referencias (catalog/pages/prefab).");
            return;
        }

        foreach (var page in pageContainers)
        {
            if (page == null) continue;
            foreach (Transform child in page)
                Destroy(child.gameObject);
        }

        var list = backgroundCatalog.backgroundDataSO;
        if (list == null || list.Count == 0) return;

        const int ITEMS_PER_PAGE = 12;

        for (int i = 0; i < list.Count; i++)
        {
            var bg = list[i];
            if (bg == null) continue;

            int pageIndex = i / ITEMS_PER_PAGE;
            if (pageIndex >= pageContainers.Length) break;

            Transform parentPage = pageContainers[pageIndex];
            if (parentPage == null) continue;

            GameObject go = Instantiate(backgroundItemPrefab, parentPage);
            var item = go.GetComponent<InventoryBackgroundItem>();
            item.Setup(bg);
        }

        foreach (var page in pageContainers)
        {
            if (page == null) continue;
            if (page is RectTransform rt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }

    // ========================
    //  SELECCIÓN / GUARDAR
    // ========================

    public void OnBackgroundSelected(InventoryBackgroundItem item)
    {
        if (item == null) return;

        var data = item.GetData();
        if (data == null) return;

        if (selectedItem != null && selectedItem != item)
            selectedItem.Deselect();

        selectedItem = item;
        selectedItem.Select();

        if (selectedNameText != null)
            selectedNameText.text = data.displayName;

        if (selectedDescriptionText != null)
        {
            if (item.IsOwned)
            {
                selectedDescriptionText.text = defaultOwnedText;
            }
            else
            {
                // ✅ personalizada si existe
                if (!string.IsNullOrEmpty(data.unlockDescription))
                    selectedDescriptionText.text = data.unlockDescription;
                else
                    selectedDescriptionText.text = defaultStoreText;
            }
        }

        if (saveButton != null)
            saveButton.interactable = item.IsOwned;
    }

    private void SaveSelectedBackground()
    {
        if (selectedItem == null || !selectedItem.IsOwned) return;

        if (saveButton != null)
            StartCoroutine(PopButton(saveButton.transform as RectTransform));

        var data = selectedItem.GetData();
        EquipBackground(data);

        ClosePanel();
    }

    private void EquipBackground(BackgroundDataSO data)
    {
        if (data == null) return;

        PlayerPrefs.SetString("SelectedBackground", data.id);
        PlayerPrefs.Save();

        UpdateEquippedPreview();

        // Si quieres aplicar el fondo instant:
        // Busca tu sistema actual y llama aquí (por ejemplo un manager de UI que cambie el sprite).
        // Ejemplo:
        // var selector = FindFirstObjectByType<FondoSelector>();
        // if (selector != null) selector.CambiarFondo(data.sprite);
    }
}
