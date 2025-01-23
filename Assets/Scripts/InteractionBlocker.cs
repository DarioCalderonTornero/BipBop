using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InteractionBlocker : MonoBehaviour
{
    public Button startButton; // Arrastra tu botón aquí desde el Inspector
    private float disableDuration = 2f; // Tiempo que estará desactivado

    void Start()
    {
        if (startButton != null)
        {
            // Desactiva el botón al inicio
            startButton.interactable = false;

            // Reactiva el botón después de X segundos
            StartCoroutine(EnableButtonAfterDelay(disableDuration));
        }
    }

    private IEnumerator EnableButtonAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        startButton.interactable = true; // Reactiva el botón
    }
}
