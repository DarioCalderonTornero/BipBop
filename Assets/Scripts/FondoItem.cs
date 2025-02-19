using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FondoItem : MonoBehaviour
{
    [Header("Datos del Fondo")]
    public Sprite backgroundSprite;  // El sprite del fondo
    public string backgroundID;      // Identificador único, por ejemplo, "FondoRojo"
    public int price;                // Precio del fondo

    // Este método se llama cuando se pulsa el fondo (puedes asignarlo en el OnClick del botón)
    public void OnItemClicked()
    {
        // Llama al sistema de preview pasando el sprite, el ID y el precio
        PreviewFondos.Instance.ShowPreview(backgroundSprite, backgroundID, price);
    }
}
