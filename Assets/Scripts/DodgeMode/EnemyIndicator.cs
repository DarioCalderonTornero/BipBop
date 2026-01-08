using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyIndicator : MonoBehaviour
{
    public static EnemyIndicator Instance;

    [Header("References")]
    public Camera mainCamera;
    public Canvas canvas;
    public RectTransform indicatorPrefab;

    [Header("Settings")]
    [Range(0f, 0.5f)]
    public float screenMarginX = 0.08f;
    [Range(0f, 0.5f)]
    public float screenMarginY = 0.08f;

    private Dictionary<Transform, RectTransform> activeIndicators = new Dictionary<Transform, RectTransform>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        List<Transform> toRemove = new List<Transform>();

        foreach (var kvp in activeIndicators)
        {
            Transform enemy = kvp.Key;
            RectTransform indicator = kvp.Value;

            if (enemy == null)
            {
                Destroy(indicator.gameObject);
                toRemove.Add(enemy);
                continue;
            }

            UpdateSingleIndicator(enemy, indicator);
        }

        foreach (var e in toRemove)
            activeIndicators.Remove(e);
    }

    public void RegisterEnemy(Transform enemy)
    {
        if (activeIndicators.ContainsKey(enemy))
            return;

        RectTransform newIndicator = Instantiate(indicatorPrefab, canvas.transform);
        newIndicator.gameObject.SetActive(false); // no mostrarlo aún

        activeIndicators.Add(enemy, newIndicator);

        // Posicionar inmediatamente para evitar “parpadeo” en el centro
        UpdateSingleIndicator(enemy, newIndicator);
    }

    private void UpdateSingleIndicator(Transform enemy, RectTransform indicator)
    {
        if (enemy == null || indicator == null)
            return;

        Vector3 viewportPos = mainCamera.WorldToViewportPoint(enemy.position);

        bool isVisible = viewportPos.z > 0 &&
                         viewportPos.x > 0 && viewportPos.x < 1 &&
                         viewportPos.y > 0 && viewportPos.y < 1;

        indicator.gameObject.SetActive(!isVisible);

        if (!isVisible)
        {
            Vector3 dir = viewportPos - new Vector3(0.5f, 0.5f, 0);
            dir.z = 0;

            float absX = Mathf.Abs(dir.x);
            float absY = Mathf.Abs(dir.y);

            Vector2 edgeViewportPos = new Vector2(0.5f, 0.5f);

            if (absX > absY)
            {
                edgeViewportPos.x = dir.x > 0 ? 1f - screenMarginX : screenMarginX;
                edgeViewportPos.y = Mathf.Clamp(viewportPos.y, screenMarginY, 1f - screenMarginY);
            }
            else
            {
                edgeViewportPos.y = dir.y > 0 ? 1f - screenMarginY : screenMarginY;
                edgeViewportPos.x = Mathf.Clamp(viewportPos.x, screenMarginX, 1f - screenMarginX);
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                mainCamera.ViewportToScreenPoint(edgeViewportPos),
                canvas.worldCamera,
                out Vector2 canvasPos);

            indicator.localPosition = canvasPos;
            indicator.localRotation = Quaternion.identity;
        }
    }

    public void UnregisterEnemy(Transform enemy)
    {
        if (activeIndicators.ContainsKey(enemy))
        {
            Destroy(activeIndicators[enemy].gameObject);
            activeIndicators.Remove(enemy);
        }
    }
}
