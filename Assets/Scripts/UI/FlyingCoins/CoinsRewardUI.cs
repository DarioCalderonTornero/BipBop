using System.Collections;
using TMPro;
using UnityEngine;

public class CoinsRewardUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform coinSpawnCenter;
    [SerializeField] private RectTransform targetIcon;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private TextMeshProUGUI totalCoinsText;

    [Header("Animation Settings")]
    [SerializeField] private float scatterRadius = 200f;
    [SerializeField] private float fadeDuration = 0.12f;
    [SerializeField] private float shortDelay = 0.15f;

    [Header("Performance (opcional)")]
    [Tooltip("Máximo de monedas visuales instanciadas. El contador sumará el total, pero repartido por llegada.")]
    [SerializeField] private int maxVisualCoins = 30;

    [Header("Burst (impulso inicial)")]
    [SerializeField] private float burstForceMin = 850f;
    [SerializeField] private float burstForceMax = 1450f;

    [Tooltip("Tiempo de 'salir disparada' antes de ir al icono (más alto = más lento)")]
    [SerializeField] private float burstDurationMin = 0.10f;
    [SerializeField] private float burstDurationMax = 0.18f;

    [Header("Homing (ir al panel)")]
    [Tooltip("Tiempo de ir hasta el icono (más alto = más lento)")]
    [SerializeField] private float homingDurationMin = 0.25f;
    [SerializeField] private float homingDurationMax = 0.38f;

    [Header("Dirección del disparo")]
    [Tooltip("0 = igual en todas direcciones. 1 = casi todo lateral (menos vertical)")]
    [SerializeField, Range(0f, 1f)] private float lateralBias = 0.65f;

    private int coinsEarned;
    private int coinsArrived;
    private int totalBefore;

    private int visualCount;
    private int coinsCountedSoFar; // cuántas monedas ya hemos sumado en UI (y en Currency)
    private int basePerCoin;
    private int remainder;

    private Vector3 originalIconScale;
    private Coroutine popCoroutine;

    private void Start()
    {
        if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0;
        if (targetIcon != null) originalIconScale = targetIcon.localScale;
    }

    public void ShowReward(int coins)
    {
        if (coins <= 0)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        coinsEarned = coins;
        coinsArrived = 0;
        coinsCountedSoFar = 0;

        totalBefore = CurrencyManager.Instance.GetCoins();

        if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0;
        if (totalCoinsText != null) totalCoinsText.text = totalBefore.ToString();

        StopAllCoroutines();
        StartCoroutine(PlaySequenceFast());
    }

    private IEnumerator PlaySequenceFast()
    {
        // Fade in rápido
        if (panelCanvasGroup != null && fadeDuration > 0f)
            yield return StartCoroutine(FadeCanvas(panelCanvasGroup, 0, 1, fadeDuration));
        else if (panelCanvasGroup != null)
            panelCanvasGroup.alpha = 1f;

        yield return new WaitForSecondsRealtime(shortDelay);

        // Spawn todas a la vez
        visualCount = Mathf.Clamp(coinsEarned, 1, Mathf.Max(1, maxVisualCoins));

        // Reparto de monedas reales entre monedas visuales:
        // Ej: 53 monedas, 30 visuales => base=1, remainder=23
        // Las primeras 23 suman 2, el resto suman 1.
        basePerCoin = coinsEarned / visualCount;
        remainder = coinsEarned % visualCount;

        for (int i = 0; i < visualCount; i++)
        {
            GameObject coin = Instantiate(coinPrefab, coinSpawnCenter.parent);

            // Dispersión visual (posición inicial alrededor)
            float angle = (i / (float)visualCount) * Mathf.PI * 2f;
            float r = Random.Range(scatterRadius * 0.35f, scatterRadius);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
            offset += Random.insideUnitCircle * 12f;

            // Dirección de impulso (más lateral)
            Vector2 dir = Random.insideUnitCircle;
            dir.y *= (1f - lateralBias);
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
            dir.Normalize();

            float force = Random.Range(burstForceMin, burstForceMax);
            float burstTime = Random.Range(burstDurationMin, burstDurationMax);
            float homingTime = Random.Range(homingDurationMin, homingDurationMax);

            FlyingCoin fc = coin.GetComponent<FlyingCoin>();
            fc.Initialize(
                coinSpawnCenter.position,      // todas nacen del centro
                targetIcon.position,
                dir,
                force,
                burstTime,
                homingTime,
                OnCoinArrivedVisual
            );
        }

        // Esperar a que lleguen todas las visuales
        while (coinsArrived < visualCount)
            yield return null;

        yield return new WaitForSecondsRealtime(shortDelay);

        // Fade out rápido
        if (panelCanvasGroup != null && fadeDuration > 0f)
            yield return StartCoroutine(FadeCanvas(panelCanvasGroup, 1, 0, fadeDuration));
        else if (panelCanvasGroup != null)
            panelCanvasGroup.alpha = 0f;

        gameObject.SetActive(false);
    }

    private IEnumerator FadeCanvas(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;

        if (duration <= 0f)
        {
            cg.alpha = to;
            yield break;
        }

        float t = 0f;
        cg.alpha = from;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        cg.alpha = to;
    }

    private void OnCoinArrivedVisual()
    {
        coinsArrived++;

        // Cuánto suma ESTA moneda visual
        int add = basePerCoin + ((coinsArrived <= remainder) ? 1 : 0);
        coinsCountedSoFar += add;

        // Actualizar texto conforme llegan
        if (totalCoinsText != null)
            totalCoinsText.text = (totalBefore + coinsCountedSoFar).ToString();

        // Actualizar monedas reales conforme llegan
        CurrencyManager.Instance.AddCoins(add);

        // POP del icono
        if (popCoroutine != null)
        {
            StopCoroutine(popCoroutine);
            if (targetIcon != null) targetIcon.localScale = originalIconScale;
        }
        popCoroutine = StartCoroutine(PopIconOnce());
    }

    private IEnumerator PopIconOnce()
    {
        if (targetIcon == null) yield break;

        float popTime = 0.05f;
        Vector3 targetScale = originalIconScale * 1.2f;

        float t = 0f;
        while (t < popTime)
        {
            t += Time.unscaledDeltaTime;
            targetIcon.localScale = Vector3.Lerp(originalIconScale, targetScale, t / popTime);
            yield return null;
        }

        t = 0f;
        while (t < popTime)
        {
            t += Time.unscaledDeltaTime;
            targetIcon.localScale = Vector3.Lerp(targetScale, originalIconScale, t / popTime);
            yield return null;
        }

        targetIcon.localScale = originalIconScale;
        popCoroutine = null;
    }
}
