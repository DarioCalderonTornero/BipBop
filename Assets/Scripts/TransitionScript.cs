using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class TransitionScript : MonoBehaviour
{
    public static TransitionScript Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private RectTransform panel;       // El panel que ocupa toda la pantalla
    [SerializeField] private TextMeshProUGUI modeLabel; // Texto con el nombre del modo
    [SerializeField] private float duration = 0.5f;     // Duración de subida/bajada

    public event Action OnTransitionOutFinished;        // Para avisar al minijuego

    private bool isTransitioning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Aseguramos que el panel existe
        if (panel == null)
            panel = GetComponent<RectTransform>();

        // Empezamos con el panel fuera de pantalla (abajo)
        Vector2 offPos = new Vector2(0, -Screen.height);
        panel.anchoredPosition = offPos;
    }

    /// <summary>
    /// Llamar desde el botón de jugar.
    /// </summary>
    public void TransitionToScene(string sceneName, string modeName)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(sceneName, modeName));
    }

    private IEnumerator TransitionRoutine(string sceneName, string modeName)
    {
        isTransitioning = true;

        if (modeLabel != null)
            modeLabel.text = modeName;

        Vector2 offPos = new Vector2(0, -Screen.height);
        Vector2 onPos = Vector2.zero;

        // Arranca abajo
        panel.anchoredPosition = offPos;

        // 1) Subir panel
        yield return panel.DOAnchorPos(onPos, duration)
                          .SetEase(Ease.OutCubic)
                          .WaitForCompletion();

        // 2) Cargar escena
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
            yield return null;

        // 3) Forzar panel en 0 POR SEGURIDAD
        panel.anchoredPosition = onPos;

        // 4) Frame extra para permitir que la escena configure todo
        yield return null;

        // 5) Bajar panel
        yield return panel.DOAnchorPos(offPos, duration)
                          .SetEase(Ease.InCubic)
                          .WaitForCompletion();

        // 6) Frame extra DESPUÉS de salir
        yield return null;

        isTransitioning = false;

        // 7) Ahora sí se notifica: TODO listo, panel ya fuera
        OnTransitionOutFinished?.Invoke();
    }
}
