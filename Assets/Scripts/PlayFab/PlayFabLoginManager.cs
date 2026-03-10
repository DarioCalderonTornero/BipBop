using System;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class PlayFabLoginManager : MonoBehaviour
{
    public static PlayFabLoginManager Instance { get; private set; }

    private const string PREF_CUSTOM_ID = "pf_custom_id";
    private const string PREF_DISPLAY_NAME = "pf_display_name";

    private const int MAX_NAME_LENGTH = 12;

    [Header("UI (assign in inspector)")]
    public GameObject namePanel;
    public TMP_InputField nameInput;
    public Button submitButton;
    public Button skipButton;
    public GameObject loadingIndicator;

    [Header("Feedback")]
    public TextMeshProUGUI feedbackText;

    [Header("Localized Feedback")]
    [SerializeField] private LocalizedString inappropriateNameLocalized;
    [SerializeField] private LocalizedString nameEmptyLocalized;
    [SerializeField] private LocalizedString nameTooLongLocalized;
    [SerializeField] private LocalizedString nameTakenLocalized;
    [SerializeField] private LocalizedString nameErrorLocalized;

    [Header("DEBUG")]
    [SerializeField] private bool debugForceNamePanel = false;

    [Header("Debug nombre")]
    [SerializeField] private bool debugNameTestMode = false;

    [SerializeField] private string[] debugTakenNames;

    [Header("Name counter")]
    [SerializeField] private TextMeshProUGUI nameCounterText;
    [SerializeField] private Color counterLowColor = Color.green;
    [SerializeField] private Color counterMidColor = Color.yellow;
    [SerializeField] private Color counterHighColor = Color.red;

    public bool IsLoggedIn { get; private set; } = false;
    public string PlayFabId { get; private set; }
    public string DisplayName { get; private set; }

    private bool isLoggingIn = false;
    private Coroutine retryCoroutine;

    public event Action OnLoginSuccess;

    // Lista de palabras vetadas
    private static readonly string[] bannedTerms = new string[]
    {
        "puta","puto","gilipollas","idiota","imbecil","cabron","maricon", "maricón", "maric0n", "mariconcillo", "maric0ncill0",
        "mierda","joder","pene","vagina","porno","follar",
        "nazi","hitler",
        "fuck","shit","bitch","asshole","bastard","dick",
        "pussy","cunt","porn","rape", "put0", "p3n3", "v@g1na", "f0llar", "n4z", "Puta", "Puto", "Gilipollas", "Idiota", "Imbecil", "Cabron","Mierda", "Joder",
        "Put0", "P3n3", "V@g1na", "F0llar", "N4z", "Hitler", "Dick", "Pussy", "Fuck", "Asshole"
    };

    private const string LanguageKey = "SelectedLocale";

    [Header("Language UI")]
    [SerializeField] private Button languageButton;
    [SerializeField] private TextMeshProUGUI languageButtonText;

    private void Awake()
    {
        if (string.IsNullOrEmpty(PlayFabSettings.TitleId))
        {
            PlayFabSettings.TitleId = "145AD3";
        }

        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        if (namePanel != null) namePanel.SetActive(false);
        if (loadingIndicator != null) loadingIndicator.SetActive(false);

        if (nameInput != null)
        {
            nameInput.onValueChanged.AddListener(OnNameInputChanged);
            nameInput.characterLimit = MAX_NAME_LENGTH;
        }

        if (languageButton != null)
        {
            languageButton.onClick.RemoveAllListeners();
            languageButton.onClick.AddListener(ToggleLanguage);
        }

        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
    }

    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;

        ApplySavedLanguage();
        RefreshLanguageUI(LocalizationSettings.SelectedLocale);

        TryLogin();

        if (nameInput != null)
            UpdateNameCounter(nameInput.text);
    }

    private void OnNameInputChanged(string value)
    {
        if (feedbackText != null && !string.IsNullOrEmpty(value))
        {
            feedbackText.text = "";
        }

        UpdateNameCounter(value);
    }

    private void UpdateNameCounter(string currentText)
    {
        if (nameCounterText == null) return;

        int length = string.IsNullOrEmpty(currentText) ? 0 : currentText.Length;

        nameCounterText.text = $"{length} / {MAX_NAME_LENGTH}";

        float ratio = (float)length / MAX_NAME_LENGTH;

        if (ratio <= 1f / 3f)
            nameCounterText.color = counterLowColor;
        else if (ratio <= 2f / 3f)
            nameCounterText.color = counterMidColor;
        else
            nameCounterText.color = counterHighColor;
    }

    public void TryLogin()
    {
        if (IsLoggedIn) return;
        if (isLoggingIn) return;

        if (Application.internetReachability == NetworkReachability.NotReachable)
            return;

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
        float amplitude = 10f;
        float frequency = 40f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float offset = Mathf.Sin(elapsed * frequency) * amplitude;
            rt.anchoredPosition = originalPos + new Vector2(offset, 0f);
            yield return null;
        }

        rt.anchoredPosition = originalPos;
    }

    #region Moderation

    private bool IsInappropriateName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        string normalized = NormalizeNameForModeration(input);

        foreach (string term in bannedTerms)
        {
            string normalizedTerm = NormalizeNameForModeration(term);

            if (normalized.Contains(normalizedTerm))
                return true;
        }

        return false;
    }

    private string NormalizeNameForModeration(string input)
    {
        string text = input.ToLowerInvariant();

        text = text
            .Replace('0', 'o')
            .Replace('1', 'i')
            .Replace('3', 'e')
            .Replace('4', 'a')
            .Replace('5', 's')
            .Replace('7', 't')
            .Replace('@', 'a')
            .Replace('$', 's');

        text = RemoveDiacritics(text);

        text = Regex.Replace(text, @"[^a-z0-9]", "");

        return text;
    }

    private string RemoveDiacritics(string text)
    {
        string normalized = text.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder();

        foreach (char c in normalized)
        {
            UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    #endregion

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

    private bool IsDebugNameTaken(string name)
    {
        if (debugTakenNames == null) return false;

        foreach (var taken in debugTakenNames)
        {
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

        string localName = PlayerPrefs.GetString(PREF_DISPLAY_NAME, "");

        if (debugForceNamePanel)
        {
            debugForceNamePanel = false;
            namePanel.SetActive(true);

            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(SubmitNameFromUI);

            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(OnSkipName);

            RefreshLanguageUI(LocalizationSettings.SelectedLocale);


            return;
        }

        if (!string.IsNullOrEmpty(localName))
        {
            UpdateDisplayNameIfNeeded(localName);
        }
        else
        {
            namePanel.SetActive(true);

            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(SubmitNameFromUI);

            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(OnSkipName);

            RefreshLanguageUI(LocalizationSettings.SelectedLocale);
        }
    }

    private void OnPlayFabError(PlayFabError error)
    {
        ShowLoading(false);
        isLoggingIn = false;

        Debug.LogWarning(error.GenerateErrorReport());

        if (retryCoroutine == null)
            retryCoroutine = StartCoroutine(RetryWhenInternetReturns());
    }

    #endregion

    private IEnumerator RetryWhenInternetReturns()
    {
        while (Application.internetReachability == NetworkReachability.NotReachable)
            yield return new WaitForSecondsRealtime(0.5f);

        yield return new WaitForSecondsRealtime(0.25f);

        retryCoroutine = null;
        TryLogin();
    }

    #region DisplayName

    public void SubmitNameFromUI()
    {
        string typed = nameInput.text.Trim();

        if (string.IsNullOrEmpty(typed))
        {
            feedbackText.text = nameEmptyLocalized.GetLocalizedString();
            return;
        }

        if (typed.Length > MAX_NAME_LENGTH)
        {
            feedbackText.text = nameTooLongLocalized.GetLocalizedString();
            return;
        }

        if (IsInappropriateName(typed))
        {
            feedbackText.text = inappropriateNameLocalized.GetLocalizedString();
            StartCoroutine(ShakeInputField());
            return;
        }

        if (debugNameTestMode)
        {
            if (IsDebugNameTaken(typed))
            {
                feedbackText.text = nameTakenLocalized.GetLocalizedString();
                StartCoroutine(ShakeInputField());
            }
            else
            {
                feedbackText.text = "Nombre válido (modo prueba).";
            }

            return;
        }

        SetDisplayName(typed);
    }

    private void OnSkipName()
    {
        string generated = "Player" + UnityEngine.Random.Range(1000, 9999);
        SetDisplayName(generated);
        namePanel.SetActive(false);
    }

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

            feedbackText.text = "";
            namePanel.SetActive(false);

            FinalizeLogin();

        }, error =>
        {
            ShowLoading(false);

            if (error.Error == PlayFabErrorCode.NameNotAvailable)
            {
                feedbackText.text = nameTakenLocalized.GetLocalizedString();
                StartCoroutine(ShakeInputField());
            }
            else
            {
                feedbackText.text = nameErrorLocalized.GetLocalizedString();
            }
        });
    }

    private void UpdateDisplayNameIfNeeded(string localName)
    {
        ShowLoading(true);

        var req = new UpdateUserTitleDisplayNameRequest { DisplayName = localName };

        PlayFabClientAPI.UpdateUserTitleDisplayName(req, res =>
        {
            ShowLoading(false);
            DisplayName = res.DisplayName;
            FinalizeLogin();
        },
        err =>
        {
            ShowLoading(false);
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

    #region Localization

    private void ApplySavedLanguage()
    {
        string saved = PlayerPrefs.GetString(LanguageKey, "");
        if (string.IsNullOrEmpty(saved) || LocalizationSettings.AvailableLocales == null)
            return;

        Locale target = null;
        foreach (var l in LocalizationSettings.AvailableLocales.Locales)
        {
            if (l.Identifier.Code == saved || l.Identifier.Code.StartsWith(saved))
            {
                target = l;
                break;
            }
        }

        if (target != null)
            LocalizationSettings.SelectedLocale = target;
    }

    private void ToggleLanguage()
    {
        if (LocalizationSettings.AvailableLocales == null) return;

        var current = LocalizationSettings.SelectedLocale;
        string code = current != null ? current.Identifier.Code : "es";
        bool isSpanish = code.StartsWith("es");

        Locale target = null;
        foreach (var l in LocalizationSettings.AvailableLocales.Locales)
        {
            if (isSpanish && l.Identifier.Code.StartsWith("en"))
            {
                target = l;
                break;
            }

            if (!isSpanish && l.Identifier.Code.StartsWith("es"))
            {
                target = l;
                break;
            }
        }

        if (target == null) return;

        LocalizationSettings.SelectedLocale = target;

        PlayerPrefs.SetString(LanguageKey, target.Identifier.Code);
        PlayerPrefs.Save();
    }

    private void OnSelectedLocaleChanged(Locale newLocale)
    {
        RefreshLanguageUI(newLocale);
    }

    private void RefreshLanguageUI(Locale locale)
    {
        if (languageButtonText == null || locale == null) return;

        string code = locale.Identifier.Code;

        if (code.StartsWith("es")) languageButtonText.text = "ESP";
        else if (code.StartsWith("en")) languageButtonText.text = "ENG";
        else languageButtonText.text = code.ToUpperInvariant();
    }

    #endregion

    private void OnDestroy()
    {
        if (nameInput != null)
            nameInput.onValueChanged.RemoveListener(OnNameInputChanged);

        if (languageButton != null)
            languageButton.onClick.RemoveListener(ToggleLanguage);

        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
    }
}