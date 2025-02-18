using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractionBlocker : MonoBehaviour
{
    public Button startButton; // Arrastra tu botón aquí desde el Inspector
    private float disableDuration = 0.3f; // Tiempo que estará desactivado
    [SerializeField] private TextMeshProUGUI recordText; // Asigna este Text desde el inspector
    [SerializeField] private TextMeshProUGUI recordTextGeometric; // Asigna este Text desde el inspector

    void Start()
    {
        if (startButton != null)
        {
            // Desactiva el botón al inicio
            startButton.interactable = false;

            // Reactiva el botón después de X segundos
            StartCoroutine(EnableButtonAfterDelay(disableDuration));
        }
        // Recupera el récord desde PlayerPrefs
        int maxRecord = PlayerPrefs.GetInt("MaxRecord", 0);

        // Actualiza el texto del récord en el menú
        recordText.text = $"Récord Máximo: {maxRecord}";

        int maxRecordGeometric = PlayerPrefs.GetInt("MaxRecordGeometric", 0);
        recordTextGeometric.text = $"Récord Máximo: {maxRecordGeometric}";

    }

    private IEnumerator EnableButtonAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        startButton.interactable = true; // Reactiva el botón
    }
}
