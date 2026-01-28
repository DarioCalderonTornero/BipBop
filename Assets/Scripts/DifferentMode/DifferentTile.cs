using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DifferentTile : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image image;

    // IMPORTANTE: transformaremos SOLO el rect del sprite
    private RectTransform imageRt;

    private int index;
    private Action<int> onClick;

    private Vector3 baseScale;
    private Quaternion baseRotation;

    private Coroutine animRoutine;

    public RectTransform GetImageRect() => image != null ? image.rectTransform : null;
    public Vector3 GetCurrentImageScale() => (image != null) ? image.rectTransform.localScale : Vector3.one;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (image == null) image = GetComponent<Image>();

        imageRt = (image != null) ? image.rectTransform : null;

        if (imageRt != null)
        {
            baseScale = imageRt.localScale;
            baseRotation = imageRt.localRotation;
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
        StopAnim();
        SetSprite(sprite);
        SetColor(color);

        // SOLO el sprite vuelve a base
        SetRotation(baseRotation);
        SetScale(baseScale);
    }

    public void StopAnim()
    {
        if (animRoutine != null)
        {
            StopCoroutine(animRoutine);
            animRoutine = null;
        }
    }

    public void AnimateToColor(Color target, float duration)
    {
        if (image == null) return;
        StopAnim();
        animRoutine = StartCoroutine(ColorRoutine(target, duration));
    }

    public void AnimateToScale(Vector3 target, float duration)
    {
        if (imageRt == null) return;
        StopAnim();
        animRoutine = StartCoroutine(ScaleRoutine(target, duration));
    }

    public void AnimateToRotationZ(float targetZDeg, float duration)
    {
        if (imageRt == null) return;
        StopAnim();
        animRoutine = StartCoroutine(RotationRoutine(targetZDeg, duration));
    }

    public void AnimateToRotation(Quaternion target, float duration)
    {
        if (imageRt == null) return;
        StopAnim();
        animRoutine = StartCoroutine(RotationQuatRoutine(target, duration));
    }

    public void AnimateToSprite(Sprite sprite, float duration)
    {
        if (image == null) return;
        StopAnim();
        animRoutine = StartCoroutine(SpriteFadeRoutine(sprite, duration));
    }

    private IEnumerator ColorRoutine(Color target, float duration)
    {
        Color start = image.color;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Smooth01(t / duration);
            image.color = Color.Lerp(start, target, u);
            yield return null;
        }

        image.color = target;
        animRoutine = null;
    }

    private IEnumerator ScaleRoutine(Vector3 target, float duration)
    {
        Vector3 start = imageRt.localScale;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Smooth01(t / duration);
            imageRt.localScale = Vector3.Lerp(start, target, u);
            yield return null;
        }

        imageRt.localScale = target;
        animRoutine = null;
    }

    private IEnumerator RotationRoutine(float targetZDeg, float duration)
    {
        float startZ = imageRt.localRotation.eulerAngles.z;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Smooth01(t / duration);
            float z = Mathf.LerpAngle(startZ, targetZDeg, u);
            imageRt.localRotation = Quaternion.Euler(0f, 0f, z);
            yield return null;
        }

        imageRt.localRotation = Quaternion.Euler(0f, 0f, targetZDeg);
        animRoutine = null;
    }

    private IEnumerator RotationQuatRoutine(Quaternion target, float duration)
    {
        Quaternion start = imageRt.localRotation;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Smooth01(t / duration);
            imageRt.localRotation = Quaternion.Slerp(start, target, u);
            yield return null;
        }

        imageRt.localRotation = target;
        animRoutine = null;
    }

    private IEnumerator SpriteFadeRoutine(Sprite newSprite, float duration)
    {
        float half = Mathf.Max(0.01f, duration * 0.5f);

        float t = 0f;
        Color c = image.color;
        float startA = c.a;

        // fade out
        while (t < half)
        {
            t += Time.deltaTime;
            float u = Smooth01(t / half);
            c.a = Mathf.Lerp(startA, 0f, u);
            image.color = c;
            yield return null;
        }

        // swap
        image.sprite = newSprite;

        // fade in
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float u = Smooth01(t / half);
            c.a = Mathf.Lerp(0f, startA, u);
            image.color = c;
            yield return null;
        }

        c.a = startA;
        image.color = c;
        animRoutine = null;
    }

    public void Pop(float duration = 0.10f, float scaleMul = 1.20f)
    {
        if (imageRt == null) return;
        StopAnim();
        animRoutine = StartCoroutine(PopRoutine(duration, scaleMul));
    }

    private IEnumerator PopRoutine(float duration, float scaleMul)
    {
        Vector3 baseS = GetBaseScale();
        Vector3 bigS = baseS * scaleMul;

        float half = Mathf.Max(0.01f, duration * 0.5f);

        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float u = Smooth01(t / half);
            imageRt.localScale = Vector3.Lerp(baseS, bigS, u);
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float u = Smooth01(t / half);
            imageRt.localScale = Vector3.Lerp(bigS, baseS, u);
            yield return null;
        }

        imageRt.localScale = baseS;
        animRoutine = null;
    }

    private float Smooth01(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    // Setters (afectan al sprite, no al tile)
    public void SetSprite(Sprite sprite) { if (image != null) image.sprite = sprite; }
    public void SetColor(Color color) { if (image != null) image.color = color; }
    public void SetRotation(Quaternion rot) { if (imageRt != null) imageRt.localRotation = rot; }
    public void SetRotationZ(float z) { if (imageRt != null) imageRt.localRotation = Quaternion.Euler(0f, 0f, z); }
    public void SetScale(Vector3 s) { if (imageRt != null) imageRt.localScale = s; }
}
