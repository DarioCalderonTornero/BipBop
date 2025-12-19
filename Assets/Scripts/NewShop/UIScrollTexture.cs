using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class UIScrollTexture : MonoBehaviour
{
    [SerializeField] private Vector2 speed = new Vector2(0.01f, 0.01f);

    private RawImage raw;
    private Material runtimeMat;
    private Vector2 offset;

    void Awake()
    {
        raw = GetComponent<RawImage>();

        runtimeMat = Instantiate(raw.material);
        raw.material = runtimeMat;
    }

    void Update()
    {
        offset += speed * Time.unscaledDeltaTime;

        // LOOP perfecto (evita offsets gigantes y glitches)
        offset.x = Mathf.Repeat(offset.x, 1f);
        offset.y = Mathf.Repeat(offset.y, 1f);

        runtimeMat.mainTextureOffset = offset;
    }
}
