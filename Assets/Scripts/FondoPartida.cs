using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FondoPartida : MonoBehaviour
{
    // Asigna este componente en el Inspector: es el SpriteRenderer del fondo de la partida.
    [SerializeField] private Image backgroundRenderer;
    // Nombre por defecto si no se ha seleccionado ninguno.
    [SerializeField] private string defaultBackgroundName = "DefaultBackground";

    private void Start()
    {
        // Lee el nombre del fondo seleccionado de PlayerPrefs.
        string selectedBackgroundName = PlayerPrefs.GetString("SelectedBackground", defaultBackgroundName);

        // Carga el sprite desde la carpeta Resources/Backgrounds
        Sprite newBackground = Resources.Load<Sprite>("Sprites/" + selectedBackgroundName);

        if (newBackground != null)
        {
            backgroundRenderer.sprite = newBackground;
            Debug.Log("Fondo cambiado a: " + selectedBackgroundName);
        }
        else
        {
            Debug.LogWarning("No se encontró el sprite para: " + selectedBackgroundName);
        }

        Image imageComponent = GetComponent<Image>();
        if (imageComponent != null)
        {
            imageComponent.color = new Color32(255, 255, 255, 255);
        }

    }
}
