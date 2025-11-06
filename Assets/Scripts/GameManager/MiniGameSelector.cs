using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class MiniGameSelector : MonoBehaviour
{
    [System.Serializable]
    public class MiniGameInfo
    {
        public Sprite image;
        public string name;
        public string description;
        public string recordKey;
        public string sceneName;
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

    void Start()
    {
        // Guardar posición inicial
        initialPos = currentImage.rectTransform.localPosition;

        // Escala inicial correcta
        currentImage.rectTransform.localScale = targetScale;
        nextImage.rectTransform.localScale = targetScale;

        // Configurar minijuego inicial
        UpdateTextUI(currentIndex);
        currentImage.sprite = miniGames[currentIndex].image;
        currentImage.color = Color.white;
        nextImage.color = new Color(1, 1, 1, 0);

        // Listeners
        playButton.onClick.AddListener(OnPlayButton);
        leftArrowButton.onClick.AddListener(() => OnArrowClicked(false));
        rightArrowButton.onClick.AddListener(() => OnArrowClicked(true));
    }

    private void OnArrowClicked(bool toRight)
    {
        // Desactivar botones durante animación
        leftArrowButton.interactable = false;
        rightArrowButton.interactable = false;
        playButton.interactable = false;

        AnimateTransition(toRight);
    }

    void AnimateTransition(bool toRight)
    {
        int nextIndex = toRight ? (currentIndex + 1) % miniGames.Length : (currentIndex - 1 + miniGames.Length) % miniGames.Length;

        Vector3 startPos = initialPos;
        Vector3 outPos = startPos + (toRight ? Vector3.right : Vector3.left) * moveDistance;
        Vector3 inPos = startPos - (toRight ? Vector3.right : Vector3.left) * moveDistance;

        // Asegurar escalas correctas antes de animar
        currentImage.rectTransform.localScale = targetScale;
        nextImage.rectTransform.localScale = targetScale;

        // Preparar la siguiente imagen
        nextImage.rectTransform.localPosition = inPos;
        nextImage.color = new Color(1, 1, 1, 0);
        nextImage.sprite = miniGames[nextIndex].image;

        // Crear secuencia de animación
        Sequence seq = DOTween.Sequence();
        seq.Join(currentImage.rectTransform.DOLocalMove(outPos, animDuration).SetEase(Ease.OutQuad));
        seq.Join(currentImage.DOFade(0, animDuration));
        seq.Join(nextImage.rectTransform.DOLocalMove(startPos, animDuration).SetEase(Ease.OutQuad));
        seq.Join(nextImage.DOFade(1, animDuration));

        seq.OnComplete(() =>
        {
            // Intercambiar imágenes
            var temp = currentImage;
            currentImage = nextImage;
            nextImage = temp;
            currentIndex = nextIndex;
            UpdateTextUI(currentIndex);

            // Restaurar escalas y posiciones
            currentImage.rectTransform.localScale = targetScale;
            nextImage.rectTransform.localScale = targetScale;

            currentImage.rectTransform.localPosition = initialPos;
            nextImage.rectTransform.localPosition = initialPos;

            // Preparar la siguiente imagen invisible
            nextImage.color = new Color(1, 1, 1, 0);

            // Reactivar botones
            leftArrowButton.interactable = true;
            rightArrowButton.interactable = true;
            playButton.interactable = true;
        });
    }

    private void UpdateTextUI(int index)
    {
        var game = miniGames[index];
        nameText.text = game.name;
        descriptionText.text = game.description;
        int record = PlayerPrefs.GetInt(game.recordKey, 0);
        recordText.text = $"Récord Máximo: {record}";
    }

    public void OnPlayButton()
    {
        SceneManager.LoadScene(miniGames[currentIndex].sceneName);
    }
}
