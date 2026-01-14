using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XPUIAnimation : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform panel;   // Panel del perfil
    [SerializeField] private Button openButton;     // Botón de usuario (icono arriba en el HUD)
    [SerializeField] private Button closeButton;    // Botón X dentro del panel

    [Header("Open Button Avatar")]
    [SerializeField] private Image openAvatarImage;

    [Header("Animación")]
    [SerializeField] private float slideDuration = 0.25f;
    [SerializeField]
    private AnimationCurve slideCurve =
        AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Botón Cerrar Pop")]
    [SerializeField] private float closePopScale = 0.85f;
    [SerializeField] private float closePopSpeed = 18f;

    // Posiciones
    private Vector2 shownPosition;                                  // posición abierta
    [SerializeField] private Vector2 hiddenPosition = new(0, -1500); // fuera de pantalla abajo

    private bool isShown = false;
    private bool isMoving = false;
    private Coroutine currentRoutine;

    // Escala original del panel (tal cual en el editor: X=0.88749, etc.)
    private Vector3 originalScale;

    // --------- AVATAR ---------
    [Header("Avatar del usuario")]
    [SerializeField] private Image avatarImage;          // Imagen circular grande

    [SerializeField] private AvatarCatalogSO avatarCatalog; // Catálogo de avatares (ScriptableObject)
    [SerializeField] private BackgroundCatalogSO backgroundCatalog;
    [SerializeField] private Sprite fallbackAvatar;      // por si falla algo

    // --------- NIVEL / XP ---------
    [Header("Nivel / XP")]
    [SerializeField] private TextMeshProUGUI levelText;  // Número de nivel
    [SerializeField] private TextMeshProUGUI xpText;     // "XP / XPToNext"
    [SerializeField] private Image xpFillImage;          // Barra de relleno

    [Header("Nombre de usuario")]
    [SerializeField] private TextMeshProUGUI usernameText;

    // --------- RÉCORDS POR MODO ---------
    [Header("Records (solo el valor a la derecha)")]
    [SerializeField] private TextMeshProUGUI classicRecordText;   // HighScore
    [SerializeField] private TextMeshProUGUI colorRecordText;     // ColorScore
    [SerializeField] private TextMeshProUGUI geometricRecordText; // GeometricScore
    [SerializeField] private TextMeshProUGUI gridRecordText;      // GridScore
    [SerializeField] private TextMeshProUGUI dodgeRecordText;     // DodgeScore

    [Header("Conteo de Avatares y Fondos")]
    [SerializeField] private TextMeshProUGUI avatarCountText;
    [SerializeField] private TextMeshProUGUI backgroundCountText;

    private bool isRemoteProfile = false;
    private string remotePlayFabId = null;

    [SerializeField] private Button pencilButton;

    [SerializeField] private Button backgroundButton;
    [SerializeField] private Image backgroundImage;

    // Control de carga remota
    private int pendingRemoteLoads = 0;
    private bool remoteOpenStarted = false;

    [Header("Open Button Pop (hide/show)")]
    [SerializeField] private float openBtnHideDuration = 0.10f;
    [SerializeField] private float openBtnShowDuration = 0.12f;
    [SerializeField] private float openBtnHideScale = 0.85f; // pop inverso
    [SerializeField] private float openBtnShowOvershoot = 1.08f; // pop suave

    private Vector3 openBtnOriginalScale = Vector3.one;
    private Coroutine openBtnRoutine;

    private void Awake()
    {
        if (panel == null)
            panel = GetComponent<RectTransform>();

        if (openButton != null)
            openBtnOriginalScale = openButton.transform.localScale;

        // Guardamos la escala original EXACTA del editor (X=0.88749, etc.)
        originalScale = panel.localScale;

        // Guardamos la posición “buena” tal y como está en el editor
        shownPosition = panel.anchoredPosition;

        // Empezamos ocultos abajo y más pequeños en Y (X se mantiene)
        panel.anchoredPosition = hiddenPosition;
        panel.localScale = new Vector3(
            originalScale.x,
            originalScale.y * 0.7f,
            originalScale.z
        );

        if (openButton != null)
            openButton.onClick.AddListener(OpenPanel);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    private void OnEnable()
    {
        if (PlayFabLoginManager.Instance != null)
            PlayFabLoginManager.Instance.OnLoginSuccess += OnLoginReady;
    }

    private void OnDisable()
    {
        if (PlayFabLoginManager.Instance != null)
            PlayFabLoginManager.Instance.OnLoginSuccess -= OnLoginReady;
    }

    private void OnLoginReady()
    {
        LoadUsername();

        // Sincronizar con PlayFab al loguear
        if (backgroundCatalog != null)
            SyncAllPurchasedBackgroundsToPlayFab(backgroundCatalog);

        if (avatarCatalog != null)
            SyncAllPurchasedAvatarsToPlayFab(avatarCatalog);
    }

    private void Start()
    {
        LoadCurrentAvatarSprite();
        UpdateLevelUI();
        UpdateOpenButtonAvatar();
        LoadUsername();
        UpdateRecordsUI();
        UpdateAvatarAndBackgroundCount();
    }

    private void UpdateAvatarAndBackgroundCount()
    {
        int ownedAvatars = 0;
        int totalAvatars = avatarCatalog != null ? avatarCatalog.avatarDataSO.Count : 0;

        if (avatarCatalog != null)
        {
            foreach (var avatar in avatarCatalog.avatarDataSO)
            {
                if (avatar == null) continue;

                string key = "AvatarPurchased_" + avatar.id;
                bool isOwned = PlayerPrefs.GetInt(key, 0) == 1;

                if (isOwned)
                    ownedAvatars++;
            }
        }

        int ownedBackgrounds = 0;
        int totalBackgrounds = backgroundCatalog != null ? backgroundCatalog.backgroundDataSO.Count : 0;

        if (backgroundCatalog != null)
        {
            foreach (var bg in backgroundCatalog.backgroundDataSO)
            {
                if (bg == null) continue;

                string id = bg.id;
                bool comprado = PlayerPrefs.GetInt("Purchased_" + id, 0) == 1 || id == "DefaultBackground";

                if (comprado)
                    ownedBackgrounds++;
            }
        }

        if (avatarCountText != null)
            avatarCountText.text = $"{ownedAvatars} / {totalAvatars}";

        if (backgroundCountText != null)
            backgroundCountText.text = $"{ownedBackgrounds} / {totalBackgrounds}";
    }

    public void UpdateOpenButtonAvatar()
    {
        if (openAvatarImage == null || avatarCatalog == null)
            return;

        string avatarId = PlayerPrefs.GetString("EquippedAvatarId", "NormalAvatar");
        AvatarDataSO data = GetAvatarById(avatarId);

        if (data != null && data.sprite != null)
        {
            openAvatarImage.sprite = data.sprite;

            // Si quieres que el botón NO use el shader (recomendado en HUD):
            openAvatarImage.material = null;

            openAvatarImage.enabled = true;
        }
        else
        {
            openAvatarImage.sprite = fallbackAvatar;
            openAvatarImage.material = null;
            openAvatarImage.enabled = (fallbackAvatar != null);
        }
    }

    private void Update()
    {
        if (!isRemoteProfile)
        {
            UpdateLevelUI();
            UpdateRecordsUI();
        }
    }

    // --------- ABRIR / CERRAR ---------

    public void OpenPanel()
    {
        ShowLocalProfile();
        if (pencilButton != null)
            pencilButton.gameObject.SetActive(true);
        if (backgroundButton != null)
            backgroundButton.gameObject.SetActive(true);
        if (backgroundImage != null)
            backgroundImage.gameObject.SetActive(true);
        if (openButton != null)
            HideOpenButtonPop();

    }

    public void ShowLocalProfile()
    {
        if (isShown || isMoving) return;

        isShown = true;
        isRemoteProfile = false;
        remotePlayFabId = null;

        LoadCurrentAvatarSprite();
        UpdateLevelUI();
        LoadUsername();
        UpdateRecordsUI();
        UpdateAvatarAndBackgroundCount();

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(SlidePanel(panel.anchoredPosition, shownPosition, true));
    }

    public void ShowRemoteProfile(string playFabId, string displayName)
    {
        if (isMoving) return;

        isRemoteProfile = true;
        remotePlayFabId = playFabId;

        // Aún NO consideramos el panel "mostrado"
        isShown = false;
        remoteOpenStarted = false;
        pendingRemoteLoads = 0;

        // Lo forzamos a la posición oculta por si estaba abierto
        panel.anchoredPosition = hiddenPosition;
        panel.localScale = new Vector3(
            originalScale.x,
            originalScale.y * 0.7f,
            originalScale.z
        );

        // Nombre
        if (usernameText != null)
            usernameText.text = string.IsNullOrEmpty(displayName) ? "Player" : displayName;

        // Cuando miramos otro jugador, sus contadores se cargan remotos más tarde
        if (avatarCountText != null) avatarCountText.text = "- / -";
        if (backgroundCountText != null) backgroundCountText.text = "- / -";

        // Lanzamos las cargas remotas
        LoadRemoteAvatarSprite(playFabId);
        LoadRemoteLevelAndXP(playFabId);
        LoadRemoteAvatarAndBackgroundCount(playFabId);
        LoadRemoteRecords(playFabId);
        LoadRemoteBackground(playFabId);

        if (pencilButton != null)
            pencilButton.gameObject.SetActive(false);
        if (backgroundButton != null)
            backgroundButton.gameObject.SetActive(false);
    }

    private void BeginRemoteLoad()
    {
        pendingRemoteLoads++;
        remoteOpenStarted = false;
    }

    private void OnRemoteLoadFinished(string playFabId)
    {
        if (!isRemoteProfile || playFabId != remotePlayFabId)
            return;

        pendingRemoteLoads = Mathf.Max(0, pendingRemoteLoads - 1);

        if (pendingRemoteLoads == 0 && !remoteOpenStarted)
        {
            remoteOpenStarted = true;

            if (!isShown)
            {
                isShown = true;
                if (currentRoutine != null) StopCoroutine(currentRoutine);
                currentRoutine = StartCoroutine(SlidePanel(panel.anchoredPosition, shownPosition, true));
            }
        }
    }

    private void LoadRemoteAvatarAndBackgroundCount(string playFabId)
    {
        if (avatarCountText == null && backgroundCountText == null) return;

        BeginRemoteLoad();

        var request = new GetUserDataRequest
        {
            PlayFabId = playFabId
        };

        PlayFabClientAPI.GetUserData(
            request,
            result =>
            {
                if (!isRemoteProfile || playFabId != remotePlayFabId)
                {
                    OnRemoteLoadFinished(playFabId);
                    return;
                }

                // --- Avatares ---
                int totalAvatars = avatarCatalog != null ? avatarCatalog.avatarDataSO.Count : 0;
                int ownedAvatars = 0;

                if (avatarCatalog != null && result.Data != null)
                {
                    foreach (var avatar in avatarCatalog.avatarDataSO)
                    {
                        if (avatar == null) continue;

                        string key = "AvatarPurchased_" + avatar.id;

                        if (result.Data.ContainsKey(key) &&
                            result.Data[key].Value == "1")
                        {
                            ownedAvatars++;
                        }
                    }
                }

                if (avatarCountText != null)
                    avatarCountText.text = $"{ownedAvatars} / {totalAvatars}";

                // --- Fondos ---
                int totalBackgrounds = backgroundCatalog != null ? backgroundCatalog.backgroundDataSO.Count : 0;
                int ownedBackgrounds = 0;

                if (backgroundCatalog != null && result.Data != null)
                {
                    foreach (var bg in backgroundCatalog.backgroundDataSO)
                    {
                        if (bg == null) continue;

                        string id = bg.id;
                        string key = "Purchased_" + id;

                        bool comprado = false;

                        if (id == "DefaultBackground")
                        {
                            comprado = true;
                        }
                        else if (result.Data.ContainsKey(key) &&
                                 result.Data[key].Value == "1")
                        {
                            comprado = true;
                        }

                        if (comprado)
                            ownedBackgrounds++;
                    }
                }

                if (backgroundCountText != null)
                    backgroundCountText.text = $"{ownedBackgrounds} / {totalBackgrounds}";

                OnRemoteLoadFinished(playFabId);
            },
            error =>
            {
                Debug.LogWarning("Error al cargar conteo remoto de avatares/fondos: " + error.GenerateErrorReport());

                if (avatarCountText != null && avatarCatalog != null)
                    avatarCountText.text = $"? / {avatarCatalog.avatarDataSO.Count}";

                if (backgroundCountText != null && backgroundCatalog != null)
                    backgroundCountText.text = $"? / {backgroundCatalog.backgroundDataSO.Count}";

                OnRemoteLoadFinished(playFabId);
            }
        );
    }

    private void LoadRemoteAvatarSprite(string playFabId)
    {
        if (avatarImage == null || avatarCatalog == null) return;

        BeginRemoteLoad();

        var request = new GetUserDataRequest
        {
            PlayFabId = playFabId,
            Keys = new List<string> { "EquippedAvatarIdPublic" }
        };

        PlayFabClientAPI.GetUserData(
            request,
            result =>
            {
                if (!isRemoteProfile || playFabId != remotePlayFabId)
                {
                    OnRemoteLoadFinished(playFabId);
                    return;
                }

                string avatarId = null;
                if (result.Data != null && result.Data.ContainsKey("EquippedAvatarIdPublic"))
                    avatarId = result.Data["EquippedAvatarIdPublic"].Value;

                if (string.IsNullOrEmpty(avatarId))
                {
                    if (fallbackAvatar != null)
                        avatarImage.sprite = fallbackAvatar;

                    OnRemoteLoadFinished(playFabId);
                    return;
                }

                var data = GetAvatarById(avatarId);
                if (data != null && data.sprite != null)
                    avatarImage.sprite = data.sprite;
                else if (fallbackAvatar != null)
                    avatarImage.sprite = fallbackAvatar;

                OnRemoteLoadFinished(playFabId);
            },
            error =>
            {
                Debug.LogWarning("Error al cargar avatar remoto: " + error.GenerateErrorReport());
                if (fallbackAvatar != null)
                    avatarImage.sprite = fallbackAvatar;

                OnRemoteLoadFinished(playFabId);
            }
        );
    }

    private void LoadRemoteLevelAndXP(string playFabId)
    {
        if (levelText == null && xpText == null && xpFillImage == null) return;

        BeginRemoteLoad();

        var request = new GetUserDataRequest
        {
            PlayFabId = playFabId,
            Keys = new List<string> { "PlayerLevel", "PlayerXP", "PlayerXPNext" }
        };

        PlayFabClientAPI.GetUserData(
            request,
            result =>
            {
                if (!isRemoteProfile || playFabId != remotePlayFabId)
                {
                    OnRemoteLoadFinished(playFabId);
                    return;
                }

                int level = 1;
                int xp = 0;
                int xpNext = 1;

                if (result.Data != null)
                {
                    if (result.Data.ContainsKey("PlayerLevel"))
                        int.TryParse(result.Data["PlayerLevel"].Value, out level);

                    if (result.Data.ContainsKey("PlayerXP"))
                        int.TryParse(result.Data["PlayerXP"].Value, out xp);

                    if (result.Data.ContainsKey("PlayerXPNext"))
                        int.TryParse(result.Data["PlayerXPNext"].Value, out xpNext);
                }

                xpNext = Mathf.Max(1, xpNext);

                if (levelText != null)
                    levelText.text = level.ToString();

                if (xpText != null)
                    xpText.text = $"{xp} / {xpNext}";

                if (xpFillImage != null)
                    xpFillImage.fillAmount = (float)xp / xpNext;

                OnRemoteLoadFinished(playFabId);
            },
            error =>
            {
                Debug.LogWarning("Error al cargar nivel remoto: " + error.GenerateErrorReport());
                OnRemoteLoadFinished(playFabId);
            }
        );
    }

    private void LoadRemoteRecords(string playFabId)
    {
        BeginRemoteLoad();

        var request = new GetLeaderboardAroundPlayerRequest
        {
            PlayFabId = playFabId,
            StatisticName = "HighScore",
            MaxResultsCount = 1
        };

        PlayFabClientAPI.GetLeaderboardAroundPlayer(
            request,
            result =>
            {
                int high = 0;

                if (result.Leaderboard != null && result.Leaderboard.Count > 0)
                    high = result.Leaderboard[0].StatValue;

                classicRecordText.text = high.ToString();

                LoadRemoteRecordForStat(playFabId, "ColorScore", colorRecordText);
                LoadRemoteRecordForStat(playFabId, "GeometricScore", geometricRecordText);
                LoadRemoteRecordForStat(playFabId, "GridScore", gridRecordText);
                LoadRemoteRecordForStat(playFabId, "DodgeScore", dodgeRecordText);

                OnRemoteLoadFinished(playFabId);
            },
            error =>
            {
                Debug.LogWarning(error.GenerateErrorReport());
                OnRemoteLoadFinished(playFabId);
            }
        );
    }

    private void LoadRemoteRecordForStat(string playFabId, string statName, TextMeshProUGUI targetText)
    {
        var request = new GetLeaderboardAroundPlayerRequest
        {
            PlayFabId = playFabId,
            StatisticName = statName,
            MaxResultsCount = 1
        };

        PlayFabClientAPI.GetLeaderboardAroundPlayer(
            request,
            result =>
            {
                int value = 0;

                if (result.Leaderboard != null && result.Leaderboard.Count > 0)
                    value = result.Leaderboard[0].StatValue;

                targetText.text = value.ToString();
            },
            error =>
            {
                Debug.LogWarning($"Error al obtener {statName}: {error.GenerateErrorReport()}");
                targetText.text = "0";
            }
        );
    }

    private void LoadRemoteBackground(string playFabId)
    {
        if (backgroundImage == null || backgroundCatalog == null) return;

        BeginRemoteLoad();

        var request = new GetUserDataRequest
        {
            PlayFabId = playFabId,
            Keys = new List<string> { "SelectedBackground" }
        };

        PlayFabClientAPI.GetUserData(
            request,
            result =>
            {
                if (!isRemoteProfile || playFabId != remotePlayFabId)
                {
                    OnRemoteLoadFinished(playFabId);
                    return;
                }

                string bgId = "DefaultBackground";
                if (result.Data != null && result.Data.ContainsKey("SelectedBackground"))
                    bgId = result.Data["SelectedBackground"].Value;

                BackgroundDataSO bgData = null;
                if (backgroundCatalog != null && backgroundCatalog.backgroundDataSO != null)
                {
                    foreach (var bg in backgroundCatalog.backgroundDataSO)
                    {
                        if (bg != null && bg.id == bgId)
                        {
                            bgData = bg;
                            break;
                        }
                    }
                }

                if (bgData != null && bgData.sprite != null)
                {
                    backgroundImage.sprite = bgData.sprite;
                    backgroundImage.gameObject.SetActive(true);
                }
                else
                {
                    backgroundImage.gameObject.SetActive(false);
                }

                OnRemoteLoadFinished(playFabId);
            },
            error =>
            {
                Debug.LogWarning("Error al cargar fondo remoto: " + error.GenerateErrorReport());
                backgroundImage.gameObject.SetActive(false);
                OnRemoteLoadFinished(playFabId);
            }
        );
    }

    public void ClosePanel()
    {
        if (!isShown || isMoving) return;

        isShown = false;

        // Pop del botón de cerrar
        if (closeButton != null)
            StartCoroutine(CloseButtonPopAnimation());

        if (openButton != null)
            ShowOpenButtonPop();

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(SlidePanel(panel.anchoredPosition, hiddenPosition, false));
    }

    public void SyncAllPurchasedBackgroundsToPlayFab(BackgroundCatalogSO catalog)
    {
        if (catalog == null)
        {
            Debug.LogWarning("[SyncBackgrounds] Catálogo nulo, no sincronizo.");
            return;
        }

        if (PlayFabLoginManager.Instance == null || !PlayFabLoginManager.Instance.IsLoggedIn)
        {
            Debug.LogWarning("[SyncBackgrounds] No logueado en PlayFab, no sincronizo todavía.");
            return;
        }

        Dictionary<string, string> data = new();
        int countPurchased = 0;

        foreach (var bg in catalog.backgroundDataSO)
        {
            if (bg == null) continue;

            string id = bg.id;
            bool purchased = PlayerPrefs.GetInt("Purchased_" + id, 0) == 1;

            if (purchased)
            {
                data["Purchased_" + id] = "1";
                countPurchased++;
            }
        }

        string equipped = PlayerPrefs.GetString("SelectedBackground", "DefaultBackground");
        data["SelectedBackground"] = equipped;

        Debug.Log($"[SyncBackgrounds] Enviando a PlayFab {countPurchased} fondos comprados. Equipped = {equipped}");

        var request = new PlayFab.ClientModels.UpdateUserDataRequest
        {
            Data = data,
            Permission = PlayFab.ClientModels.UserDataPermission.Public
        };

        PlayFab.PlayFabClientAPI.UpdateUserData(
            request,
            res => Debug.Log("[SyncBackgrounds] Sincronización completa de fondos con PlayFab."),
            err => Debug.LogWarning("[SyncBackgrounds] Error sincronizando fondos: " + err.GenerateErrorReport())
        );
    }

    public void SyncAllPurchasedAvatarsToPlayFab(AvatarCatalogSO catalog)
    {
        if (catalog == null)
        {
            Debug.LogWarning("[SyncAvatars] Catálogo nulo, no sincronizo.");
            return;
        }

        if (PlayFabLoginManager.Instance == null || !PlayFabLoginManager.Instance.IsLoggedIn)
        {
            Debug.LogWarning("[SyncAvatars] No logueado en PlayFab, no sincronizo todavía.");
            return;
        }

        Dictionary<string, string> data = new();
        int countPurchased = 0;

        foreach (var av in catalog.avatarDataSO)
        {
            if (av == null) continue;

            string key = "AvatarPurchased_" + av.id;
            bool purchased = PlayerPrefs.GetInt(key, 0) == 1;

            if (purchased)
            {
                data[key] = "1";
                countPurchased++;
            }
        }

        Debug.Log($"[SyncAvatars] Enviando a PlayFab {countPurchased} avatares comprados.");

        var request = new UpdateUserDataRequest
        {
            Data = data,
            Permission = UserDataPermission.Public
        };

        PlayFabClientAPI.UpdateUserData(
            request,
            res => Debug.Log("[SyncAvatars] Sincronización completa de avatares con PlayFab."),
            err => Debug.LogWarning("[SyncAvatars] Error sincronizando avatares: " + err.GenerateErrorReport())
        );
    }

    private IEnumerator CloseButtonPopAnimation()
    {
        Transform btn = closeButton.transform;
        Vector3 original = btn.localScale;
        Vector3 target = original * closePopScale;

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * closePopSpeed;
            btn.localScale = Vector3.Lerp(original, target, t);
            yield return null;
        }

        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * closePopSpeed;
            btn.localScale = Vector3.Lerp(target, original, t);
            yield return null;
        }

        btn.localScale = original;
    }

    private void HideOpenButtonPop()
    {
        if (openBtnRoutine != null) StopCoroutine(openBtnRoutine);
        openBtnRoutine = StartCoroutine(HideOpenButtonRoutine());
    }

    private void ShowOpenButtonPop()
    {
        if (openBtnRoutine != null) StopCoroutine(openBtnRoutine);
        openBtnRoutine = StartCoroutine(ShowOpenButtonRoutine());
    }

    private IEnumerator HideOpenButtonRoutine()
    {
        Transform t = openButton.transform;

        // asegurar escala original
        t.localScale = openBtnOriginalScale;

        float time = 0f;
        Vector3 from = openBtnOriginalScale;
        Vector3 to = openBtnOriginalScale * openBtnHideScale;

        while (time < openBtnHideDuration)
        {
            time += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(time / openBtnHideDuration);
            // ease in suave
            float eased = u * u;
            t.localScale = Vector3.Lerp(from, to, eased);
            yield return null;
        }

        t.localScale = to;
        openButton.gameObject.SetActive(false);

        // reset para cuando vuelva
        t.localScale = openBtnOriginalScale;
        openBtnRoutine = null;
    }

    private IEnumerator ShowOpenButtonRoutine()
    {
        Transform t = openButton.transform;

        openButton.gameObject.SetActive(true);

        Vector3 baseScale = openBtnOriginalScale;
        Vector3 start = baseScale * openBtnHideScale;
        Vector3 overshoot = baseScale * openBtnShowOvershoot;

        // arrancar pequeño
        t.localScale = start;

        // 1) subir hasta overshoot
        float time = 0f;
        float half = openBtnShowDuration * 0.6f;
        while (time < half)
        {
            time += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(time / half);
            float eased = EaseOutCubic(u);
            t.localScale = Vector3.LerpUnclamped(start, overshoot, eased);
            yield return null;
        }

        // 2) volver a escala normal
        time = 0f;
        float settle = openBtnShowDuration * 0.4f;
        while (time < settle)
        {
            time += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(time / settle);
            float eased = EaseOutCubic(u);
            t.localScale = Vector3.LerpUnclamped(overshoot, baseScale, eased);
            yield return null;
        }

        t.localScale = baseScale;
        openBtnRoutine = null;
    }

    private float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    /// <summary>
    /// Animación: el panel sube desde abajo y cambia de escala SOLO en Y,
    /// manteniendo la X original del editor.
    /// opening = true → entra desde abajo, Y pequeña → Y grande + pop.
    /// opening = false → sale hacia abajo, Y grande → Y pequeña.
    /// </summary>
    private IEnumerator SlidePanel(Vector2 from, Vector2 to, bool opening)
    {
        isMoving = true;

        if (opening)
        {
            // ---------- APERTURA ----------
            float t = 0f;

            float startFactor = 0.8f;   // 80% de la escala original
            float endFactor = 0.9f;   // 90% durante la subida

            Vector3 startScale = originalScale * startFactor;
            Vector3 endScale = originalScale * endFactor;

            panel.anchoredPosition = from;
            panel.localScale = startScale;

            // Subida desde abajo + escala de 0.8 → 0.9
            while (t < slideDuration)
            {
                t += Time.deltaTime;
                float lerp = Mathf.Clamp01(t / slideDuration);
                float eased = slideCurve.Evaluate(lerp);

                panel.anchoredPosition = Vector2.Lerp(from, to, eased);
                panel.localScale = Vector3.Lerp(startScale, endScale, lerp);

                yield return null;
            }

            panel.anchoredPosition = to;
            panel.localScale = endScale;

            // POP final hasta la escala EXACTA del editor (originalScale)
            float popDuration = 0.10f;
            float popT = 0f;
            float popFactor = 1.05f;

            Vector3 overshoot = originalScale * popFactor;
            Vector3 final = originalScale;

            panel.localScale = overshoot;

            while (popT < popDuration)
            {
                popT += Time.deltaTime;
                float lerp = Mathf.Clamp01(popT / popDuration);
                panel.localScale = Vector3.Lerp(overshoot, final, lerp);
                yield return null;
            }

            panel.localScale = final;
        }
        else
        {
            // ---------- CIERRE ----------
            // 1) POP en su sitio (no se mueve, solo escala)
            float popDuration = 0.10f;
            float popT = 0f;
            float popFactor = 1.05f;

            Vector3 baseScale = originalScale;
            Vector3 overshoot = originalScale * popFactor;

            panel.anchoredPosition = from;
            panel.localScale = baseScale;

            while (popT < popDuration)
            {
                popT += Time.deltaTime;
                float lerp = Mathf.Clamp01(popT / popDuration);

                if (lerp < 0.5f)
                {
                    // 0 → 0.5: 1.0 → 1.05
                    float inner = lerp / 0.5f;
                    panel.localScale = Vector3.Lerp(baseScale, overshoot, inner);
                }
                else
                {
                    // 0.5 → 1: 1.05 → 1.0
                    float inner = (lerp - 0.5f) / 0.5f;
                    panel.localScale = Vector3.Lerp(overshoot, baseScale, inner);
                }

                yield return null;
            }

            panel.localScale = baseScale;

            // 2) Desplazamiento hacia abajo + encoger (1.0 → 0.8)
            float t = 0f;
            Vector3 endScale = originalScale * 0.8f;

            while (t < slideDuration)
            {
                t += Time.deltaTime;
                float lerp = Mathf.Clamp01(t / slideDuration);
                float eased = slideCurve.Evaluate(lerp);

                panel.anchoredPosition = Vector2.Lerp(from, to, eased);
                panel.localScale = Vector3.Lerp(baseScale, endScale, lerp);

                yield return null;
            }

            panel.anchoredPosition = to;
            panel.localScale = endScale;
        }

        isMoving = false;
    }

    // --------- AVATAR ---------

    public void LoadCurrentAvatarSprite()
    {
        if (avatarImage == null) return;

        string avatarId = PlayerPrefs.GetString("EquippedAvatarId", "NormalAvatar");
        AvatarDataSO data = GetAvatarById(avatarId);

        if (data != null && data.sprite != null)
        {
            avatarImage.sprite = data.sprite;

            // Aplicar shader especial si lo tiene
            ApplyAvatarMaterial(data);
        }
        else if (fallbackAvatar != null)
        {
            avatarImage.sprite = fallbackAvatar;
            // Material por defecto sin efecto
            avatarImage.material = null;
        }
    }

    private void ApplyAvatarMaterial(AvatarDataSO data)
    {
        // Si no hay efecto, material por defecto
        if (!data.hasShaderEffect || data.effectMaterial == null)
        {
            avatarImage.material = null; // o un material UI por defecto
            return;
        }

        // Asignar el material base del shader
        // IMPORTANTE: instanciar para no modificar el asset original
        Material matInstance = Instantiate(data.effectMaterial);
        avatarImage.material = matInstance;

        // Opcional: setear parámetros por avatar
        /* if (data.effectType == AvatarShaderEffectType.Wave)
        {
            // Los nombres deben coincidir con las propiedades del shader
            matInstance.SetFloat("_WaveAmount", data.waveAmount);
            matInstance.SetFloat("_WaveSpeed", data.waveSpeed);
            matInstance.SetFloat("_WaveStrength", data.waveStrength);
            // Si hace falta, X/Y Axis:
            // matInstance.SetFloat("_WaveX", data.waveXAxis);
            // matInstance.SetFloat("_WaveY", data.waveYAxis);
        }*/
    }

    private AvatarDataSO GetAvatarById(string id)
    {
        if (string.IsNullOrEmpty(id) || avatarCatalog == null) return null;

        foreach (var a in avatarCatalog.avatarDataSO)
        {
            if (a != null && a.id == id)
                return a;
        }
        return null;
    }

    // --------- NIVEL / XP ---------

    private void UpdateLevelUI()
    {
        if (PlayerLevelManager.Instance == null) return;

        int lvl = PlayerLevelManager.Instance.currentLevel;
        int xp = PlayerLevelManager.Instance.currentXP;
        int xpNext = Mathf.Max(1, PlayerLevelManager.Instance.xpToNextLevel);

        if (levelText != null)
            levelText.text = lvl.ToString();

        if (xpText != null)
            xpText.text = $"{xp} / {xpNext}";

        if (xpFillImage != null)
            xpFillImage.fillAmount = (float)xp / xpNext;
    }

    // --------- NOMBRE DE USUARIO ---------

    private void LoadUsername()
    {
        if (usernameText == null) return;

        string finalName = "Player";

        var login = PlayFabLoginManager.Instance;
        if (login != null)
        {
            if (!string.IsNullOrEmpty(login.DisplayName))
            {
                finalName = login.DisplayName;
            }
            else
            {
                string localName = login.GetLocalDisplayName();
                if (!string.IsNullOrEmpty(localName))
                    finalName = localName;
            }
        }

        usernameText.text = finalName;
    }

    // --------- RÉCORDS POR MODO ---------

    private void UpdateRecordsUI()
    {
        if (classicRecordText != null)
            classicRecordText.text = PlayerPrefs.GetInt("MaxRecord", 0).ToString();

        if (colorRecordText != null)
            colorRecordText.text = PlayerPrefs.GetInt("MaxRecordColor", 0).ToString();

        if (geometricRecordText != null)
            geometricRecordText.text = PlayerPrefs.GetInt("MaxRecordGeometric", 0).ToString();

        if (gridRecordText != null)
            gridRecordText.text = PlayerPrefs.GetInt("MaxRecordGrid", 0).ToString();

        if (dodgeRecordText != null)
            dodgeRecordText.text = PlayerPrefs.GetInt("MaxRecordDodge", 0).ToString();
    }
}
