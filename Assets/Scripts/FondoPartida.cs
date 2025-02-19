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
        // Comprueba si ya se ha equipado un fondo; si no, lo establece como predeterminado.
        string selectedBackgroundName = PlayerPrefs.GetString("SelectedBackground", "");
        if (string.IsNullOrEmpty(selectedBackgroundName))
        {
            selectedBackgroundName = defaultBackgroundName;
            PlayerPrefs.SetString("SelectedBackground", defaultBackgroundName);
            PlayerPrefs.Save();
        }

        // Carga el sprite desde la carpeta Resources/Sprites
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

        // Asegura que el color del componente Image sea blanco
        Image imageComponent = GetComponent<Image>();
        if (imageComponent != null)
        {
            imageComponent.color = new Color32(255, 255, 255, 255);
        }
    }
}
