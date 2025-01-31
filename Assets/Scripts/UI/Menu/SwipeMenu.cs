using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwipeMenu : MonoBehaviour
{
   
    public RectTransform contentPanel; // Panel que contiene las pantallas
    private Vector2 startPosition; // Posición inicial para comparar el swipe
    private Vector2 targetPosition; // Posición objetivo del swipe
    private float lerpSpeed = 10f; // Velocidad de interpolación
    private int currentPage = 0; // Página actual
    private int totalPages = 2; // Número total de pantallas (Ajusta si agregas más)

    private void Start()
    {
        // Iniciar en la posición correcta del menú principal
        targetPosition = contentPanel.anchoredPosition ;
    }

    private void Update()
    {
        // Interpola suavemente entre la posición actual y la deseada
        contentPanel.anchoredPosition = Vector2.Lerp(contentPanel.anchoredPosition, targetPosition, Time.deltaTime * lerpSpeed);
    }

    public void OnBeginDrag()
    {
        // Guarda la posición inicial cuando el jugador empieza a deslizar
        startPosition = Input.mousePosition;
    }

    public void OnEndDrag()
    {
        // Obtiene la dirección del swipe
        Vector2 endPosition = Input.mousePosition;
        float swipeDelta = endPosition.x - startPosition.x;

        // Determina si se deslizó suficiente para cambiar de pantalla
        if (Mathf.Abs(swipeDelta) > Screen.width * 0.2f)
        {
            if (swipeDelta < 0 && currentPage < totalPages - 1)
            {
                // Ir a la siguiente página
                currentPage++;
            }
            else if (swipeDelta > 0 && currentPage > 0)
            {
                // Ir a la anterior página
                currentPage--;
            }
        }

        // Calcula la nueva posición del panel basado en la página actual
        targetPosition = new Vector2(-currentPage * Screen.width, contentPanel.anchoredPosition.y);
    }
}

