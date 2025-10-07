using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerLevelUIAnimator : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    public RectTransform levelGroup;    // Grupo que contiene el icono (imágenes + número)
    public RectTransform xpPanel;       // Panel con el texto de XP
    public TextMeshProUGUI xpText;      // Texto dentro del panel

    [Header("Animation Settings")]
    public float buttonAnimDuration = 0.2f;
    public float panelAnimDuration = 0.15f;
    public Vector3 shrinkScale = new Vector3(0.85f, 0.85f, 1f);

    private bool isPanelVisible = false;
    private bool isAnimating = false;

    private void Start()
    {
        if (xpPanel != null)
            xpPanel.gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isAnimating) return; //  Evita pulsar durante animaciones
        ToggleXPPanel();
    }

    public void ToggleXPPanel()
    {
        StartCoroutine(AnimateLevelIcon());
        isPanelVisible = !isPanelVisible;
        StartCoroutine(AnimateXPPanel(isPanelVisible));
    }

    private IEnumerator AnimateLevelIcon()
    {
        isAnimating = true;

        Vector3 originalScale = levelGroup.localScale;

        // Escala hacia abajo
        float elapsed = 0f;
        while (elapsed < buttonAnimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / buttonAnimDuration;
            levelGroup.localScale = Vector3.Lerp(originalScale, shrinkScale, t);
            yield return null;
        }

        // Escala hacia arriba
        elapsed = 0f;
        while (elapsed < buttonAnimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / buttonAnimDuration;
            levelGroup.localScale = Vector3.Lerp(shrinkScale, originalScale, t);
            yield return null;
        }

        isAnimating = false;
    }

    private IEnumerator AnimateXPPanel(bool show)
    {
        if (xpPanel == null) yield break;

        if (show)
        {
            xpPanel.gameObject.SetActive(true);
            xpPanel.localScale = Vector3.zero;
            xpText.text = $"{PlayerLevelManager.Instance.currentXP} / {PlayerLevelManager.Instance.xpToNextLevel} XP";

            float elapsed = 0f;
            while (elapsed < panelAnimDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / panelAnimDuration;
                xpPanel.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
                yield return null;
            }
        }
        else
        {
            float elapsed = 0f;
            Vector3 startScale = xpPanel.localScale;

            while (elapsed < panelAnimDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / panelAnimDuration;
                xpPanel.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }

            xpPanel.gameObject.SetActive(false);
        }
    }
}
