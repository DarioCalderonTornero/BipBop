using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class AvatarImageBinder : MonoBehaviour
{
    [SerializeField] private Image targetImage;

    private Material matInstance;
    private IAvatarFxRuntime fxRuntime;

    private AvatarDataSO lastData;
    private bool hasAppliedOnce;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        // ✅ Si este GO se desactiva/activa (ej: botón user),
        // re-aplicamos el avatar para recrear material/runtime
        if (hasAppliedOnce && lastData != null)
            ApplyAvatar(lastData);
    }

    public void ApplyAvatar(AvatarDataSO data)
    {
        lastData = data;
        hasAppliedOnce = true;

        CleanupRuntimeOnly();

        if (data == null || targetImage == null)
            return;

        targetImage.sprite = data.sprite;

        // Sin shader
        if (!data.hasShaderEffect || data.effectMaterial == null)
        {
            CleanupMaterial();
            targetImage.material = null;
            return;
        }

        // ✅ material instance por Image
        CleanupMaterial();
        matInstance = Instantiate(data.effectMaterial);
        targetImage.material = matInstance;

        // ✅ FX opcional (animación)
        if (data.fxPreset != null)
        {
            fxRuntime = data.fxPreset.CreateRuntime();
            fxRuntime.Init(matInstance);
        }
    }

    public void Clear(Sprite fallback = null)
    {
        CleanupAll();

        if (targetImage != null)
        {
            targetImage.sprite = fallback;
            targetImage.material = null;
        }

        lastData = null;
        hasAppliedOnce = false;
    }

    private void Update()
    {
        if (fxRuntime != null)
            fxRuntime.Tick(Time.unscaledDeltaTime);
    }

    private void OnDisable()
    {
        // ✅ NO destruir aquí.
        // Si destruyes, al reactivar se queda "normal" porque nadie re-aplica.
        // Aquí como mucho podrías pausar, pero nuestro runtime no tiene pause.
    }

    private void OnDestroy()
    {
        CleanupAll();
    }

    private void CleanupRuntimeOnly()
    {
        if (fxRuntime != null)
        {
            fxRuntime.Dispose();
            fxRuntime = null;
        }
    }

    private void CleanupMaterial()
    {
        if (matInstance != null)
        {
            Destroy(matInstance);
            matInstance = null;
        }
    }

    private void CleanupAll()
    {
        CleanupRuntimeOnly();
        CleanupMaterial();

        if (targetImage != null)
            targetImage.material = null;
    }
}
