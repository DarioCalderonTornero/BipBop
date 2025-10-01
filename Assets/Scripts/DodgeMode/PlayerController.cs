using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Vector3 targetPosition;

    private void Start()
    {
        targetPosition = transform.position;
    }

    private void Update()
    {
#if UNITY_EDITOR
        // PC - seguir mouse
        if (Input.GetMouseButton(0))
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 10f; // distancia de cámara
            targetPosition = Camera.main.ScreenToWorldPoint(mousePos);
        }
#else
        // Móvil - seguir dedo
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 touchPos = touch.position;
            touchPos.z = 10f;
            targetPosition = Camera.main.ScreenToWorldPoint(touchPos);
        }
#endif

        // Movimiento suave
        transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }
}
