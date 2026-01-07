using UnityEngine;

public class GemHighlight : MonoBehaviour
{
    [SerializeField] private Color colorA = Color.white;
    [SerializeField] private Color colorB = Color.gray;
    [SerializeField] private float cycleDuration = 1.5f; // segundos ida y vuelta

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if (sr == null) return;

        // t va 0 1 0 de forma continua
        float t = Mathf.PingPong(Time.time / cycleDuration, 1f);
        sr.color = Color.Lerp(colorA, colorB, t);
    }
}
