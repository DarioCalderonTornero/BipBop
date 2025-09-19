using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpriteTrail : MonoBehaviour
{
    [Header("Trail settings")]
    [SerializeField] private float spawnInterval = 0.05f;
    [SerializeField] private float ghostLife = 0.35f;
    [Tooltip("Multiplica color del sprite (RGB) y alfa. Usar (1,1,1,0.6) para mantener color pero 60% alpha)")]
    [SerializeField] private Color colorMultiplier = new Color(1f, 1f, 1f, 0.6f);

    [Header("Behavior")]
    [SerializeField] private bool useSpriteRendererInChildren = true; // busca SpriteRenderer en hijos si no está en el propio
    [SerializeField] private bool spawnWhileStationary = false; // si false, spawn solo si hay velocidad (mejora performance)

    private SpriteRenderer sr;
    private Image uiImage;
    private bool isUI = false;
    private Coroutine spawnCoroutine;
    private Vector3 lastPos;

    private void Awake()
    {
        // Intentar localizar SpriteRenderer
        sr = GetComponent<SpriteRenderer>();
        if (sr == null && useSpriteRendererInChildren)
            sr = GetComponentInChildren<SpriteRenderer>();

        // Si no hay SpriteRenderer, comprobar UI Image (Canvas)
        if (sr == null)
        {
            uiImage = GetComponent<Image>();
            if (uiImage == null)
                uiImage = GetComponentInChildren<Image>();
        }

        if (sr == null && uiImage == null)
        {
            Debug.LogWarning($"SpriteTrail: no encontré SpriteRenderer ni Image en '{name}'. Desactivando SpriteTrail.");
            enabled = false;
            return;
        }

        isUI = (sr == null && uiImage != null);
        lastPos = transform.position;
    }

    private void OnEnable()
    {
        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    private void OnDisable()
    {
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            // opcional: no spawnear si está parado
            if (spawnWhileStationary || Vector3.Distance(transform.position, lastPos) > 0.001f)
            {
                if (isUI)
                    SpawnUIGhost();
                else
                    SpawnSpriteGhost();
            }

            lastPos = transform.position;
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnSpriteGhost()
    {
        if (sr == null || sr.sprite == null) return;

        GameObject ghost = new GameObject(name + "_ghost");
        // dejarlo en la misma jerarquía para que tenga el mismo espacio
        ghost.transform.SetParent(sr.transform.parent, true);
        ghost.transform.position = sr.transform.position;
        ghost.transform.rotation = sr.transform.rotation;
        ghost.transform.localScale = sr.transform.localScale;

        SpriteRenderer gr = ghost.AddComponent<SpriteRenderer>();
        gr.sprite = sr.sprite;
        gr.sortingLayerID = sr.sortingLayerID;
        // poner ligeramente detrás en orden; si esto causa problemas, ajusta a sr.sortingOrder
        gr.sortingOrder = sr.sortingOrder - 1;

        // combinar colores (multiplicación simple)
        Color final = new Color(
            sr.color.r * colorMultiplier.r,
            sr.color.g * colorMultiplier.g,
            sr.color.b * colorMultiplier.b,
            sr.color.a * colorMultiplier.a
        );
        gr.color = final;

        // asegurar z correcto (evita que quede oculto por la cámara si usas Z)
        Vector3 pos = ghost.transform.position;
        pos.z = sr.transform.position.z;
        ghost.transform.position = pos;

        Destroy(ghost, ghostLife);
    }

    private void SpawnUIGhost()
    {
        if (uiImage == null || uiImage.sprite == null) return;

        GameObject ghost = new GameObject(name + "_ghostUI", typeof(RectTransform));
        ghost.transform.SetParent(uiImage.transform.parent, false);

        RectTransform src = uiImage.rectTransform;
        RectTransform rt = ghost.GetComponent<RectTransform>();
        rt.anchorMin = src.anchorMin;
        rt.anchorMax = src.anchorMax;
        rt.pivot = src.pivot;
        rt.anchoredPosition = src.anchoredPosition;
        rt.sizeDelta = src.sizeDelta;
        rt.localScale = src.localScale;
        rt.localRotation = src.localRotation;

        Image gi = ghost.AddComponent<Image>();
        gi.sprite = uiImage.sprite;
        gi.raycastTarget = false;

        Color final = new Color(
            uiImage.color.r * colorMultiplier.r,
            uiImage.color.g * colorMultiplier.g,
            uiImage.color.b * colorMultiplier.b,
            uiImage.color.a * colorMultiplier.a
        );
        gi.color = final;

        Destroy(ghost, ghostLife);
    }

    // Opcional: método público para forzar spawn instantáneo (si lo necesitas)
    public void SpawnOnceNow()
    {
        if (isUI) SpawnUIGhost(); else SpawnSpriteGhost();
    }
}
