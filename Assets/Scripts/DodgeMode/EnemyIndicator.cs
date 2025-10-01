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

            if (enemy == null) // enemigo destruido
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
                // Clamp dentro de pantalla
                viewportPos.x = Mathf.Clamp01(viewportPos.x);
                viewportPos.y = Mathf.Clamp01(viewportPos.y);

                Vector2 canvasPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.transform as RectTransform,
                    mainCamera.ViewportToScreenPoint(viewportPos),
                    canvas.worldCamera,
                    out canvasPos);

                indicator.localPosition = canvasPos;

                // Rotar hacia el enemigo
                Vector3 dir = enemy.position - mainCamera.transform.position;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                indicator.rotation = Quaternion.Euler(0, 0, angle - 90f);
            }
        }

        // limpiar los destruidos
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
