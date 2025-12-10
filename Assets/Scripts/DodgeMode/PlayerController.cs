using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;   // 👈 IMPORTANTE para detectar UI

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
        // Solo mover si el estado es Playing
        if (DodgeState.Instance.dodgeGameState != DodgeState.DodgeGameStateEnum.Playing)
            return;

#if UNITY_EDITOR || UNITY_STANDALONE
        // PC - seguir mouse
        if (Input.GetMouseButton(0))
        {
            // 👇 Si el ratón está sobre UI, NO cambiamos el target
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                // Pero seguimos moviendo hacia el último targetPosition
            }
            else
            {
                Vector3 mousePos = Input.mousePosition;
                mousePos.z = 10f; // distancia de cámara
                targetPosition = Camera.main.ScreenToWorldPoint(mousePos);
            }
        }
#else
        // Móvil - seguir dedo
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // 👇 Comprobamos si ese dedo está sobre UI
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                // Está tocando UI → ignoramos este toque para movimiento
            }
            else
            {
                Vector3 touchPos = touch.position;
                touchPos.z = 10f;
                targetPosition = Camera.main.ScreenToWorldPoint(touchPos);
            }
        }
#endif

        // Movimiento suave hacia el último target "válido"
        transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }
}
