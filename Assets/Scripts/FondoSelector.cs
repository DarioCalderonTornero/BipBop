using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FondoSelector : MonoBehaviour
{
    public Image fondoPantalla; 

    public void CambiarFondo(Sprite nuevoFondo)
    {
        fondoPantalla.sprite = nuevoFondo;
    }
}
