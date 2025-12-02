using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XPUIAnimation : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform panel;   // Panel del perfil
    [SerializeField] private Button openButton;     // Botón de usuario (icono arriba en el HUD)
    [SerializeField] private Button closeButton;    // Botón X dentro del panel

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

    // --------- AVATAR ---------
    [Header("Avatar del usuario")]
    [SerializeField] private Image avatarImage;          // Imagen circular grande
    [SerializeField] private AvatarDataSO[] avatarDB;    // TODOS los AvatarDataSO
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

    private void Awake()
    {
        if (panel == null)
            panel = GetComponent<RectTransform>();

        // Guardamos la posición “buena” tal y como está en el editor
        shownPosition = panel.anchoredPosition;

        // Empezamos ocultos abajo
        panel.anchoredPosition = hiddenPosition;

        if (openButton != null)
            openButton.onClick.AddListener(OpenPanel);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    private void OnEnable()
    {
        // Si el login termina mientras este panel está activo, refrescamos el nombre
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
    }

    private void Start()
    {
        LoadCurrentAvatarSprite();
        UpdateLevelUI();
        LoadUsername();
        UpdateRecordsUI();   // <- inicializamos también los récords
    }

    private void Update()
    {
        // Si el nivel/XP puede cambiar mientras juegas, lo refrescamos
        UpdateLevelUI();
        // Si los récords pueden cambiar mientras juegas (por ejemplo tras una partida),
        // también los refrescamos aquí. Si no hace falta, puedes quitar esta línea.
        UpdateRecordsUI();
    }

    // --------- ABRIR / CERRAR ---------

    public void OpenPanel()
    {
        if (isShown || isMoving) return;

        isShown = true;
        LoadCurrentAvatarSprite();
        UpdateLevelUI();
        LoadUsername();
        UpdateRecordsUI();

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(SlidePanel(panel.anchoredPosition, shownPosition));
    }

    public void ClosePanel()
    {
        if (!isShown || isMoving) return;

        isShown = false;

        // Lanzamos la animación POP DEL BOTÓN antes de cerrar
        if (closeButton != null)
            StartCoroutine(CloseButtonPopAnimation());

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(SlidePanel(panel.anchoredPosition, hiddenPosition));
    }

    private IEnumerator CloseButtonPopAnimation()
    {
        Transform btn = closeButton.transform;
        Vector3 original = btn.localScale;
        Vector3 target = original * closePopScale;

        // Reducir
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * closePopSpeed;
            btn.localScale = Vector3.Lerp(original, target, t);
            yield return null;
        }

        // Volver al tamaño normal
        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * closePopSpeed;
            btn.localScale = Vector3.Lerp(target, original, t);
            yield return null;
        }

        btn.localScale = original;
    }

    private IEnumerator SlidePanel(Vector2 from, Vector2 to)
    {
        isMoving = true;
        float t = 0f;

        while (t < slideDuration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / slideDuration);
            float eased = slideCurve.Evaluate(lerp);

            panel.anchoredPosition = Vector2.Lerp(from, to, eased);
            yield return null;
        }

        panel.anchoredPosition = to;
        isMoving = false;
    }

    // --------- AVATAR ---------

    private void LoadCurrentAvatarSprite()
    {
        if (avatarImage == null) return;

        string avatarId = PlayerPrefs.GetString("EquippedAvatarId", "NormalAvatar");
        AvatarDataSO data = GetAvatarById(avatarId);

        if (data != null && data.sprite != null)
            avatarImage.sprite = data.sprite;
        else if (fallbackAvatar != null)
            avatarImage.sprite = fallbackAvatar;
    }

    private AvatarDataSO GetAvatarById(string id)
    {
        if (string.IsNullOrEmpty(id) || avatarDB == null) return null;

        foreach (var a in avatarDB)
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
            // 1) Si el manager ya tiene DisplayName (después del login), lo usamos
            if (!string.IsNullOrEmpty(login.DisplayName))
            {
                finalName = login.DisplayName;
            }
            else
            {
                // 2) Si no, usamos el nombre local que guarda el propio manager
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
        // Cambia las claves de PlayerPrefs aquí si en tu proyecto usas otras
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
