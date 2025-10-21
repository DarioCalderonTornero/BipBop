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
    public float screenMargin = 0.08f; // margen en viewport (0 = borde total, 0.1 = más separado)

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
                    // Izquierda o derecha
                    edgeViewportPos.x = dir.x > 0 ? 1f - screenMargin : screenMargin;
                    edgeViewportPos.y = Mathf.Clamp(viewportPos.y, screenMargin, 1f - screenMargin);
                }
                else
                {
                    // Arriba o abajo
                    edgeViewportPos.y = dir.y > 0 ? 1f - screenMargin : screenMargin;
                    edgeViewportPos.x = Mathf.Clamp(viewportPos.x, screenMargin, 1f - screenMargin);
                }

                // Convertir a posición en canvas
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.transform as RectTransform,
                    mainCamera.ViewportToScreenPoint(edgeViewportPos),
                    canvas.worldCamera,
                    out Vector2 canvasPos);

                indicator.localPosition = canvasPos;
                indicator.localRotation = Quaternion.identity;
            }
        }

        foreach (var e in toRemove)
            activeIndicators.Remove(e);
    }

    public void RegisterEnemy(Transform enemy)
    {
        if (!activeIndicators.ContainsKey(enemy))
        {
            RectTransform newIndicator = Instantiate(indicatorPrefab, canvas.transform);
            activeIndicators.Add(enemy, newIndicator);
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
