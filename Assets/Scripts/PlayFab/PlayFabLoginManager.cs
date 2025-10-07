using System;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayFabLoginManager : MonoBehaviour
{
    public static PlayFabLoginManager Instance { get; private set; }

    // Claves PlayerPrefs
    private const string PREF_CUSTOM_ID = "pf_custom_id";
    private const string PREF_DISPLAY_NAME = "pf_display_name";

    [Header("UI (assign in inspector)")]
    public GameObject namePanel;             // panel que pide el nombre (SetActive true/false)
    public TMP_InputField nameInput;         // input para escribir el nombre
    public Button submitButton;              // botón para enviar el nombre
    public Button skipButton;                // (opcional) botón para omitir
    public GameObject loadingIndicator;      // spinner u objeto que muestra carga

    // Estado
    public bool IsLoggedIn { get; private set; } = false;
    public string PlayFabId { get; private set; }
    public string DisplayName { get; private set; }

    // Evento para notificar a otros sistemas que ya estamos logueados
    public event Action OnLoginSuccess;

    private void Awake()
    {
        if (string.IsNullOrEmpty(PlayFabSettings.TitleId))
        {
            PlayFabSettings.TitleId = "145AD3"; // pega tu TitleId
        }

        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        // seguridad: desactivar UI si están asignadas al inicio
        if (namePanel != null) namePanel.SetActive(false);
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
    }

    private void Start()
    {
        StartLoginFlow();
    }

    #region Login Flow
    public void StartLoginFlow()
    {
        string customId = PlayerPrefs.GetString(PREF_CUSTOM_ID, "");
        if (string.IsNullOrEmpty(customId))
        {
            customId = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(PREF_CUSTOM_ID, customId);
            PlayerPrefs.Save();
        }

        ShowLoading(true);
        var request = new LoginWithCustomIDRequest
        {
            CustomId = customId,
            CreateAccount = true
        };

        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccessInternal, OnPlayFabError);
    }

    private void OnLoginSuccessInternal(LoginResult result)
    {
        ShowLoading(false);
        IsLoggedIn = true;
        PlayFabId = result.PlayFabId;
        Debug.Log($"PlayFab login OK - PlayFabId: {PlayFabId}");

        // Si localmente ya teníamos un display name, sincronizarlo
        string localName = PlayerPrefs.GetString(PREF_DISPLAY_NAME, "");
        if (!string.IsNullOrEmpty(localName))
        {
            // aseguramos que PlayFab tenga el nombre (opcional)
            UpdateDisplayNameIfNeeded(localName);
        }
        else
        {
            // Pedir nombre al usuario (si namePanel asignado)
            if (namePanel != null)
            {
                namePanel.SetActive(true);
                // Configurar listeners si no están ya
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(SubmitNameFromUI);

                if (skipButton != null)
                {
                    skipButton.onClick.RemoveAllListeners();
                    skipButton.onClick.AddListener(OnSkipName);
                }
            }
            else
            {
                // No hay UI para pedir nombre -> consideramos completado
                FinalizeLogin();
            }
        }
    }

    private void OnPlayFabError(PlayFabError error)
    {
        ShowLoading(false);
        Debug.LogWarning("PlayFab error: " + error.GenerateErrorReport());
        // Aquí puedes mostrar UI de "volver a intentar"
    }
    #endregion

    #region DisplayName handling
    // Llamado por el botón submit en la UI
    public void SubmitNameFromUI()
    {
        string typed = nameInput != null ? nameInput.text?.Trim() : "";
        if (string.IsNullOrEmpty(typed))
        {
            Debug.Log("Nombre vacío. Ignorado.");
            return;
        }

        SetDisplayName(typed);
    }

    // Opción "omitir": generamos un nombre por defecto y lo aplicamos
    private void OnSkipName()
    {
        string generated = "Player" + UnityEngine.Random.Range(1000, 9999);
        SetDisplayName(generated);
        if (namePanel != null) namePanel.SetActive(false);
    }

    // Settea displayName en PlayFab y guarda local
    public TextMeshProUGUI feedbackText; // <- Asigna un TextMeshPro debajo del input

    public void SetDisplayName(string newName)
    {
        ShowLoading(true);

        var req = new UpdateUserTitleDisplayNameRequest { DisplayName = newName };

        PlayFabClientAPI.UpdateUserTitleDisplayName(req, res =>
        {
            ShowLoading(false);

            DisplayName = res.DisplayName;
            PlayerPrefs.SetString(PREF_DISPLAY_NAME, DisplayName);
            PlayerPrefs.Save();

            Debug.Log("DisplayName guardado: " + DisplayName);
            if (feedbackText != null) feedbackText.text = ""; // limpiar mensaje si había error anterior
            if (namePanel != null) namePanel.SetActive(false);

            FinalizeLogin();

        }, error =>
        {
            ShowLoading(false);

            if (error.Error == PlayFabErrorCode.NameNotAvailable)
            {
                Debug.LogWarning("Ese nombre ya existe, elige otro.");
                if (feedbackText != null)
                    feedbackText.text = "Ese nombre ya existe, elige otro.";

                if (nameInput != null)
                {
                    nameInput.text = "";
                    nameInput.Select();
                    nameInput.ActivateInputField(); // enfocar para volver a escribir
                }
            }
            else
            {
                Debug.LogWarning("Error al establecer displayName: " + error.GenerateErrorReport());
                if (feedbackText != null)
                    feedbackText.text = "Error al asignar nombre, inténtalo de nuevo.";
            }
        });
    }

    // Si localName ya existía, forzamos su presencia en PlayFab
    private void UpdateDisplayNameIfNeeded(string localName)
    {
        ShowLoading(true);
        var req = new UpdateUserTitleDisplayNameRequest { DisplayName = localName };
        PlayFabClientAPI.UpdateUserTitleDisplayName(req, res =>
        {
            ShowLoading(false);
            DisplayName = res.DisplayName;
            Debug.Log("DisplayName sincronizado: " + DisplayName);
            FinalizeLogin();
        }, err =>
        {
            ShowLoading(false);
            Debug.LogWarning("No se pudo sincronizar displayName: " + err.GenerateErrorReport());
            FinalizeLogin();
        });
    }

    private void FinalizeLogin()
    {
        // Ya tenemos PlayFabId y posiblemente DisplayName
        OnLoginSuccess?.Invoke();
    }
    #endregion

    #region Helpers
    private void ShowLoading(bool show)
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(show);
    }

    // Método público para que otras clases obtengan el displayName guardado en local
    public string GetLocalDisplayName()
    {
        return PlayerPrefs.GetString(PREF_DISPLAY_NAME, "");
    }
    #endregion
}
