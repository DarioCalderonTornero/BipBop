using System;
using System.Collections;
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

    // Límite de caracteres del nombre
    private const int MAX_NAME_LENGTH = 12;

    [Header("UI (assign in inspector)")]
    public GameObject namePanel;             // panel que pide el nombre
    public TMP_InputField nameInput;         // input para escribir el nombre
    public Button submitButton;              // botón para enviar el nombre
    public Button skipButton;                // botón para omitir (opcional)
    public GameObject loadingIndicator;      // spinner u objeto que muestra carga

    [Header("Feedback")]
    public TextMeshProUGUI feedbackText;     // texto de errores bajo el input

    [Header("DEBUG")]
    [Tooltip("Si lo marcas en el inspector, forzará que se abra el panel de nombre aunque ya tengas uno guardado.")]
    [SerializeField] private bool debugForceNamePanel = false;

    [Header("Debug nombre")]
    [SerializeField] private bool debugNameTestMode = false;

    // Lista de nombres que se considerarán "ocupados" en modo debug
    [SerializeField] private string[] debugTakenNames;

    [Header("Name counter")]
    [SerializeField] private TextMeshProUGUI nameCounterText;
    [SerializeField] private Color counterLowColor = Color.green;    // 0 - 1/3
    [SerializeField] private Color counterMidColor = Color.yellow;   // 1/3 - 2/3
    [SerializeField] private Color counterHighColor = Color.red;     // 2/3 - full

    // Estado
    public bool IsLoggedIn { get; private set; } = false;
    public string PlayFabId { get; private set; }
    public string DisplayName { get; private set; }

    private bool isLoggingIn = false;
    private Coroutine retryCoroutine;

    // Evento para notificar a otros sistemas que ya estamos logueados
    public event Action OnLoginSuccess;

    private void Awake()
    {
        if (string.IsNullOrEmpty(PlayFabSettings.TitleId))
        {
            PlayFabSettings.TitleId = "145AD3"; // tu TitleId
        }

        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        // seguridad: desactivar UI si están asignadas al inicio
        if (namePanel != null) namePanel.SetActive(false);
        if (loadingIndicator != null) loadingIndicator.SetActive(false);

        if (nameInput != null)
        {
            nameInput.onValueChanged.AddListener(OnNameInputChanged);
            // Aseguramos el límite también a nivel de input
            nameInput.characterLimit = MAX_NAME_LENGTH;
        }
    }

    private void Start()
    {
        TryLogin();  // en vez de StartLoginFlow directo

        if (nameInput != null)
            UpdateNameCounter(nameInput.text);
    }

    private void OnNameInputChanged(string value)
    {
        if (feedbackText != null)
            feedbackText.text = ""; // limpia mensajes anteriores mientras escribe

        UpdateNameCounter(value);
    }

    private void UpdateNameCounter(string currentText)
    {
        if (nameCounterText == null) return;

        int length = string.IsNullOrEmpty(currentText) ? 0 : currentText.Length;

        // Texto tipo "X / MAX"
        nameCounterText.text = $"{length} / {MAX_NAME_LENGTH}";

        // Proporción usada
        float ratio = (float)length / MAX_NAME_LENGTH;

        // Elegimos color según tercio
        if (ratio <= 1f / 3f)
        {
            nameCounterText.color = counterLowColor;    // verde
        }
        else if (ratio <= 2f / 3f)
        {
            nameCounterText.color = counterMidColor;    // naranja/amarillo
        }
        else
        {
            nameCounterText.color = counterHighColor;   // rojo
        }
    }

    public void TryLogin()
    {
        if (IsLoggedIn) return;
        if (isLoggingIn) return;

        // Si no hay internet, no intentamos, pero dejamos el sistema listo para reintentar
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            // opcional: si quieres mostrar algún aviso global aquí
            return;
        }

        StartLoginFlow();
    }

    private IEnumerator ShakeInputField()
    {
        if (nameInput == null) yield break;

        RectTransform rt = nameInput.GetComponent<RectTransform>();
        if (rt == null) yield break;

        Vector2 originalPos = rt.anchoredPosition;

        float duration = 0.18f;
        float elapsed = 0f;
        float amplitude = 10f;   // cuánto se mueve a los lados
        float frequency = 40f;   // velocidad de la vibración

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // por si el tiempo está pausado en alguna escena
            float offset = Mathf.Sin(elapsed * frequency) * amplitude;
            rt.anchoredPosition = originalPos + new Vector2(offset, 0f);
            yield return null;
        }

        rt.anchoredPosition = originalPos;
    }


    #region Login Flow
    public void StartLoginFlow()
    {
        if (IsLoggedIn) return;
        if (isLoggingIn) return;

        isLoggingIn = true;

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

    // Helper para modo debug: simula nombres ocupados
    private bool IsDebugNameTaken(string name)
    {
        if (debugTakenNames == null || debugTakenNames.Length == 0)
            return false;

        foreach (var taken in debugTakenNames)
        {
            if (string.IsNullOrEmpty(taken)) continue;
            if (string.Equals(taken.Trim(), name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void OnLoginSuccessInternal(LoginResult result)
    {
        isLoggingIn = false;
        ShowLoading(false);
        IsLoggedIn = true;
        PlayFabId = result.PlayFabId;
        Debug.Log($"PlayFab login OK - PlayFabId: {PlayFabId}");

        string localName = PlayerPrefs.GetString(PREF_DISPLAY_NAME, "");

        // DEBUG: fuerza mostrar el panel aunque ya hubiera nombre
        if (debugForceNamePanel)
        {
            debugForceNamePanel = false; // se auto-resetea
            if (namePanel != null)
            {
                namePanel.SetActive(true);

                if (submitButton != null)
                {
                    submitButton.onClick.RemoveAllListeners();
                    submitButton.onClick.AddListener(SubmitNameFromUI);
                }

                if (skipButton != null)
                {
                    skipButton.onClick.RemoveAllListeners();
                    skipButton.onClick.AddListener(OnSkipName);
                }
            }
            return;
        }



        // Flujo normal
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

                if (submitButton != null)
                {
                    submitButton.onClick.RemoveAllListeners();
                    submitButton.onClick.AddListener(SubmitNameFromUI);
                }

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
        isLoggingIn = false;

        Debug.LogWarning("PlayFab error: " + error.GenerateErrorReport());

        // Si el fallo puede ser por red, reintentamos cuando vuelva internet
        if (retryCoroutine == null)
            retryCoroutine = StartCoroutine(RetryWhenInternetReturns());
    }
    #endregion

    private IEnumerator RetryWhenInternetReturns()
    {
        // Espera a recuperar internet
        while (Application.internetReachability == NetworkReachability.NotReachable)
            yield return new WaitForSecondsRealtime(0.5f);

        // Espera un pelín por estabilidad
        yield return new WaitForSecondsRealtime(0.25f);

        retryCoroutine = null;
        TryLogin();
    }

    #region DisplayName handling
    // Llamado por el botón submit en la UI
    public void SubmitNameFromUI()
    {
        string typed = nameInput != null ? nameInput.text?.Trim() : "";

        // 1) Vacío
        if (string.IsNullOrEmpty(typed))
        {
            if (feedbackText != null)
                feedbackText.text = "El nombre no puede estar vacío.";
            return;
        }

        // 2) Longitud
        if (typed.Length > MAX_NAME_LENGTH)
        {
            if (feedbackText != null)
                feedbackText.text = $"Nombre demasiado largo, máximo {MAX_NAME_LENGTH} caracteres.";
            return;
        }

        // 3) MODO DEBUG: solo probar, NO guardar ni llamar a PlayFab
        if (debugNameTestMode)
        {
            if (IsDebugNameTaken(typed))
            {
                if (feedbackText != null)
                    feedbackText.text = "Ese nombre ya existe, elige otro.";

                // Agitar input en debug cuando el nombre "ya existe"
                StartCoroutine(ShakeInputField());
            }
            else
            {
                if (feedbackText != null)
                    feedbackText.text = "Nombre válido (modo prueba). No se ha guardado.";
            }

            return;
        }

        // 4) Modo real
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
            if (feedbackText != null) feedbackText.text = "";
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
                    nameInput.ActivateInputField();
                }

                StartCoroutine(ShakeInputField());
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
        OnLoginSuccess?.Invoke();
    }
    #endregion

    #region Helpers
    private void ShowLoading(bool show)
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(show);
    }

    public string GetLocalDisplayName()
    {
        return PlayerPrefs.GetString(PREF_DISPLAY_NAME, "");
    }
    #endregion
}
