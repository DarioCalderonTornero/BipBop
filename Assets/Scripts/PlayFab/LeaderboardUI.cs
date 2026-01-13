using System;
using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
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
    public GameObject levelIcon;     // Icono de nivel
    public TextMeshProUGUI levelText; // Texto del nivel
    public Sprite defaultAvatarSprite;
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

    public enum MyPosState
    {
        Prompt,             // "Toca un botón..."
        Loading,            // "Cargando..."
        NoScores,           // "No hay puntuaciones..."
        HasScore,           // "Tu posición actual: ..."
        NoScoreThisMode     // "Aún no tienes puntuación..."
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        // Listeners de los botones
        classicButton.onClick.AddListener(() => OnModeButtonClicked("HighScore", classicButton));
        colorButton.onClick.AddListener(() => OnModeButtonClicked("ColorScore", colorButton));
        geometricButton.onClick.AddListener(() => OnModeButtonClicked("GeometricScore", geometricButton));
        gridButton.onClick.AddListener(() => OnModeButtonClicked("GridScore", gridButton));
        dodgeButton.onClick.AddListener(() => OnModeButtonClicked("DodgeScore", dodgeButton));
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
    }

    private bool wasOffline = false;

    private void Update()
    {
        bool offline = Application.internetReachability == NetworkReachability.NotReachable;

        // Si acabamos de perder conexión
        if (offline && !wasOffline)
        {
            ShowNoConnectionUI(true);
        }

        // Si acabamos de recuperar conexión
        if (!offline && wasOffline)
        {
            ShowNoConnectionUI(false);
            RefreshCurrentLeaderboard(); // vuelve a pedir el ranking del modo actual
        }

        wasOffline = offline;
    }

    private IEnumerator WaitForPlayFabAndInit()
    {
        // ✅ Si no hay internet al entrar, mostramos el panel y no iniciamos ranking
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            ShowNoConnectionUI(true);
            yield break;
        }

        // Si hay internet, ocultamos el panel y esperamos login
        ShowNoConnectionUI(false);

        while (PlayFabLoginManager.Instance == null || !PlayFabLoginManager.Instance.IsLoggedIn)
            yield return null;

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
            default:
                OnModeButtonClicked("HighScore", classicButton);
                break;
        }
    }

    private void OnModeButtonClicked(string statisticName, Button clickedButton)
    {
        if (isLoading) return; // Ignorar clicks mientras carga

        PlayerPrefs.SetString("LastLeaderboardMode", statisticName);
        PlayerPrefs.Save();

        // Cambiar color del botón seleccionado
        if (currentSelectedButton != null)
            SetButtonSelected(currentSelectedButton, false);

        SetButtonSelected(clickedButton, true);
        currentSelectedButton = clickedButton;

        ShowLeaderboard(statisticName, 10);
    }

    private void SetButtonSelected(Button button, bool selected)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = selected ? new Color(0.8f, 0.8f, 1f) : Color.white;
        colors.selectedColor = colors.normalColor;
        button.colors = colors;
    }

    public void ShowLeaderboard(string statisticName, int top = 10)
    {
        if (contentParent == null || playerRowPrefab == null)
        {
            Debug.LogWarning("LeaderboardUI: Falta asignar referencias.");
            return;
        }

        isLoading = true;
        ShowNoConnectionUI(false);
        currentRequestedStat = statisticName;

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

            // Texto de abajo (tu posición) opcional: puedes dejarlo como quieras
            myPosState = MyPosState.NoScores;
            if (myPositionText != null)
                myPositionText.text = noScoresText.GetLocalizedString();

            // ✅ SOLO icono + texto, y nada de slots
            ShowNoConnectionUI(true);
            return;
        }

        PlayFabScoreManager.Instance.GetLeaderboard(statisticName, top, leaderboard =>
        {
            if (leaderboard == null)
            {
                // ✅ fallo de carga: icono + texto, sin slots
                ShowNoConnectionUI(true);

                isLoading = false;
                myPosState = MyPosState.NoScores;
                if (myPositionText != null)
                    myPositionText.text = noScoresText.GetLocalizedString();

                return;
            }

            if (statisticName != currentRequestedStat)
                return;

            // Limpia lista y top3
            foreach (Transform child in contentParent)
                Destroy(child.gameObject);
            ClearTop3Slots();

            if (leaderboard == null || leaderboard.Count == 0)
            {
                myPosState = MyPosState.NoScores;
                myPositionText.text = noScoresText.GetLocalizedString();

                // Rellena top3 sólo con placeholders
                FillTop3(null);

                // Limpia la lista de 4–10 (no hay nadie aún)
                foreach (Transform child in contentParent)
                    Destroy(child.gameObject);
            }
            else
            {
                // Top 3 (con reales + placeholders si faltan)
                FillTop3(leaderboard);

                // Del 4 en adelante
                FillListFromFourth(leaderboard, top);
            }

            Canvas.ForceUpdateCanvases();
            if (contentParent is RectTransform rt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

            isLoading = false;
        });

        // Mostrar posición del jugador
        if (PlayFabScoreManager.Instance != null)
        {
            PlayFabScoreManager.Instance.GetPlayerRank(statisticName, myEntry =>
            {
                if (statisticName != currentRequestedStat) return; // Evita resultados antiguos

                if (myEntry != null)
                {
                    myPosState = MyPosState.HasScore;
                    lastRank = myEntry.Position + 1;
                    lastScore = myEntry.StatValue;

                    // Smart String: una sola entrada localizada que recibe {0} (rank) y {1} (score)
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

    private void GetPlayerLevel(string playFabId, Action<int> onLevelFound)
    {
        var request = new GetUserDataRequest
        {
            PlayFabId = playFabId,
            Keys = new List<string> { "PlayerLevel" }
        };

        PlayFabClientAPI.GetUserData(request,
            result =>
            {
                int level = 1;
                if (result.Data != null && result.Data.ContainsKey("PlayerLevel"))
                    int.TryParse(result.Data["PlayerLevel"].Value, out level);
                onLevelFound?.Invoke(level);
            },
            error =>
            {
                onLevelFound?.Invoke(1);
            }
        );
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
                GetPlayerLevel(entry.PlayFabId, level =>
                {
                    if (levelText != null)
                        levelText.text = level.ToString();
                });

                if (avatarImg != null)
                {
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
                    // Si no tiene avatar guardado, podrías poner uno por defecto
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

            // Reset avatar
            if (slot.avatarImage != null && slot.defaultAvatarSprite != null)
            {
                slot.avatarImage.sprite = slot.defaultAvatarSprite;
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

                // Nivel
                if (slot.levelText != null)
                {
                    GetPlayerLevel(entry.PlayFabId, level =>
                    {
                        slot.levelText.text = level.ToString();
                    });
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
    
    private void ShowNoConnectionUI(bool show)
    {
        if (noConnectionRoot != null)
            noConnectionRoot.SetActive(show);

        if (show)
        {
            // Oculta TOP 3
            ClearTop3Slots();

            // Limpia 4-10
            foreach (Transform child in contentParent)
                Destroy(child.gameObject);

            // Set icon + text
            if (noConnectionImage != null && noConnectionSprite != null)
                noConnectionImage.sprite = noConnectionSprite;

            if (noConnectionText != null)
                noConnectionText.text = noConnectionLocalizedText.IsEmpty
                    ? "Sin conexión"
                    : noConnectionLocalizedText.GetLocalizedString();
        }
    }


    public void RefreshCurrentLeaderboard()
    {
        // Si aún no se ha mostrado ningún modo, no hacemos nada
        if (string.IsNullOrEmpty(currentRequestedStat))
            return;

        // Si ya está cargando, mejor no pisar la petición
        if (isLoading)
            return;

        // Volvemos a pedir el mismo leaderboard que está activo ahora
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

        if (data == null || data.sprite == null)
        {
            // Sin datos → avatar por defecto sin shader
            targetImage.material = null;
            return;
        }

        targetImage.sprite = data.sprite;

        if (data.hasShaderEffect && data.effectMaterial != null)
        {
            // Instanciamos material para no modificar el asset global
            var matInstance = Instantiate(data.effectMaterial);
            targetImage.material = matInstance;

            // Si quieres, setea aquí los parámetros del shader
            // Ojo con los nombres reales de las props del AllIn1SpriteShader
           //  matInstance.SetFloat("_WaveAmount", data.waveAmount);
           // matInstance.SetFloat("_WaveSpeed", data.waveSpeed);
            // matInstance.SetFloat("_WaveStrength", data.waveStrength);
            // etc. según lo que uses
        }
        else
        {
            // Avatares sin efecto → material por defecto
            targetImage.material = null;
        }
    }

}
