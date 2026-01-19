using UnityEngine;

public class PlayArea2D : MonoBehaviour
{
    public enum Shape { Box, Circle }

    [Header("Area")]
    [SerializeField] private Shape shape = Shape.Box;
    [SerializeField] private BoxCollider2D box;
    [SerializeField] private CircleCollider2D circle;

    private void Reset()
    {
        box = GetComponent<BoxCollider2D>();
        circle = GetComponent<CircleCollider2D>();
    }

    private void Awake()
    {
        if (box == null) box = GetComponent<BoxCollider2D>();
        if (circle == null) circle = GetComponent<CircleCollider2D>();

        // Auto-detect
        if (circle != null) shape = Shape.Circle;
        else if (box != null) shape = Shape.Box;
    }

    public Vector2 ClosestPoint(Vector2 worldPos)
    {
        switch (shape)
        {
            case Shape.Circle:
                return ClosestPointCircle(worldPos);
            default:
                return box != null ? box.ClosestPoint(worldPos) : worldPos;
        }
    }

    public bool Contains(Vector2 worldPos)
    {
        Vector2 closest = ClosestPoint(worldPos);
        // Si el punto está dentro, closestPoint == worldPos (o muy cerca)
        return (closest - worldPos).sqrMagnitude < 0.00001f;
    }

    private Vector2 ClosestPointCircle(Vector2 worldPos)
    {
        if (circle == null) return worldPos;

        Vector2 center = (Vector2)circle.transform.TransformPoint(circle.offset);
        float radius = circle.radius * MaxAbsScale(circle.transform);

        Vector2 dir = worldPos - center;
        float dist = dir.magnitude;

        if (dist <= radius) return worldPos; // dentro

        if (dist < 0.0001f) return center + Vector2.right * radius;

        return center + dir / dist * radius;
    }

    private float MaxAbsScale(Transform t)
    {
        Vector3 s = t.lossyScale;
        return Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
    }
}
