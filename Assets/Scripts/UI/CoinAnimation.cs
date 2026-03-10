using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class CoinShineEffect : MonoBehaviour
{
    [Header("Shader")]
    [SerializeField] private string shinePropertyName = "_ShineLocation";

    [Header("Timing")]
    [SerializeField] private float intervalBetweenShines = 3f;
    [SerializeField] private float shineDuration = 0.8f;

    private Image targetImage;
    private Material materialInstance;

    private void Awake()
    {
        targetImage = GetComponent<Image>();

        // Instancia del material para no modificar el original
        materialInstance = Instantiate(targetImage.material);
        targetImage.material = materialInstance;

        materialInstance.SetFloat(shinePropertyName, 0f);
    }

    private void OnEnable()
    {
        StartCoroutine(ShineLoop());
    }

    private IEnumerator ShineLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(intervalBetweenShines);
            yield return StartCoroutine(PlayShine());
        }
    }

    private IEnumerator PlayShine()
    {
        float time = 0f;

        // 0 → 1
        while (time < shineDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / shineDuration);
            materialInstance.SetFloat(shinePropertyName, Mathf.Lerp(0f, 1f, t));
            yield return null;
        }

        time = 0f;

        // 1 → 0
        while (time < shineDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / shineDuration);
            materialInstance.SetFloat(shinePropertyName, Mathf.Lerp(1f, 0f, t));
            yield return null;
        }
    }

    private void OnDestroy()
    {
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }
}