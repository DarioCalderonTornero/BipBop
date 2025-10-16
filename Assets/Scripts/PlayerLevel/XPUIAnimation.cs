using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XPUIAnimation : MonoBehaviour
{
    [Header("Panel Deslizante")]
    public RectTransform panel;
    public Button toggleButton;
    public Vector2 hiddenPosition = new Vector2(0, 600);
    public Vector2 shownPosition = new Vector2(0, 0);
    public float slideSpeed = 18f;

    [Header("Botón Pop")]
    public float buttonPopScale = 0.85f;
    public float buttonPopSpeed = 30f;

    [Header("XP UI")]
    public Image xpFillImage;             // Barra de XP
    public TextMeshProUGUI xpText;        // Texto XP
    public TextMeshProUGUI levelText;     // Texto nivel
    public Image xpBackgroundImage;       // Imagen de fondo opcional (decorativa)

    private bool isShown = false;
    private bool isMoving = false;

    void Start()
    {
        panel.anchoredPosition = hiddenPosition;
        toggleButton.onClick.AddListener(OnButtonClick);
        UpdateXPUI();
    }

    void Update()
    {
        // Actualizar UI cada frame por si la XP cambia dinámicamente
        UpdateXPUI();
    }

    void OnButtonClick()
    {
        if (!isMoving)
        {
            StartCoroutine(ButtonPopAnimation());
            TogglePanel();
        }
    }

    private void TogglePanel()
    {
        if (!isMoving)
            StartCoroutine(MovePanel(isShown ? hiddenPosition : shownPosition));

        isShown = !isShown;
    }

    private System.Collections.IEnumerator MovePanel(Vector2 target)
    {
        isMoving = true;

        while (Vector2.Distance(panel.anchoredPosition, target) > 0.05f)
        {
            panel.anchoredPosition = Vector2.Lerp(panel.anchoredPosition, target, Time.deltaTime * slideSpeed);
            yield return null;
        }

        panel.anchoredPosition = target;
        isMoving = false;
    }

    private System.Collections.IEnumerator ButtonPopAnimation()
    {
        Vector3 originalScale = toggleButton.transform.localScale;
        Vector3 targetScale = originalScale * buttonPopScale;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * buttonPopSpeed;
            toggleButton.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * buttonPopSpeed;
            toggleButton.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }
    }

    private void UpdateXPUI()
    {
        if (!PlayerLevelManager.Instance) return;

        if (levelText) levelText.text = PlayerLevelManager.Instance.currentLevel.ToString();
        if (xpText) xpText.text = $"{PlayerLevelManager.Instance.currentXP} / {PlayerLevelManager.Instance.xpToNextLevel}";
        if (xpFillImage) xpFillImage.fillAmount = (float)PlayerLevelManager.Instance.currentXP / PlayerLevelManager.Instance.xpToNextLevel;
    }
}
