using System;
using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[System.Serializable]
public class TopPlayerSlot
{
    public GameObject root;
    public Image trophyImage;         // la copa (no hace falta tocarla por código si ya tiene el sprite)
    public Image avatarImage;         // círculo / avatar
    public TextMeshProUGUI nameText;  // nombre del jugador
    public TextMeshProUGUI scoreText; // puntos
    public GameObject levelIcon;      // Icono de nivel
    public TextMeshProUGUI levelText; // Texto del nivel
    public Sprite defaultAvatarSprite;

    [HideInInspector] public AvatarImageBinder binder;
}

public class LeaderboardUI : MonoBehaviour
{
    public static LeaderboardUI Instance { get; private set; }

    [Header("UI References")]
    public Transform contentParent;
    public GameObject playerRowPrefab;
    public TextMeshProUGUI myPositionText;

    [Header("Modo Botones")]
    public Button classicButton;
    public Button colorButton;
    public Button geometricButton;
    public Button gridButton;
    public Button dodgeButton;
    public Button differentButton;

    [Header("Localization")]
    public LocalizedString touchButtonPrompt;      // "¡Toca un botón..."
    public LocalizedString loadingScoresText;      // "Cargando..."
    public LocalizedString noScoresText;           // "No hay puntuaciones..."
    public LocalizedString noScoreThisModeText;    // "Aún no tienes puntuación..."
    public LocalizedString leaderboardHasScore;    // Smart String con rank y score

    private MyPosState myPosState = MyPosState.Prompt;
    private int lastRank = -1;
    private int lastScore = 0;

    private Button currentSelectedButton;
    private bool isLoading = false;
    private string currentRequestedStat = "";

    [Header("Top 3 UI")]
    public TopPlayerSlot[] top3Slots;

    [Header("Top 3 Placeholders")]
    public string noRegisteredName = "No registrado";
    public string noRegisteredScore = "--";

    [Header("Avatares")]
    [SerializeField] private AvatarCatalogSO avatarCatalog;

    [Header("Perfil jugador")]
    [SerializeField] private XPUIAnimation profilePanel;

    [Header("No Connection UI")]
    [SerializeField] private GameObject noConnectionRoot; // panel con Image + Text
    [SerializeField] private Image noConnectionImage;
    [SerializeField] private TextMeshProUGUI noConnectionText;
    [SerializeField] private Sprite noConnectionSprite;
    [SerializeField] private LocalizedString noConnectionLocalizedText; // "Sin conexión" / "No connection"

    [Header("Player Level Leaderboard")]
    [SerializeField] private string playerLevelStatisticName = "PlayerLevel"; // leaderboard de nivel

    // Cache niveles (por carga)
    private Dictionary<string, int> levelsByPlayFabId = new Dictionary<string, int>();

    public enum MyPosState
    {
        Prompt,             // "Toca un botón..."
        Loading,            // "Cargando..."
        NoScores,           // "No hay puntuaciones..."
        HasScore,           // "Tu posición actual: ..."
        NoScoreThisMode     // "Aún no tienes puntuación..."
    }

    private int requestToken = 0;
    private Coroutine reconnectRoutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        classicButton.onClick.AddListener(() => OnModeButtonClicked("HighScore", classicButton));
        colorButton.onClick.AddListener(() => OnModeButtonClicked("ColorScore", colorButton));
        geometricButton.onClick.AddListener(() => OnModeButtonClicked("GeometricScore", geometricButton));
        gridButton.onClick.AddListener(() => OnModeButtonClicked("GridScore", gridButton));
        dodgeButton.onClick.AddListener(() => OnModeButtonClicked("DodgeScore", dodgeButton));
        differentButton.onClick.AddListener(() => OnModeButtonClicked("DifferentScore", differentButton));

        // ✅ Cache binders top3
        if (top3Slots != null)
        {
            foreach (var s in top3Slots)
            {
                if (s == null || s.avatarImage == null) continue;
                s.binder = s.avatarImage.GetComponent<AvatarImageBinder>();
                if (s.binder == null) s.binder = s.avatarImage.gameObject.AddComponent<AvatarImageBinder>();
            }
        }
    }

    private void Start()
    {
        if (myPositionText != null)
        {
            myPosState = MyPosState.Prompt;
            myPositionText.text = touchButtonPrompt.GetLocalizedString();
        }

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        ShowNoConnectionUI(false);

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            ShowNoConnectionUI(true);
        }
        else
        {
            ShowNoConnectionUI(false);
        }

        // 🔹 Esperar a que PlayFab esté logueado antes de intentar cargar ranking
        StartCoroutine(WaitForPlayFabAndInit());

        wasOffline = Application.internetReachability == NetworkReachability.NotReachable;
    }

    private bool wasOffline = false;

    private void Update()
    {
        bool offline = Application.internetReachability == NetworkReachability.NotReachable;

        // Si acabamos de perder conexión
        if (offline && !wasOffline)
        {
            requestToken++;

            // ✅ cancelamos cualquier carga bloqueada
            isLoading = false;

            ShowNoConnectionUI(true);

            // opcional: si quieres que el texto no se quede en "Cargando..."
            if (myPositionText != null)
            {
                myPosState = MyPosState.NoScores;
                myPositionText.text = noScoresText.GetLocalizedString();
            }
        }

        // Si acabamos de recuperar conexión
        if (!offline && wasOffline)
        {
            ShowNoConnectionUI(false);

            if (reconnectRoutine != null) StopCoroutine(reconnectRoutine);
            reconnectRoutine = StartCoroutine(EnsureLoginThenRefresh());
        }

        wasOffline = offline;
    }

    private IEnumerator EnsureLoginThenRefresh()
    {
        // Esperar a que exista el login manager
        while (PlayFabLoginManager.Instance == null)
            yield return null;

        // Si todavía no hay modo seleccionado (caso: entré offline y nunca se inicializó)
        if (string.IsNullOrEmpty(currentRequestedStat))
        {
            PlayFabLoginManager.Instance.TryLogin();

            while (!PlayFabLoginManager.Instance.IsLoggedIn)
            {
                PlayFabLoginManager.Instance.TryLogin();
                yield return new WaitForSecondsRealtime(0.25f);
            }

            yield return StartCoroutine(InitLastMode());
            reconnectRoutine = null;
            yield break;
        }

        // Caso normal
        PlayFabLoginManager.Instance.TryLogin();

        while (!PlayFabLoginManager.Instance.IsLoggedIn)
        {
            PlayFabLoginManager.Instance.TryLogin();
            yield return new WaitForSecondsRealtime(0.25f);
        }

        RefreshCurrentLeaderboard();
        reconnectRoutine = null;
    }

    private IEnumerator WaitForPlayFabAndInit()
    {
        // 1) Esperar a tener internet (si entraste offline)
        while (Application.internetReachability == NetworkReachability.NotReachable)
        {
            ShowNoConnectionUI(true);
            yield return new WaitForSecondsRealtime(0.25f);
        }

        ShowNoConnectionUI(false);

        // 2) Esperar PlayFabLoginManager
        while (PlayFabLoginManager.Instance == null)
            yield return null;

        // ✅ 3) FORZAR intento de login (CRÍTICO)
        PlayFabLoginManager.Instance.TryLogin();

        // 4) Esperar login (si falla por red, TryLogin se podrá re-lanzar)
        while (!PlayFabLoginManager.Instance.IsLoggedIn)
        {
            // ✅ por seguridad (si hubo un fallo antes o entró offline)
            PlayFabLoginManager.Instance.TryLogin();
            yield return new WaitForSecondsRealtime(0.25f);
        }

        // 5) Ya se puede iniciar el último modo
        yield return null;
        StartCoroutine(InitLastMode());
    }

    private IEnumerator InitLastMode()
    {
        yield return null; // Espera 1 frame

        // Recuperar último modo o usar HighScore por defecto
        string lastMode = PlayerPrefs.GetString("LastLeaderboardMode", "HighScore");

        switch (lastMode)
        {
            case "HighScore":
                OnModeButtonClicked("HighScore", classicButton);
                break;
            case "ColorScore":
                OnModeButtonClicked("ColorScore", colorButton);
                break;
            case "GeometricScore":
                OnModeButtonClicked("GeometricScore", geometricButton);
                break;
            case "GridScore":
                OnModeButtonClicked("GridScore", gridButton);
                break;
            case "DodgeScore":
                OnModeButtonClicked("DodgeScore", dodgeButton);
                break;
            case "DifferentScore":
                OnModeButtonClicked("DifferentScore", differentButton);
                break;
            default:
                OnModeButtonClicked("HighScore", classicButton);
                break;
        }
    }

    private void OnModeButtonClicked(string statisticName, Button clickedButton)
    {
        if (isLoading) return;

        if (clickedButton != null && !clickedButton.interactable)
            return;

        PlayerPrefs.SetString("LastLeaderboardMode", statisticName);
        PlayerPrefs.Save();

        // ✅ Botón activo = disabled
        SetButtonActiveAsDisabled(clickedButton);
        currentSelectedButton = clickedButton;

        PlayButtonPop(clickedButton);
        ShowLeaderboard(statisticName, 10);
    }

    public void ShowLeaderboard(string statisticName, int top = 10)
    {
        int token = ++requestToken;

        if (contentParent == null || playerRowPrefab == null)
        {
            Debug.LogWarning("LeaderboardUI: Falta asignar referencias.");
            return;
        }

        isLoading = true;
        ShowNoConnectionUI(false);
        currentRequestedStat = statisticName;

        // limpia cache de niveles por carga
        levelsByPlayFabId.Clear();

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            isLoading = false;

            myPosState = MyPosState.NoScores;
            if (myPositionText != null)
                myPositionText.text = noScoresText.GetLocalizedString();

            ShowNoConnectionUI(true);
            return;
        }

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        myPosState = MyPosState.Loading;
        myPositionText.text = loadingScoresText.GetLocalizedString();

        if (PlayFabScoreManager.Instance == null)
        {
            Debug.LogWarning("[LeaderboardUI] PlayFabScoreManager.Instance es NULL. No se puede cargar el ranking.");

            isLoading = false;

            myPosState = MyPosState.NoScores;
            if (myPositionText != null)
                myPositionText.text = noScoresText.GetLocalizedString();

            ShowNoConnectionUI(true);
            return;
        }

        // 1) Pedimos leaderboard principal (puntos)
        bool gotMain = false;
        bool gotLevels = false;
        List<PlayerLeaderboardEntry> mainLeaderboard = null;

        void TryFinalize()
        {
            if (token != requestToken) return;
            if (!gotMain || !gotLevels) return;
            if (statisticName != currentRequestedStat) return;

            // Limpia lista y top3
            foreach (Transform child in contentParent)
                Destroy(child.gameObject);
            ClearTop3Slots();

            if (mainLeaderboard == null || mainLeaderboard.Count == 0)
            {
                myPosState = MyPosState.NoScores;
                myPositionText.text = noScoresText.GetLocalizedString();

                FillTop3(null);
                foreach (Transform child in contentParent)
                    Destroy(child.gameObject);
            }
            else
            {
                FillTop3(mainLeaderboard);
                FillListFromFourth(mainLeaderboard, top);
            }

            Canvas.ForceUpdateCanvases();
            if (contentParent is RectTransform rt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

            isLoading = false;
        }

        // Leaderboard principal (puntos)
        PlayFabScoreManager.Instance.GetLeaderboard(statisticName, top, leaderboard =>
        {
            if (token != requestToken) return;

            if (leaderboard == null)
            {
                ShowNoConnectionUI(true);

                isLoading = false;
                myPosState = MyPosState.NoScores;
                if (myPositionText != null)
                    myPositionText.text = noScoresText.GetLocalizedString();

                return;
            }

            mainLeaderboard = leaderboard;
            gotMain = true;
            TryFinalize();
        });

        // 2) Pedimos leaderboard de niveles (Top N) y montamos diccionario PlayFabId->Level
        LoadLevelsLeaderboard(top, token, () =>
        {
            gotLevels = true;
            TryFinalize();
        });

        // Mostrar posición del jugador (del leaderboard principal)
        if (PlayFabScoreManager.Instance != null)
        {
            PlayFabScoreManager.Instance.GetPlayerRank(statisticName, myEntry =>
            {
                if (token != requestToken) return;
                if (statisticName != currentRequestedStat) return;

                if (myEntry != null)
                {
                    myPosState = MyPosState.HasScore;
                    lastRank = myEntry.Position + 1;
                    lastScore = myEntry.StatValue;

                    myPositionText.text = leaderboardHasScore.GetLocalizedString(lastRank, lastScore);
                }
                else
                {
                    myPosState = MyPosState.NoScoreThisMode;
                    myPositionText.text = noScoreThisModeText.GetLocalizedString();
                }
            });
        }
    }

    private void LoadLevelsLeaderboard(int top, int token, Action onDone)
    {
        // Si no hay nombre de stat, termina
        if (string.IsNullOrEmpty(playerLevelStatisticName))
        {
            onDone?.Invoke();
            return;
        }

        var req = new GetLeaderboardRequest
        {
            StatisticName = playerLevelStatisticName,
            StartPosition = 0,
            MaxResultsCount = top
        };

        PlayFabClientAPI.GetLeaderboard(req,
            res =>
            {
                if (token != requestToken) return;

                levelsByPlayFabId.Clear();

                if (res != null && res.Leaderboard != null)
                {
                    foreach (var e in res.Leaderboard)
                    {
                        if (string.IsNullOrEmpty(e.PlayFabId)) continue;
                        levelsByPlayFabId[e.PlayFabId] = e.StatValue; // StatValue = nivel
                    }
                }

                onDone?.Invoke();
            },
            err =>
            {
                if (token != requestToken) return;
                // Si falla, seguimos (nivel fallback a 1)
                levelsByPlayFabId.Clear();
                onDone?.Invoke();
            }
        );
    }

    private int GetCachedLevel(string playFabId)
    {
        if (string.IsNullOrEmpty(playFabId)) return 1;
        return levelsByPlayFabId.TryGetValue(playFabId, out int lvl) ? Mathf.Max(1, lvl) : 1;
    }

    private void ClearTop3Slots()
    {
        if (top3Slots == null) return;

        foreach (var slot in top3Slots)
        {
            if (slot == null || slot.root == null) continue;
            slot.root.SetActive(false);
        }
    }

    private void FillListFromFourth(List<PlayerLeaderboardEntry> leaderboard, int top)
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        int startIndex = Mathf.Min(3, leaderboard.Count); // 4º puesto
        int count = Mathf.Min(leaderboard.Count, top);

        for (int i = startIndex; i < count; i++)
        {
            var entry = leaderboard[i];
            GameObject row = Instantiate(playerRowPrefab, contentParent);

            var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
            var levelIcon = row.transform.Find("LevelIcon");
            var levelText = levelIcon?.GetComponentInChildren<TextMeshProUGUI>();

            var avatarImgTransform = row.transform.Find("AvatarImage");
            Image avatarImg = avatarImgTransform != null ? avatarImgTransform.GetComponent<Image>() : null;

            if (texts != null && texts.Length >= 3)
            {
                texts[0].text = (entry.Position + 1).ToString();
                texts[1].text = entry.DisplayName ?? "Player";
                texts[2].text = entry.StatValue.ToString();
            }

            if (!string.IsNullOrEmpty(entry.PlayFabId))
            {
                // ✅ Nivel desde leaderboard cache (no UserData)
                if (levelText != null)
                    levelText.text = GetCachedLevel(entry.PlayFabId).ToString();

                if (avatarImg != null)
                {
                    var binder = avatarImg.GetComponent<AvatarImageBinder>();
                    if (binder == null) binder = avatarImg.gameObject.AddComponent<AvatarImageBinder>();

                    SetAvatarForPlayFabId(entry.PlayFabId, avatarImg, null);
                }
            }

            // 🔹 CLICK PARA ABRIR PERFIL REMOTO
            var rowButton = row.GetComponent<Button>();
            if (rowButton != null && profilePanel != null && !string.IsNullOrEmpty(entry.PlayFabId))
            {
                string capturedId = entry.PlayFabId;
                string capturedName = entry.DisplayName ?? "Player";

                rowButton.onClick.AddListener(() =>
                {
                    profilePanel.ShowRemoteProfile(capturedId, capturedName);
                });
            }
        }
    }

    // Busca el AvatarDataSO por id
    private AvatarDataSO GetAvatarDataById(string id)
    {
        if (string.IsNullOrEmpty(id) || avatarCatalog == null) return null;

        foreach (var a in avatarCatalog.avatarDataSO)
        {
            if (a != null && a.id == id)
                return a;
        }
        return null;
    }

    private void SetAvatarForPlayFabId(string playFabId, Image targetImage, Sprite defaultSprite)
    {
        if (targetImage == null) return;
        if (string.IsNullOrEmpty(playFabId)) return;

        var request = new GetUserDataRequest
        {
            PlayFabId = playFabId,
            Keys = new List<string> { "EquippedAvatarIdPublic" }
        };

        PlayFabClientAPI.GetUserData(
            request,
            result =>
            {
                string avatarId = null;

                if (result.Data != null && result.Data.ContainsKey("EquippedAvatarIdPublic"))
                    avatarId = result.Data["EquippedAvatarIdPublic"].Value;

                if (string.IsNullOrEmpty(avatarId))
                {
                    if (defaultSprite != null)
                    {
                        targetImage.sprite = defaultSprite;
                        targetImage.material = null;
                    }
                    return;
                }

                var data = GetAvatarDataById(avatarId);
                if (data != null)
                {
                    ApplyAvatarVisualToImage(targetImage, data);
                }
                else if (defaultSprite != null)
                {
                    targetImage.sprite = defaultSprite;
                    targetImage.material = null;
                }
            },
            error =>
            {
                Debug.LogWarning("Error al obtener EquippedAvatarIdPublic de " + playFabId + ": " + error.GenerateErrorReport());
            }
        );
    }

    private void FillTop3(List<PlayerLeaderboardEntry> leaderboard)
    {
        if (top3Slots == null) return;

        if (leaderboard == null)
            leaderboard = new List<PlayerLeaderboardEntry>();

        for (int i = 0; i < top3Slots.Length; i++)
        {
            var slot = top3Slots[i];
            if (slot == null || slot.root == null)
                continue;

            slot.root.SetActive(true);

            if (slot.avatarImage != null)
            {
                if (slot.binder == null)
                    slot.binder = slot.avatarImage.GetComponent<AvatarImageBinder>() ?? slot.avatarImage.gameObject.AddComponent<AvatarImageBinder>();

                slot.binder.Clear(slot.defaultAvatarSprite);
            }

            // Limpia listeners antiguos del botón del slot
            var slotButton = slot.root.GetComponent<Button>();
            if (slotButton != null)
                slotButton.onClick.RemoveAllListeners();

            if (i < leaderboard.Count)
            {
                var entry = leaderboard[i];

                if (slot.nameText != null)
                    slot.nameText.text = entry.DisplayName ?? "Player";

                if (slot.scoreText != null)
                    slot.scoreText.text = entry.StatValue + " pts";

                // Avatar
                if (slot.avatarImage != null && !string.IsNullOrEmpty(entry.PlayFabId))
                {
                    SetAvatarForPlayFabId(entry.PlayFabId, slot.avatarImage, null);
                }

                // ✅ Nivel desde cache de leaderboard PlayerLevel
                if (slot.levelText != null)
                {
                    slot.levelText.text = GetCachedLevel(entry.PlayFabId).ToString();
                }

                // 🔹 CLICK PARA ABRIR PERFIL REMOTO
                if (slotButton != null && profilePanel != null && !string.IsNullOrEmpty(entry.PlayFabId))
                {
                    string capturedId = entry.PlayFabId;
                    string capturedName = entry.DisplayName ?? "Player";

                    slotButton.onClick.AddListener(() =>
                    {
                        profilePanel.ShowRemoteProfile(capturedId, capturedName);
                    });
                }
            }
            else
            {
                // Placeholder
                if (slot.nameText != null)
                    slot.nameText.text = noRegisteredName;

                if (slot.scoreText != null)
                    slot.scoreText.text = noRegisteredScore;

                if (slot.levelText != null)
                    slot.levelText.text = "";
            }
        }
    }

    [Header("Button Pop")]
    [SerializeField] private float popUpScale = 1.08f;
    [SerializeField] private float popUpDuration = 0.08f;
    [SerializeField] private float popDownDuration = 0.10f;

    private Coroutine popRoutine;

    private void PlayButtonPop(Button btn)
    {
        if (btn == null) return;

        var rt = btn.transform as RectTransform;
        if (rt == null) return;

        if (popRoutine != null)
            StopCoroutine(popRoutine);

        popRoutine = StartCoroutine(PopRoutine(rt));
    }

    private IEnumerator PopRoutine(RectTransform rt)
    {
        Vector3 baseScale = rt.localScale;
        Vector3 upScale = baseScale * popUpScale;

        float t = 0f;
        while (t < popUpDuration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / popUpDuration);
            rt.localScale = Vector3.Lerp(baseScale, upScale, u);
            yield return null;
        }

        t = 0f;
        while (t < popDownDuration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / popDownDuration);
            rt.localScale = Vector3.Lerp(upScale, baseScale, u);
            yield return null;
        }

        rt.localScale = baseScale;
        popRoutine = null;
    }

    private void ShowNoConnectionUI(bool show)
    {
        if (noConnectionRoot != null)
            noConnectionRoot.SetActive(show);

        if (show)
        {
            ClearTop3Slots();

            foreach (Transform child in contentParent)
                Destroy(child.gameObject);

            if (noConnectionImage != null && noConnectionSprite != null)
                noConnectionImage.sprite = noConnectionSprite;

            if (noConnectionText != null)
                noConnectionText.text = noConnectionLocalizedText.IsEmpty
                    ? "Sin conexión"
                    : noConnectionLocalizedText.GetLocalizedString();
        }
    }

    private void SetButtonActiveAsDisabled(Button activeButton)
    {
        if (classicButton != null) classicButton.interactable = true;
        if (colorButton != null) colorButton.interactable = true;
        if (geometricButton != null) geometricButton.interactable = true;
        if (gridButton != null) gridButton.interactable = true;
        if (dodgeButton != null) dodgeButton.interactable = true;
        if (differentButton != null) differentButton.interactable = true;

        if (activeButton != null)
            activeButton.interactable = false;
    }

    public void RefreshCurrentLeaderboard()
    {
        if (string.IsNullOrEmpty(currentRequestedStat))
            return;

        if (isLoading)
            return;

        ShowLeaderboard(currentRequestedStat, 10);
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale newLocale)
    {
        switch (myPosState)
        {
            case MyPosState.Prompt:
                myPositionText.text = touchButtonPrompt.GetLocalizedString();
                break;
            case MyPosState.Loading:
                myPositionText.text = loadingScoresText.GetLocalizedString();
                break;
            case MyPosState.NoScores:
                myPositionText.text = noScoresText.GetLocalizedString();
                break;
            case MyPosState.NoScoreThisMode:
                myPositionText.text = noScoreThisModeText.GetLocalizedString();
                break;
            case MyPosState.HasScore:
                myPositionText.text = leaderboardHasScore.GetLocalizedString(lastRank, lastScore);
                break;
        }
    }

    private void ApplyAvatarVisualToImage(Image targetImage, AvatarDataSO data)
    {
        if (targetImage == null) return;

        var binder = targetImage.GetComponent<AvatarImageBinder>();
        if (binder == null) binder = targetImage.gameObject.AddComponent<AvatarImageBinder>();

        if (data != null && data.sprite != null)
        {
            binder.ApplyAvatar(data);
        }
        else
        {
            binder.Clear(null);
        }
    }
}
