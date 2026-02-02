using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class MiniGameSelector : MonoBehaviour
{
    [System.Serializable]
    public class MiniGameInfo
    {
        public Sprite image;

        public LocalizedString name;
        public LocalizedString description;
        public LocalizedString recordLabel;

        public string recordKey;
        public string sceneName;

        public bool showMotionTasksToggle;
    }

    public MiniGameInfo[] miniGames;

    public Image currentImage;
    public Image nextImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI recordText;
    public Button playButton;
    public Button leftArrowButton;
    public Button rightArrowButton;

    private int currentIndex = 0;
    public float buttonDisableDuration = 0.3f;
    public float moveDistance = 500f;
    public float animDuration = 0.3f;

    public Vector3 targetScale = new Vector3(0.3662947f, 0.1966797f, 1f);

    private Vector3 initialPos;

    private const string LastMiniGameIndexKey = "LastMiniGameIndex";

    [Header("Mode Options")]
    [SerializeField] private Toggle motionTasksToggle;

    [Header("Tutorial Option")]
    [SerializeField] private Toggle showTutorialToggle;
    private const string ShowTutorialKey = "ShowTutorialOnStart";

    void Start()
    {
        // --- Restaurar el último minijuego guardado ---
        currentIndex = PlayerPrefs.GetInt(LastMiniGameIndexKey, 0);
        currentIndex = Mathf.Clamp(currentIndex, 0, miniGames.Length - 1);

        // Guardar posición inicial
        initialPos = currentImage.rectTransform.localPosition;

        // Escala inicial correcta
        currentImage.rectTransform.localScale = targetScale;
        nextImage.rectTransform.localScale = targetScale;

        // Configurar minijuego inicial restaurado
        UpdateTextUI(currentIndex);
        currentImage.sprite = miniGames[currentIndex].image;
        currentImage.color = Color.white;
        nextImage.color = new Color(1, 1, 1, 0);

        // --- Tutorial toggle: cargar + escuchar cambios ---
        if (showTutorialToggle != null)
        {
            bool saved = PlayerPrefs.GetInt(ShowTutorialKey, 1) == 1; // por defecto ON
            showTutorialToggle.SetIsOnWithoutNotify(saved);
            showTutorialToggle.onValueChanged.AddListener(OnShowTutorialToggleChanged);
        }

        // Listeners
        playButton.onClick.AddListener(OnPlayButton);
        leftArrowButton.onClick.AddListener(() => OnArrowClicked(false));
        rightArrowButton.onClick.AddListener(() => OnArrowClicked(true));
    }

    private void OnShowTutorialToggleChanged(bool value)
    {
        PlayerPrefs.SetInt(ShowTutorialKey, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnArrowClicked(bool toRight)
    {
        leftArrowButton.interactable = false;
        rightArrowButton.interactable = false;
        playButton.interactable = false;

        AnimateTransition(toRight);
    }

    void AnimateTransition(bool toRight)
    {
        int nextIndex = toRight ? (currentIndex + 1) % miniGames.Length
                                : (currentIndex - 1 + miniGames.Length) % miniGames.Length;

        Vector3 startPos = initialPos;
        Vector3 outPos = startPos + (toRight ? Vector3.right : Vector3.left) * moveDistance;
        Vector3 inPos = startPos - (toRight ? Vector3.right : Vector3.left) * moveDistance;

        currentImage.rectTransform.localScale = targetScale;
        nextImage.rectTransform.localScale = targetScale;

        nextImage.rectTransform.localPosition = inPos;
        nextImage.color = new Color(1, 1, 1, 0);
        nextImage.sprite = miniGames[nextIndex].image;

        Sequence seq = DOTween.Sequence();
        seq.Join(currentImage.rectTransform.DOLocalMove(outPos, animDuration).SetEase(Ease.OutQuad));
        seq.Join(currentImage.DOFade(0, animDuration));
        seq.Join(nextImage.rectTransform.DOLocalMove(startPos, animDuration).SetEase(Ease.OutQuad));
        seq.Join(nextImage.DOFade(1, animDuration));

        seq.OnComplete(() =>
        {
            var temp = currentImage;
            currentImage = nextImage;
            nextImage = temp;

            currentIndex = nextIndex;
            UpdateTextUI(currentIndex);

            PlayerPrefs.SetInt(LastMiniGameIndexKey, currentIndex);
            PlayerPrefs.Save();

            currentImage.rectTransform.localScale = targetScale;
            nextImage.rectTransform.localScale = targetScale;

            currentImage.rectTransform.localPosition = initialPos;
            nextImage.rectTransform.localPosition = initialPos;

            nextImage.color = new Color(1, 1, 1, 0);

            leftArrowButton.interactable = true;
            rightArrowButton.interactable = true;
            playButton.interactable = true;
        });
    }

    private void UpdateTextUI(int index)
    {
        var game = miniGames[index];

        nameText.text = game.name.GetLocalizedString();
        descriptionText.text = game.description.GetLocalizedString();

        int record = PlayerPrefs.GetInt(game.recordKey, 0);
        string recordLabel = game.recordLabel.GetLocalizedString();
        recordText.text = $"{recordLabel}: {record}";

        if (motionTasksToggle != null)
            motionTasksToggle.gameObject.SetActive(game.showMotionTasksToggle);
    }

    public void OnPlayButton()
    {
        PlayerPrefs.SetInt(LastMiniGameIndexKey, currentIndex);
        PlayerPrefs.Save();

        string sceneName = miniGames[currentIndex].sceneName;
        string modeName = miniGames[currentIndex].name.GetLocalizedString();

        if (TransitionScript.Instance != null)
            TransitionScript.Instance.TransitionToScene(sceneName, modeName);
        else
            SceneManager.LoadScene(sceneName);
    }

    void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    void OnLocaleChanged(Locale newLocale)
    {
        UpdateTextUI(currentIndex);
    }
}
