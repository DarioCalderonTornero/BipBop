using UnityEngine;
using System;

public class ClassifierInput : MonoBehaviour
{
    [Header("Configuración de Input")]
    public float minSwipeDistance = 50f;

    // Evento que envía la dirección detectada a quien le interese escuchar
    public Action<SwipeDirection> OnSwipeDetected;

    // Lo controlará el Manager para evitar inputs durante animaciones
    public bool isInputActive = false;

    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;

    void Update()
    {
        if (!isInputActive) return;

        // Ratón
        if (Input.GetMouseButtonDown(0))
        {
            startTouchPosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            endTouchPosition = Input.mousePosition;
            ProcessSwipe();
        }

        // Táctil
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began) startTouchPosition = touch.position;
            else if (touch.phase == TouchPhase.Ended)
            {
                endTouchPosition = touch.position;
                ProcessSwipe();
            }
        }
    }

    private void ProcessSwipe()
    {
        float swipeDistance = Vector2.Distance(startTouchPosition, endTouchPosition);

        if (swipeDistance >= minSwipeDistance)
        {
            Vector2 swipeDirection = endTouchPosition - startTouchPosition;
            swipeDirection.Normalize();

            SwipeDirection finalDirection = GetSwipeDirection(swipeDirection);

            // Disparamos el evento pasando la dirección
            OnSwipeDetected?.Invoke(finalDirection);
        }
    }

    private SwipeDirection GetSwipeDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            return direction.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
        }
        else
        {
            return direction.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
        }
    }
}