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

    [Header("Base de Datos de Avatares")]
    [SerializeField] private AvatarDataSO[] avatarDatabase;   // Asegúrate de agregar esta referencia en el Inspector

    [Header("Animación")]
    [SerializeField] private float popDuration = 0.25f;  // Duración de la animación de pop
    [SerializeField] private float popScale = 1.1f;      // Factor de escala para el pop (más grande de lo normal)
    [SerializeField] private AnimationCurve popCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);  // Curva para la animación de escala

    private bool isPanelVisible = false;  // Estado del panel (si está visible o no)
    private bool isAnimating = false;    // Si la animación está en curso

    private InventoryAvatarItem selectedAvatarItem = null;  // El avatar seleccionado

    private void Start()
    {
        // Comprobamos que las referencias están asignadas correctamente
        if (avatarDatabase == null || avatarDatabase.Length == 0)
        {
            Debug.LogError("AvatarDatabase no está asignado o está vacío.");
        }

        if (contentPanel == null)
        {
            Debug.LogError("contentPanel no está asignado.");
        }

        if (avatarItemPrefab == null)
        {
            Debug.LogError("avatarItemPrefab no está asignado.");
        }

        // Asignamos listeners
        openButton.onClick.AddListener(OpenPanel);
        closeButton.onClick.AddListener(ClosePanel);
        saveButton.onClick.AddListener(SaveSelectedAvatar);

        // Cargamos los avatares en el panel
        LoadAvatarsInPanel();

        // Iniciamos el panel cerrado (escala 0)
        panel.localScale = Vector3.zero;

        saveButton.gameObject.SetActive(true);  // Botón de "Guardar" siempre visible
        saveButton.interactable = false;  // Al principio, desactivamos el botón (no hay avatar seleccionado)
    }

    public void OpenPanel()
    {
        if (isAnimating || isPanelVisible) return;  // Si está animando o ya está visible, no hacer nada

        isPanelVisible = true;  // El panel ahora está visible
        StartCoroutine(PopPanel(Vector3.zero, Vector3.one * popScale));
    }

    public void ClosePanel()
    {
        if (isAnimating || !isPanelVisible) return;  // Si está animando o no está visible, no hacer nada

        isPanelVisible = false;  // El panel ahora está oculto
        StartCoroutine(PopPanel(Vector3.one * popScale, Vector3.zero));
    }

    private IEnumerator PopPanel(Vector3 from, Vector3 to)
    {
        isAnimating = true;  // Empieza la animación
        float t = 0f;

        while (t < popDuration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / popDuration);
            float eased = popCurve.Evaluate(lerp);  // Aplicamos la curva de animación

            panel.localScale = Vector3.Lerp(from, to, eased);  // Escala el panel
            yield return null;
        }

        panel.localScale = to;  // Aseguramos que el panel llega al tamaño final
        isAnimating = false;  // La animación ha terminado
    }

    private void LoadAvatarsInPanel()
    {
        // Verifica si el panel y avatarDatabase están configurados correctamente
        if (contentPanel == null || avatarDatabase == null)
        {
            Debug.LogError("No se puede cargar avatares. contentPanel o avatarDatabase no están asignados.");
            return;
        }

        // Limpiamos cualquier avatar anterior en el contenedor
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }

        // Buscar todos los avatares comprados
        var purchasedAvatars = GetPurchasedAvatars();

        // Instanciamos los avatares comprados
        foreach (var avatarId in purchasedAvatars)
        {
            GameObject avatarItemGO = Instantiate(avatarItemPrefab, contentPanel);
            InventoryAvatarItem avatarItem = avatarItemGO.GetComponent<InventoryAvatarItem>();
            AvatarDataSO avatarData = GetAvatarById(avatarId);
            avatarItem.Setup(avatarData);

            // Añadimos el listener al botón de selección
            avatarItem.selectButton.onClick.AddListener(() => OnAvatarSelected(avatarItem));
        }
    }

    private List<string> GetPurchasedAvatars()
    {
        // Devuelve una lista con los avatares comprados desde PlayerPrefs
        List<string> purchasedAvatars = new List<string>();

        foreach (var avatarData in avatarDatabase)   // Aquí accedemos al array avatarDatabase
        {
            string key = "AvatarPurchased_" + avatarData.id;
            if (PlayerPrefs.GetInt(key, 0) == 1)  // Verificamos si está marcado como comprado
            {
                purchasedAvatars.Add(avatarData.id);
            }
        }

        return purchasedAvatars;
    }

    private AvatarDataSO GetAvatarById(string id)
    {
        foreach (var avatar in avatarDatabase)
        {
            if (avatar.id == id)
                return avatar;
        }
        return null;
    }

    // Se llama cuando se selecciona un avatar
    public void OnAvatarSelected(InventoryAvatarItem avatarItem)
    {
        Debug.Log($"Avatar seleccionado: {avatarItem.GetAvatarData().displayName}");

        // Si ya hay un avatar seleccionado y es el mismo que el clickeado, no hacemos nada
        if (selectedAvatarItem == avatarItem)
        {
            return;  // El avatar ya está seleccionado, no hacemos nada
        }

        // Si ya hay un avatar seleccionado, lo deseleccionamos
        if (selectedAvatarItem != null)
        {
            selectedAvatarItem.Deselect();  // Deseleccionamos el avatar previamente seleccionado
        }

        // Ahora seleccionamos el nuevo avatar
        selectedAvatarItem = avatarItem;
        selectedAvatarItem.Select();  // Seleccionamos el avatar clickeado
        saveButton.interactable = true;  // Activamos el botón de guardar
    }

    // Guardamos el avatar seleccionado
    private void SaveSelectedAvatar()
    {
        if (selectedAvatarItem != null)
        {
            AvatarDataSO selectedAvatarData = selectedAvatarItem.GetAvatarData();
            EquipAvatar(selectedAvatarData);
            ClosePanel();  // Cerramos el panel

            // Actualizamos el avatar de inmediato (sin esperar a salir y reabrir el panel)
            StartCoroutine(WaitAndUpdateAvatar());
        }
    }

    private IEnumerator WaitAndUpdateAvatar()
    {
        // Esperamos un segundo para permitir que el panel se cierre correctamente
        yield return new WaitForSeconds(1);

        // Ahora actualizamos el avatar en el menú principal
        XPUIAnimation menuAvatar = FindFirstObjectByType<XPUIAnimation>();
        if (menuAvatar != null)
        {
            menuAvatar.LoadCurrentAvatarSprite();  // Reflejamos el cambio de avatar en el menú
        }

        LeaderboardUI.Instance.RefreshCurrentLeaderboard();
    }

    private void EquipAvatar(AvatarDataSO avatarData)
    {
        // 1. Guardar el avatar en PlayerPrefs como "EquippedAvatarId"
        PlayerPrefs.SetString("EquippedAvatarId", avatarData.id);
        PlayerPrefs.Save();

        // 2. Actualizar en PlayFab (si es necesario)
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

            // Actualizar UI del avatar
            XPUIAnimation menuAvatar = FindFirstObjectByType<XPUIAnimation>();
            if (menuAvatar != null)
                menuAvatar.LoadCurrentAvatarSprite();
        }, error =>
        {
            Debug.LogWarning("Error al actualizar avatar en PlayFab: " + error.GenerateErrorReport());
        });
    }
}
