using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ExitConfirmPopup : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject root;      // ExitConfirmRoot
    [SerializeField] private RectTransform panel;  // Panel (el que hace pop)
    [SerializeField] private Button exitButton;
    [SerializeField] private Button cancelButton;

    [Header("Pop")]
    [SerializeField] private float popUpScale = 1.06f;
    [SerializeField] private float popUpDuration = 0.08f;
    [SerializeField] private float popDownDuration = 0.10f;

    private bool isOpen;
    private Coroutine popRoutine;

    private void Awake()
    {
        if (root != null) root.SetActive(false);

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnExit);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(Hide);
        }
    }

    private void Update()
    {
        // Android back / Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isOpen) Show();
            else Hide();
        }
    }

    public void Show()
    {
        if (root == null || panel == null) return;
        if (isOpen) return;

        isOpen = true;
        root.SetActive(true);

        // pop relativo a escala real
        if (popRoutine != null) StopCoroutine(popRoutine);
        popRoutine = StartCoroutine(Pop(panel));
    }

    public void Hide()
    {
        if (root == null) return;

        isOpen = false;
        root.SetActive(false);
    }

    private void OnExit()
    {
        // Cierra app (Android/iOS). En editor no hace nada útil.
        Application.Quit();
    }

    private IEnumerator Pop(RectTransform rt)
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
}
