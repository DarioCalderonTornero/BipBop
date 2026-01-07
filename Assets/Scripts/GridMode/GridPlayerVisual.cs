using UnityEngine;

public class GridPlayerVisual : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite onCellSprite;
    [SerializeField] private Sprite inAirSprite;

    [Header("Auto")]
    [SerializeField] private SpriteRenderer sr;

    private void Awake()
    {
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>(true);
        SetOnCell(); // estado inicial
    }

    public void SetOnCell()
    {
        if (sr == null || onCellSprite == null) return;
        sr.sprite = onCellSprite;
    }

    public void SetInAir()
    {
        if (sr == null || inAirSprite == null) return;
        sr.sprite = inAirSprite;
    }

    // Si quieres debug fácil:
    public void LogState(string msg)
    {
        Debug.Log($"[GridPlayerVisual] {msg} | sr={(sr ? sr.name : "NULL")} onCell={(onCellSprite != null)} inAir={(inAirSprite != null)}", this);
    }
}
