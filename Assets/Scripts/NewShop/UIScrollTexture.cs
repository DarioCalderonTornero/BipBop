using UnityEngine;
using UnityEngine.UI;

public class UIScrollTexture : MonoBehaviour
{
    [SerializeField] private Vector2 speed = new Vector2(0.01f, 0.01f);
    [SerializeField] private string textureProperty = "_MainTex";

    private Material runtimeMat;
    private int propId;
    private Vector2 offset;

    void Awake()
    {
        var img = GetComponent<Image>();
        runtimeMat = Instantiate(img.material);   // IMPORTANTE: instancia para no afectar a otras UI
        img.material = runtimeMat;

        propId = Shader.PropertyToID(textureProperty);
    }

    void Update()
    {
        offset += speed * Time.unscaledDeltaTime; // UI suele ir mejor sin Time.timeScale
        runtimeMat.SetTextureOffset(propId, offset);
    }
}
