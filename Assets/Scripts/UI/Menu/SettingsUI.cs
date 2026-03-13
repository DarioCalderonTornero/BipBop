using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using UnityEngine.Localization;

public class SettingsUI : MonoBehaviour
{
    [Header("Images")]
    [SerializeField] private Image blackBackgroundImage;
    [SerializeField] private Image settingsBackgroundImage;
    [SerializeField] private Image idiomaBackgroundImage;
    [SerializeField] private Image vibrationBackgroundImage;
    [SerializeField] private Image soundBackgroundImage;
    [SerializeField] private Image musicBackgroundImage;
    [SerializeField] private Image creditsBackgroundImage;
    [SerializeField] private Image resetSettingsImage;
    [SerializeField] private Image generalNextLineImage;
    [SerializeField] private Image audioNextLineImage;
    [SerializeField] private Image extrasNextLineImage;
    [SerializeField] private Image FPSNextLineImage;

    [Header("Buttons")]
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button openSettingsButton;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private Button idiomaButton;

    [SerializeField] private Button vibrationButton;
    [SerializeField] private Button soundVolumeButton;
    [SerializeField] private Button musicVolumeButton;
    [SerializeField] private Button resetSettingsButton;
    [SerializeField] private Button FPSSettingsButton;

    [Header("General Texts")]
    [SerializeField] private TextMeshProUGUI settingsText;
    [SerializeField] private TextMeshProUGUI generalSettingsText;
    [SerializeField] private TextMeshProUGUI audioSettingsText;
    [SerializeField] private TextMeshProUGUI extraSettingsText;

    [SerializeField] private TextMeshProUGUI idiomaButtonText;

    [Header("Settings Texts")]
    [SerializeField] private TextMeshProUGUI idiomaText;
    [SerializeField] private TextMeshProUGUI vibrationText;
    [SerializeField] private TextMeshProUGUI soundText;
    [SerializeField] private TextMeshProUGUI musicText;
    [SerializeField] private TextMeshProUGUI creditsText;
    [SerializeField] private TextMeshProUGUI resetText;
    [SerializeField] private TextMeshProUGUI FPSText;

    [SerializeField] private TextMeshProUGUI viewText;

    /* [Header("Sound Logic Images")]
    [SerializeField] private Image soundVolumeOnImage;
    [SerializeField] private Image soundVolumeOffImage;

    [SerializeField] private Image musicVolumeOnImage;
    [SerializeField] private Image musicVolumeOffImage;*/

    [Header("Sound Logic Texts (ON/OFF)")]
    [SerializeField] private TextMeshProUGUI soundStateText;
    [SerializeField] private TextMeshProUGUI musicStateText;

    [SerializeField] private LocalizedString lsOn;
    [SerializeField] private LocalizedString lsOff;

    [Header("Sprite Images")]
    [SerializeField] private Image settingsImage;
    [SerializeField] private Image idiomaImage;
    [SerializeField] private Image vibrationImage;
    [SerializeField] private Image soundImage;
    [SerializeField] private Image musicImage;
    [SerializeField] private Image creditsImage;
    [SerializeField] private Image resetImage;
    [SerializeField] private Image FPSImage;

    [Header("Switch Sprites")]
    [SerializeField] private Image soundYellowImage;
    [SerializeField] private Image soundPurpleImage;

    [SerializeField] private Image musicYellowImage;
    [SerializeField] private Image musicPurpleImage;

    [SerializeField] private Image idiomaYellowImage;
    [SerializeField] private Image idiomaPurpleImage;


    [Header("Vibration State Images")]
    [SerializeField] private Image vibrationOnImage;
    [SerializeField] private Image vibrationOffImage;

    [SerializeField] private Animator settingsAnimator;
    [SerializeField] private Settings settings;

    private bool isVibrationImageOn = true;

    [System.Serializable]
    private class SwitchVisual
    {
        public Button button;                 // SoundVolumeButton / MusicVolumeButton / IdiomaButton
        public Image targetImage;             // Imagen a la que cambias el sprite (si null, usa button.image)
        public Sprite leftSprite;             // Amarillo
        public Sprite rightSprite;            // Morado
        public float leftX = -45f;            // Ajusta a tu UI
        public float rightX = 45f;            // Ajusta a tu UI
        public float moveTime = 0.12f;        // Animación
    }

    [Header("Switch Visuals (Yellow Left / Purple Right)")]
    [SerializeField] private SwitchVisual soundSwitch;
    [SerializeField] private SwitchVisual musicSwitch;
    [SerializeField] private SwitchVisual idiomaSwitch;

    [Header("Background Fade")]
    [SerializeField] private CanvasGroup blackBgGroup;   // CanvasGroup del blackBackgroundImage
    [SerializeField] private float bgFadeTime = 0.15f;

    private Coroutine _bgFadeCo;

    [Header("Localization (Settings Table)")]
    [SerializeField] private LocalizedString lsSettingsTitle;
    [SerializeField] private LocalizedString lsIdioma;
    [SerializeField] private LocalizedString lsVibration;
    [SerializeField] private LocalizedString lsSound;
    [SerializeField] private LocalizedString lsMusic;
    [SerializeField] private LocalizedString lsCredits;
    [SerializeField] private LocalizedString lsReset;
    [SerializeField] private LocalizedString lsView;

    [Header("Popup (No Animator)")]
    [SerializeField] private RectTransform popupRoot;   // el root del panel (SettingsBackground o un padre)
    [SerializeField] private CanvasGroup popupGroup;    // opcional pero recomendado (alpha + bloquear clicks)
    [SerializeField] private float popInTime = 0.18f;
    [SerializeField] private float popOutTime = 0.12f;
    [SerializeField] private float startScale = 0.85f;

    private Coroutine _popCo;

    private const string LanguageKey = "SelectedLocale";

    private readonly int[] fpsOptions = { 60, 90, 120, -1 }; // -1 = AUTO

    [Header("FPS UI")]
    [SerializeField] private TextMeshProUGUI fpsValueText;

    [Header("FPS Sprites")]
    [SerializeField] private Sprite fps60Sprite;
    [SerializeField] private Sprite fps90Sprite;
    [SerializeField] private Sprite fps120Sprite;
    [SerializeField] private Sprite fpsAutoSprite;

    [Header("Credits Popup")]
    [SerializeField] private RectTransform creditsPopupRoot;
    [SerializeField] private CanvasGroup creditsPopupGroup;
    [SerializeField] private Button closeCreditsButton;

    [SerializeField] private float creditsPopInTime = 0.18f;
    [SerializeField] private float creditsPopOutTime = 0.12f;
    [SerializeField] private float creditsStartScale = 0.85f;

    private Coroutine _creditsPopCo;
    private bool _creditsOpen;

    private void Awake()
    {
        openSettingsButton.onClick.AddListener(() =>
        {
            FadeBlackBg(true);
            PopOpen();
        });

        closeSettingsButton.onClick.AddListener(() =>
        {
            if (_creditsOpen)
                CloseCredits();
            else
            {
                FadeBlackBg(false);
                PopClose();
            }
        });

        creditsButton.onClick.AddListener(OpenCredits);
        closeCreditsButton.onClick.AddListener(CloseCredits);

        SetCreditsVisibleInstant(false);

        if (settingsAnimator != null)
        {
            settingsAnimator.enabled = false;
        }

        vibrationButton.onClick.AddListener(() =>
        {
            bool newState = !Haptics.enabled;
            Haptics.SetEnabled(newState);

            if (newState)
                Haptics.TryVibrate();

            RefreshVibrationUI();
        });

        soundVolumeButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.GetCancelVolume();
            HandleCancelSoundImage();
            RefreshSoundSwitch(false);
        });

        musicVolumeButton.onClick.AddListener(() =>
        {
            MusicManager.Instance.CancelMusicVolume();
            HandleCancelMusicImage();
            RefreshMusicSwitch(false);
        });

        idiomaButton.onClick.AddListener(() =>
        {
            ToggleLanguage();
        });

        resetSettingsButton.onClick.AddListener(() =>
        {
            settings.ResetSettings();
            RefreshVibrationUI();
            HandleCancelSoundImage();
            HandleCancelMusicImage();
        });

        FPSSettingsButton.onClick.AddListener(CycleFPS);
    }

    private void ShowWithPop()
    {
        Show();               // tu lógica de activar textos, imágenes, botones, etc.
        FadeBlackBg(true);

        if (_popCo != null) StopCoroutine(_popCo);
        _popCo = StartCoroutine(PopRoutine(true));
    }

    private void HideWithPop()
    {
        if (_popCo != null) StopCoroutine(_popCo);
        _popCo = StartCoroutine(PopRoutine(false));
    }

    private IEnumerator Start()
    {
        Show();
        SetSettingsVisibleInstant(false);

        RefreshVibrationUI();
        HandleCancelSoundImage();
        HandleCancelMusicImage();

        LocalizationSettings.SelectedLocaleChanged += LocalizationSettings_SelectedLocaleChanged;

        yield return LocalizationSettings.InitializationOperation;

        // ✅ Aplicar idioma guardado aquí (SIEMPRE)
        string saved = PlayerPrefs.GetString(LanguageKey, "");
        if (!string.IsNullOrEmpty(saved) && LocalizationSettings.AvailableLocales != null)
        {
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

        // ✅ Pintar UI con el locale ya aplicado
        ApplyLocalizedTexts();
        RefreshLenguage(LocalizationSettings.SelectedLocale);

        RefreshSoundSwitch(true);
        RefreshMusicSwitch(true);
        RefreshIdiomaSwitch(true);

        HandleCancelSoundImage();
        HandleCancelMusicImage();

        RefreshFPSValue();
    }

    private void HandleCancelSoundImage()
    {
        bool soundOn = SoundManager.Instance.GetSoundVolumeNormalized() > 0f;

        if (soundStateText != null)
            soundStateText.text = (soundOn ? lsOn : lsOff).GetLocalizedString();
    }

    private void HandleCancelMusicImage()
    {
        bool musicOn = MusicManager.Instance.GetMusicVolumeNormalized() > 0f;

        if (musicStateText != null)
            musicStateText.text = (musicOn ? lsOn : lsOff).GetLocalizedString();
    }

    private void CycleFPS()
    {
        int current = FPSManager.GetSavedFPS();

        int idx = System.Array.IndexOf(fpsOptions, current);
        if (idx < 0) idx = 0;

        int next = fpsOptions[(idx + 1) % fpsOptions.Length];

        FPSManager.SetFPS(next);
        RefreshFPSValue();
    }

    private void RefreshFPSValue()
    {
        int currentFPS = FPSManager.GetSavedFPS();

        if (fpsValueText != null)
            fpsValueText.text = FPSManager.GetFPSDisplay(); // "60" / "90" / "120" / "AUTO"

        if (FPSImage != null)
        {
            switch (currentFPS)
            {
                case 60:
                    FPSImage.sprite = fps60Sprite;
                    break;

                case 90:
                    FPSImage.sprite = fps90Sprite;
                    break;

                case 120:
                    FPSImage.sprite = fps120Sprite;
                    break;

                case -1: // AUTO
                    FPSImage.sprite = fpsAutoSprite;
                    break;

                default:
                    FPSImage.sprite = fpsAutoSprite;
                    break;
            }
        }
    }

    private void LocalizationSettings_SelectedLocaleChanged(Locale newLocale)
    {
        ApplyLocalizedTexts();
        RefreshLenguage(newLocale);
        RefreshIdiomaSwitch(false);

        HandleCancelSoundImage(); // <- para recalcular ON/OFF traducido
        HandleCancelMusicImage();
    }

    private void RefreshLenguage(Locale locale)
    {
        if (locale == null) return;

        string code = locale.Identifier.Code;
        if (code.StartsWith("es")) idiomaButtonText.text = "ESP";
        else if (code.StartsWith("en")) idiomaButtonText.text = "ENG";
    }


    private void RefreshVibrationUI()
    {
        if (vibrationButton == null) return;

        Color on = new Color(1f, 0.75f, 0.2f, 1f);
        Color off = new Color(0.6f, 0.6f, 0.6f, 1f);

        bool enabled = Haptics.enabled;

        // Colores del botón
        var cb = vibrationButton.colors;
        Color baseCol = enabled ? on : off;
        cb.normalColor = baseCol;
        cb.highlightedColor = baseCol * 1.05f;
        cb.pressedColor = baseCol * 0.9f;
        cb.selectedColor = baseCol;
        cb.disabledColor = new Color(baseCol.r, baseCol.g, baseCol.b, 0.5f);
        cb.colorMultiplier = 1f;
        vibrationButton.colors = cb;

        if (vibrationButton.image != null)
            vibrationButton.image.color = baseCol;

        // Mostrar/ocultar imágenes según el estado
        if (vibrationOnImage != null)
            vibrationOnImage.gameObject.SetActive(enabled);

        if (vibrationOffImage != null)
            vibrationOffImage.gameObject.SetActive(!enabled);
    }

    private void Show()
    {
        //Animations
        settingsAnimator.SetBool("IsSettingsOpen", true);
        settingsAnimator.SetBool("IsSettingsClose", false);
        //StartCoroutine(OpenSettingsAnimFalse());

        //-----GENERAL SHOW/HIDE LOGIC------//

        //Texts Show
        settingsText.gameObject.SetActive(true);
        generalSettingsText.gameObject.SetActive(true);
        audioSettingsText.gameObject.SetActive(true);
        extraSettingsText.gameObject.SetActive(true);

        idiomaText.gameObject.SetActive(true);
        vibrationText.gameObject.SetActive(true);
        soundText.gameObject.SetActive(true);
        musicText.gameObject.SetActive(true);
        creditsText.gameObject.SetActive(true);
        resetText.gameObject.SetActive(true);
        FPSText.gameObject.SetActive(true);

        //Images Show
        settingsBackgroundImage.gameObject.SetActive(true);
        blackBackgroundImage.gameObject.SetActive(true);
        FadeBlackBg(true);

        idiomaBackgroundImage.gameObject.SetActive(true);
        vibrationBackgroundImage.gameObject.SetActive(true);
        soundBackgroundImage.gameObject.SetActive(true);    
        musicBackgroundImage.gameObject.SetActive(true);
        creditsBackgroundImage.gameObject.SetActive(true);
        resetSettingsImage.gameObject.SetActive(true); 
        audioNextLineImage.gameObject.SetActive(true);
        extrasNextLineImage.gameObject.SetActive(true);
        generalNextLineImage.gameObject.SetActive(true);
        FPSNextLineImage.gameObject.SetActive(true);

        //Images sprites Show
        settingsImage.gameObject.SetActive(true);
        idiomaImage.gameObject.SetActive(true); 
        vibrationImage.gameObject.SetActive(true);
        soundImage.gameObject.SetActive(true);  
        musicImage.gameObject.SetActive(true);  
        creditsImage.gameObject.SetActive(true);
        resetImage.gameObject.SetActive(true);
        FPSImage.gameObject.SetActive(true);

        //Buttons Show
        creditsButton.gameObject.SetActive(true);
        closeSettingsButton.gameObject.SetActive(true);
        idiomaButton.gameObject.SetActive(true);    
        vibrationButton.gameObject.SetActive(true);
        soundVolumeButton.gameObject.SetActive(true);
        musicVolumeButton.gameObject.SetActive(true);  
        resetSettingsButton.gameObject.SetActive(true);
        FPSSettingsButton.gameObject.SetActive(true);

        soundYellowImage.gameObject.SetActive(true);
        soundPurpleImage.gameObject.SetActive(true);

        musicYellowImage.gameObject.SetActive(true);
        musicPurpleImage.gameObject.SetActive(true);

        idiomaYellowImage.gameObject.SetActive(true);
        idiomaPurpleImage.gameObject.SetActive(true);

        if (soundStateText != null) soundStateText.gameObject.SetActive(true);
        if (musicStateText != null) musicStateText.gameObject.SetActive(true);
    }

    public void Hide()
    {
        //-----GENERAL SHOW/HIDE LOGIC------//

        //Texts Hide
        settingsText.gameObject.SetActive(false);
        generalSettingsText.gameObject.SetActive(false);
        audioSettingsText.gameObject.SetActive(false);
        extraSettingsText.gameObject.SetActive(false);

        idiomaText.gameObject.SetActive(false);
        vibrationText.gameObject.SetActive(false);
        soundText.gameObject.SetActive(false);
        musicText.gameObject.SetActive(false);
        creditsText.gameObject.SetActive(false);
        resetText.gameObject.SetActive(false);
        FPSText.gameObject.SetActive(false);

        //Images Hide
        settingsBackgroundImage.gameObject.SetActive(false);
        blackBackgroundImage.gameObject.SetActive(false);

        idiomaBackgroundImage.gameObject.SetActive(false);
        vibrationBackgroundImage.gameObject.SetActive(false);
        soundBackgroundImage.gameObject.SetActive(false);
        musicBackgroundImage.gameObject.SetActive(false);
        creditsBackgroundImage.gameObject.SetActive(false);
        resetSettingsImage.gameObject.SetActive(false);
        audioNextLineImage.gameObject.SetActive(false);
        extrasNextLineImage.gameObject.SetActive(false);
        generalNextLineImage.gameObject.SetActive(false);
        FPSNextLineImage.gameObject.SetActive(false);

        //Images sprites Show
        settingsImage.gameObject.SetActive(false);
        idiomaImage.gameObject.SetActive(false);
        vibrationImage.gameObject.SetActive(false);
        soundImage.gameObject.SetActive(false);
        musicImage.gameObject.SetActive(false);
        creditsImage.gameObject.SetActive(false);
        resetImage.gameObject.SetActive(false);
        FPSImage.gameObject.SetActive(false);

        //Buttons Hide
        creditsButton.gameObject.SetActive(false);
        closeSettingsButton.gameObject.SetActive(false);
        idiomaButton.gameObject.SetActive(false);
        vibrationButton.gameObject.SetActive(false);
        soundVolumeButton.gameObject.SetActive(false);
        musicVolumeButton.gameObject.SetActive(false);
        resetSettingsButton.gameObject.SetActive(false);
        FPSSettingsButton.gameObject.SetActive(false);

        soundYellowImage.gameObject.SetActive(false);
        soundPurpleImage.gameObject.SetActive(false);

        musicYellowImage.gameObject.SetActive(false);
        musicPurpleImage.gameObject.SetActive(false);

        idiomaYellowImage.gameObject.SetActive(false);
        idiomaPurpleImage.gameObject.SetActive(false);

        if (soundStateText != null) soundStateText.gameObject.SetActive(false);
        if (musicStateText != null) musicStateText.gameObject.SetActive(false);

        SetCreditsVisibleInstant(false);
    }

    private Coroutine _soundMoveCo, _musicMoveCo, _idiomaMoveCo;

    private void ApplySwitch(SwitchVisual sw, bool left, bool instant, ref Coroutine runningCo)
    {
        if (sw == null || sw.button == null) return;

        var img = sw.targetImage != null ? sw.targetImage : sw.button.image;
        if (img != null) img.sprite = left ? sw.leftSprite : sw.rightSprite;

        RectTransform rt = sw.button.GetComponent<RectTransform>();
        if (rt == null) return;

        float targetX = left ? sw.leftX : sw.rightX;

        if (runningCo != null) StopCoroutine(runningCo);

        if (instant)
        {
            var p = rt.anchoredPosition;
            p.x = targetX;
            rt.anchoredPosition = p;
            runningCo = null;
        }
        else
        {
            runningCo = StartCoroutine(MoveX(rt, targetX, sw.moveTime));
        }
    }

    private void SetSettingsVisibleInstant(bool show)
    {
        if (popupRoot != null) popupRoot.localScale = show ? Vector3.one : Vector3.one * startScale;

        if (popupGroup != null)
        {
            popupGroup.alpha = show ? 1f : 0f;
            popupGroup.blocksRaycasts = show;
            popupGroup.interactable = show;
        }
    }

    private IEnumerator MoveX(RectTransform rt, float targetX, float t)
    {
        Vector2 start = rt.anchoredPosition;
        Vector2 end = new Vector2(targetX, start.y);

        if (t <= 0f)
        {
            rt.anchoredPosition = end;
            yield break;
        }

        float e = 0f;
        while (e < t)
        {
            e += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(e / t);
            // suavizado simple
            a = a * a * (3f - 2f * a);
            rt.anchoredPosition = Vector2.LerpUnclamped(start, end, a);
            yield return null;
        }

        rt.anchoredPosition = end;
    }

    private void RefreshSoundSwitch(bool instant)
    {
        bool soundOn = SoundManager.Instance.GetSoundVolumeNormalized() > 0f;
        // ON = izquierda (amarillo), OFF = derecha (morado)
        ApplySwitch(soundSwitch, soundOn, instant, ref _soundMoveCo);
    }

    private void RefreshMusicSwitch(bool instant)
    {
        bool musicOn = MusicManager.Instance.GetMusicVolumeNormalized() > 0f;
        ApplySwitch(musicSwitch, musicOn, instant, ref _musicMoveCo);
    }

    private void RefreshIdiomaSwitch(bool instant)
    {
        var loc = LocalizationSettings.SelectedLocale;
        string code = loc != null ? loc.Identifier.Code : "es";
        bool isSpanish = code.StartsWith("es");

        // ✅ Ajusta esto según tu UI real:
        // Si tu switch tiene ESP a la izquierda y ENG a la derecha:
        bool left = isSpanish;

        // Si en tu UI es al revés (ENG izquierda, ESP derecha), usa:
        // bool left = !isSpanish;

        ApplySwitch(idiomaSwitch, left, instant, ref _idiomaMoveCo);
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
            if (isSpanish && l.Identifier.Code.StartsWith("en")) { target = l; break; }
            if (!isSpanish && l.Identifier.Code.StartsWith("es")) { target = l; break; }
        }

        if (target == null) return;

        LocalizationSettings.SelectedLocale = target;

        // ✅ Guarda EXACTO lo que has puesto
        PlayerPrefs.SetString(LanguageKey, target.Identifier.Code);
        PlayerPrefs.Save();
    }

    private void ApplyLocalizedTexts()
    {
        if (settingsText != null) settingsText.text = lsSettingsTitle.GetLocalizedString();

        if (idiomaText != null) idiomaText.text = lsIdioma.GetLocalizedString();
        if (vibrationText != null) vibrationText.text = lsVibration.GetLocalizedString();
        if (soundText != null) soundText.text = lsSound.GetLocalizedString();
        if (musicText != null) musicText.text = lsMusic.GetLocalizedString();
        if (creditsText != null) creditsText.text = lsCredits.GetLocalizedString();
        if (resetText != null) resetText.text = lsReset.GetLocalizedString();

        if (viewText != null) viewText.text = lsView.GetLocalizedString();
    }

    private void FadeBlackBg(bool show)
    {
        if (blackBgGroup == null) return;

        if (_bgFadeCo != null) StopCoroutine(_bgFadeCo);
        _bgFadeCo = StartCoroutine(FadeRoutine(blackBgGroup, show ? 1f : 0f, bgFadeTime, () =>
        {
            blackBackgroundImage.gameObject.SetActive(show);
        }));
    }

    private IEnumerator FadeRoutine(CanvasGroup cg, float target, float time, System.Action onDone)
    {
        float start = cg.alpha;
        float t = 0f;

        // si vamos a mostrar, actívalo antes
        if (target > 0f && !cg.gameObject.activeSelf)
            cg.gameObject.SetActive(true);

        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float a = time <= 0f ? 1f : Mathf.Clamp01(t / time);
            cg.alpha = Mathf.Lerp(start, target, a);
            yield return null;
        }

        cg.alpha = target;
        onDone?.Invoke();
    }

    private void PopOpen()
    {
        if (_popCo != null) StopCoroutine(_popCo);
        _popCo = StartCoroutine(PopRoutine(true));
    }

    private void PopClose()
    {
        if (_creditsOpen)
            SetCreditsVisibleInstant(false);

        if (_popCo != null) StopCoroutine(_popCo);
        _popCo = StartCoroutine(PopRoutine(false));
    }

    private IEnumerator PopRoutine(bool opening)
    {
        if (popupRoot == null || popupGroup == null) yield break;

        // mostrar y permitir pop aunque estaba "invisible"
        popupGroup.alpha = 1f;
        popupGroup.blocksRaycasts = opening;
        popupGroup.interactable = opening;

        float time = opening ? popInTime : popOutTime;
        float t = 0f;

        Vector3 from = opening ? Vector3.one * startScale : Vector3.one;
        Vector3 to = opening ? Vector3.one : Vector3.one * startScale;

        popupRoot.localScale = from;

        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float a = time <= 0f ? 1f : Mathf.Clamp01(t / time);
            float eased = opening ? EaseOutBack(a) : EaseInBack(a);
            popupRoot.localScale = Vector3.LerpUnclamped(from, to, eased);
            yield return null;
        }

        popupRoot.localScale = to;

        if (!opening)
        {
            popupGroup.alpha = 0f;            // “invisible”
            popupGroup.blocksRaycasts = false;
            popupGroup.interactable = false;
        }
    }

    private float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    private float EaseInBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return c3 * x * x * x - c1 * x * x;
    }

    private void OpenCredits()
    {
        if (_creditsPopCo != null) StopCoroutine(_creditsPopCo);
        _creditsPopCo = StartCoroutine(CreditsPopRoutine(true));
    }

    private void CloseCredits()
    {
        if (_creditsPopCo != null) StopCoroutine(_creditsPopCo);
        _creditsPopCo = StartCoroutine(CreditsPopRoutine(false));
    }

    private void SetCreditsVisibleInstant(bool show)
    {
        if (creditsPopupRoot != null)
            creditsPopupRoot.localScale = show ? Vector3.one : Vector3.one * creditsStartScale;

        if (creditsPopupGroup != null)
        {
            creditsPopupGroup.alpha = show ? 1f : 0f;
            creditsPopupGroup.blocksRaycasts = show;
            creditsPopupGroup.interactable = show;
        }

        _creditsOpen = show;
    }

    private IEnumerator CreditsPopRoutine(bool opening)
    {
        if (creditsPopupRoot == null || creditsPopupGroup == null)
            yield break;

        _creditsOpen = opening;

        creditsPopupGroup.alpha = 1f;
        creditsPopupGroup.blocksRaycasts = opening;
        creditsPopupGroup.interactable = opening;

        float time = opening ? creditsPopInTime : creditsPopOutTime;
        float t = 0f;

        Vector3 from = opening ? Vector3.one * creditsStartScale : Vector3.one;
        Vector3 to = opening ? Vector3.one : Vector3.one * creditsStartScale;

        creditsPopupRoot.localScale = from;

        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float a = time <= 0f ? 1f : Mathf.Clamp01(t / time);
            float eased = opening ? EaseOutBack(a) : EaseInBack(a);
            creditsPopupRoot.localScale = Vector3.LerpUnclamped(from, to, eased);
            yield return null;
        }

        creditsPopupRoot.localScale = to;

        if (!opening)
        {
            creditsPopupGroup.alpha = 0f;
            creditsPopupGroup.blocksRaycasts = false;
            creditsPopupGroup.interactable = false;
        }
    }

    void OnDestroy()
    {
        LocalizationSettings.SelectedLocaleChanged -= LocalizationSettings_SelectedLocaleChanged;
    }
}
