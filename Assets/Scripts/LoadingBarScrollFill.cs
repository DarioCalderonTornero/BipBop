using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoadingBarScrollFill : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image fillImage;     // Image con Fill normal
    [SerializeField] private RawImage fillTexture; // RawImage que scrollea

    [Header("Scroll")]
    [SerializeField] private float scrollSpeed = 0.6f;

    private float u;

    private void Awake()
    {
        if (fillImage == null) Debug.LogError("Falta fillImage");
        if (fillTexture == null) Debug.LogError("Falta fillTexture");
    }

    private void Start()
    {
        // Asegurar configuración correcta
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;

        SetFill01(0f); // empieza vacía
    }

    private void Update()
    {
        // Scroll horizontal de la textura
        if (fillTexture != null)
        {
            u += scrollSpeed * Time.unscaledDeltaTime;
            var r = fillTexture.uvRect;
            r.x = u;
            fillTexture.uvRect = r;
        }
    }

    public void SetFill01(float value)
    {
        if (fillImage == null) return;
        fillImage.fillAmount = Mathf.Clamp01(value);
    }
}
