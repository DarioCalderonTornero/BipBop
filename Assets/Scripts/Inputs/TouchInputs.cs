using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchInputs : MonoBehaviour
{
    public static TouchInputs Instance { get; private set; }

    public event EventHandler OnOneTouch;
    public event EventHandler OnZoomIn;
    public event EventHandler OnZoomOut;
    public event EventHandler OnShake;
    public event EventHandler OnLookDown;
    public event EventHandler OnSwipeUp;
    public event EventHandler OnSwipeDown;
    public event EventHandler OnSwipeLeft;
    public event EventHandler OnSwipeRight;
    public event EventHandler OnRotateRight;
    public event EventHandler OnRotateLeft;

    private float initialPinchDistance;
    private bool isZooming = false;

    private Vector2 swipeStartPos;
    private bool isSwiping = false;

    private float previousZRotation = 0f; // Almacena el ángulo previo
    private const float rotationSensitivity = 10f; // Sensibilidad para rotación
    private bool hasRotatedRight = false;
    private bool hasRotatedLeft = false;

    private void Awake()
    {
        Instance = this;
        Input.gyro.enabled = true;
    }

    void Update()
    {
        // Detecta un toque o clic del mouse
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                // Detecta el inicio de un toque
                if (touch.phase == TouchPhase.Began)
                {
                    OnOneTouch?.Invoke(this, EventArgs.Empty);
                    LogicaJuego.Instance.OnTaskAction("¡Toca la pantalla!");

                    // Guarda la posición inicial del toque para detectar deslizamientos
                    swipeStartPos = touch.position;
                    isSwiping = true;
                }

                // Detecta el fin del toque para analizar el deslizamiento
                if (isSwiping && touch.phase == TouchPhase.Ended)
                {
                    Vector2 swipeEndPos = touch.position; // Posición final del toque
                    Vector2 swipeDelta = swipeEndPos - swipeStartPos; // Diferencia entre inicio y fin

                    // Si el movimiento es lo suficientemente grande, detecta la dirección
                    if (swipeDelta.magnitude > 100f) // Puedes ajustar este umbral
                    {
                        if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
                        {
                            // Movimiento horizontal
                            if (swipeDelta.x > 0)
                            {
                                OnSwipeRight?.Invoke(this, EventArgs.Empty);
                                LogicaJuego.Instance.OnTaskAction("¡Desliza hacia la derecha!");
                            }
                            else
                            {
                                OnSwipeLeft?.Invoke(this, EventArgs.Empty);
                                LogicaJuego.Instance.OnTaskAction("¡Desliza hacia la izquierda!");
                            }
                        }
                        else
                        {
                            // Movimiento vertical
                            if (swipeDelta.y > 0)
                            {
                                OnSwipeUp?.Invoke(this, EventArgs.Empty);
                                LogicaJuego.Instance.OnTaskAction("¡Desliza hacia arriba!");
                            }
                            else
                            {
                                OnSwipeDown?.Invoke(this, EventArgs.Empty);
                                LogicaJuego.Instance.OnTaskAction("¡Desliza hacia abajo!");
                            }
                        }
                    }

                    isSwiping = false; // Reinicia la bandera
                }
            }
        }

        // Detecta gestos de zoom
        if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            // Calcula la distancia actual entre los dos dedos
            float currentPinchDistance = Vector2.Distance(touch1.position, touch2.position);

            if (!isZooming && (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began))
            {
                initialPinchDistance = currentPinchDistance;
                isZooming = true;
            }
            else if (isZooming && (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved))
            {
                if (currentPinchDistance > initialPinchDistance * 1.2f) // Zoom In
                {
                    OnZoomIn?.Invoke(this, EventArgs.Empty);
                    LogicaJuego.Instance.OnTaskAction("¡Haz zoom hacia dentro!");
                    isZooming = false;
                }
                else if (currentPinchDistance < initialPinchDistance * 0.8f) // Zoom Out
                {
                    OnZoomOut?.Invoke(this, EventArgs.Empty);
                    LogicaJuego.Instance.OnTaskAction("¡Haz zoom hacia fuera!");
                    isZooming = false;
                }
            }
        }
        else
        {
            isZooming = false;
        }

        // Detecta una sacudida del dispositivo
        if (Input.acceleration.magnitude > 4.0f)
        {
            OnShake?.Invoke(this, EventArgs.Empty);
            LogicaJuego.Instance.OnTaskAction("¡Agita el teléfono!");
        }

        // Detecta cuando el dispositivo está boca abajo
        if (Input.deviceOrientation == DeviceOrientation.FaceDown)
        {
            OnLookDown?.Invoke(this, EventArgs.Empty);
            LogicaJuego.Instance.OnTaskAction("¡Ponlo boca abajo!");
        }

        // Detecta rotación hacia la derecha o izquierda
        float zRotation = Input.gyro.attitude.eulerAngles.z;

        // Corrige el salto entre 0° y 360°
        if (zRotation - previousZRotation > 180f)
        {
            zRotation -= 360f;
        }
        else if (zRotation - previousZRotation < -180f)
        {
            zRotation += 360f;
        }

        // Rotación hacia la derecha
        if (!hasRotatedRight && (zRotation - previousZRotation) > rotationSensitivity)
        {
            hasRotatedRight = true;
            OnRotateRight?.Invoke(this, EventArgs.Empty);
            LogicaJuego.Instance.OnTaskAction("¡Gira a la derecha!");
        }
        else if (hasRotatedRight && (zRotation - previousZRotation) < -rotationSensitivity / 2f)
        {
            hasRotatedRight = false; // Permite volver a detectar la rotación
        }

        // Rotación hacia la izquierda
        if (!hasRotatedLeft && (zRotation - previousZRotation) < -rotationSensitivity)
        {
            hasRotatedLeft = true;
            OnRotateLeft?.Invoke(this, EventArgs.Empty);
            LogicaJuego.Instance.OnTaskAction("¡Gira a la izquierda!");
        }
        else if (hasRotatedLeft && (zRotation - previousZRotation) > rotationSensitivity / 2f)
        {
            hasRotatedLeft = false; // Permite volver a detectar la rotación
        }

        // Actualiza el valor previo de zRotation
        previousZRotation = zRotation;
    }

}

