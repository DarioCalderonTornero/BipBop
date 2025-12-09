using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AvatarUnlockPopup : MonoBehaviour
{
    public static AvatarUnlockPopup Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private AvatarCatalogSO avatarCatalog;

    [Header("Animación tiempos")]
    [SerializeField] private float enterDownDuration = 0.18f;   // caída hasta un poco por debajo
    [SerializeField] private float enterUpDuration = 0.14f;     // rebote hasta posición final
    [SerializeField] private float visibleTime = 1.3f;          // tiempo visible arriba
    [SerializeField] private float exitPreDownDuration = 0.12f; // pequeña bajada antes de subir
    [SerializeField] private float exitUpDuration = 0.18f;      // subida final

    [Header("Animación distancias")]
    [SerializeField] private float enterBounceOffset = 40f;     // cuánto se pasa hacia abajo al caer
    [SerializeField] private float exitPreDownOffset = 25f;     // cuánto baja antes de subir

    [Header("Posiciones")]
    [SerializeField] private Vector2 shownPosition;             // posición final arriba
    [SerializeField] private Vector2 hiddenPosition = new Vector2(0, 350f); // fuera de pantalla por arriba

    [Header("Debug")]
    [SerializeField] private bool debugPlayAnimation = false;   // marcar en inspector para probar

    private Coroutine routine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (panel == null)
            panel = GetComponent<RectTransform>();

        // Usamos la posición en el editor como "shown"
        shownPosition = panel.anchoredPosition;

        // Empezamos ocultos arriba
        panel.anchoredPosition = hiddenPosition;
        panel.localScale = Vector3.one;
        panel.gameObject.SetActive(false);
    }

    private void Update()
    {
        // 🔹 Test desde el inspector
        if (debugPlayAnimation)
        {
            debugPlayAnimation = false;
            ShowByAvatarId("NormalAvatar"); // Cambia este ID si quieres otro por defecto
        }
    }

    /// <summary>
    /// Muestra el popup para un avatar concreto.
    /// </summary>
    public void ShowByAvatarId(string avatarId)
    {
        if (avatarCatalog == null)
        {
            Debug.LogWarning("[AvatarUnlockPopup] No hay avatarCatalog asignado.");
            return;
        }

        AvatarDataSO data = avatarCatalog.avatarDataSO.Find(a => a != null && a.id == avatarId);

        if (data == null)
        {
            Debug.LogWarning("[AvatarUnlockPopup] Avatar no encontrado: " + avatarId);
            return;
        }

        if (avatarImage != null)
            avatarImage.sprite = data.sprite;

        if (messageText != null)
            messageText.text = $"¡{data.displayName} desbloqueado!";

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(PlayAnimation());
    }

    private IEnumerator PlayAnimation()
    {
        panel.gameObject.SetActive(true);

        // --- Estado inicial: arriba, sin escala rara ---
        panel.anchoredPosition = hiddenPosition;
        panel.localScale = Vector3.one;

        // =========================
        // 1️⃣ ENTRADA: CAER + REBOTE
        // =========================

        // 1.1 Caída hasta un poco por debajo de la posición final
        Vector2 fallTarget = shownPosition + new Vector2(0, -enterBounceOffset);
        float t = 0f;

        while (t < enterDownDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / enterDownDuration);

            // Aceleración hacia abajo (ease-in)
            float ease = p * p;

            panel.anchoredPosition = Vector2.Lerp(hiddenPosition, fallTarget, ease);
            yield return null;
        }

        // 1.2 Rebote hacia la posición final
        t = 0f;
        while (t < enterUpDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / enterUpDuration);

            // Ease-out hacia la posición final
            float ease = 1f - Mathf.Pow(1f - p, 2f);

            panel.anchoredPosition = Vector2.Lerp(fallTarget, shownPosition, ease);
            yield return null;
        }

        panel.anchoredPosition = shownPosition;

        // =========================
        // 2️⃣ TIEMPO VISIBLE
        // =========================
        yield return new WaitForSeconds(visibleTime);

        // =========================
        // 3️⃣ SALIDA: BAJA UN POCO
        // =========================
        Vector2 preDownPos = shownPosition + new Vector2(0, -exitPreDownOffset);
        t = 0f;

        while (t < exitPreDownDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / exitPreDownDuration);

            // Movimiento suave hacia abajo (coge carrerilla)
            float ease = 1f - Mathf.Cos(p * Mathf.PI * 0.5f); // ease-out suave

            panel.anchoredPosition = Vector2.Lerp(shownPosition, preDownPos, ease);
            yield return null;
        }

        // =========================
        // 4️⃣ SUBIDA FINAL
        // =========================
        t = 0f;
        while (t < exitUpDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / exitUpDuration);

            // Ease-in cuadrático hacia arriba
            float ease = p * p;

            panel.anchoredPosition = Vector2.Lerp(preDownPos, hiddenPosition, ease);
            yield return null;
        }

        panel.anchoredPosition = hiddenPosition;
        panel.gameObject.SetActive(false);
        routine = null;
    }

    // Helper estático para usar desde cualquier lado:
    public static void TryShow(string avatarId)
    {
        if (Instance != null)
            Instance.ShowByAvatarId(avatarId);
    }
}
