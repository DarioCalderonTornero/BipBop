using System;
using UnityEngine;
using UnityEngine.UI;

public class DifferentTile : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image image;

    private RectTransform rt;
    private int index;
    private Action<int> onClick;

    // BASE desde el prefab (lo que tenga el prefab en local)
    private Vector3 baseScale;
    private Quaternion baseRotation;

    private void Awake()
    {
        rt = transform as RectTransform;

        if (button == null) button = GetComponent<Button>();
        if (image == null) image = GetComponent<Image>();

        // Guardamos “base” tal cual está en el prefab
        if (rt != null)
        {
            baseScale = rt.localScale;
            baseRotation = rt.localRotation;
        }
        else
        {
            baseScale = Vector3.one;
            baseRotation = Quaternion.identity;
        }
    }

    public void Bind(int tileIndex, Action<int> clickCallback)
    {
        index = tileIndex;
        onClick = clickCallback;

        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke(index));
    }

    public Vector3 GetBaseScale() => baseScale;
    public Quaternion GetBaseRotation() => baseRotation;

    public void ApplyBase(Sprite sprite, Color color)
    {
        SetSprite(sprite);
        SetColor(color);
        SetRotation(baseRotation);
        SetScale(baseScale);
    }

    public void SetSprite(Sprite sprite)
    {
        if (image == null) return;
        image.sprite = sprite;
    }

    public void SetColor(Color color)
    {
        if (image == null) return;
        image.color = color;
    }

    public void SetRotation(Quaternion rot)
    {
        if (rt == null) return;
        rt.localRotation = rot;
    }

    public void SetRotationZ(float zRotationDeg)
    {
        if (rt == null) return;
        rt.localRotation = Quaternion.Euler(0f, 0f, zRotationDeg);
    }

    public void SetScale(Vector3 scale)
    {
        if (rt == null) return;
        rt.localScale = scale;
    }
}
