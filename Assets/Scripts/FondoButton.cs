using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FondoButton : MonoBehaviour
{
    public Sprite fondoSprite; // Asigna el sprite en el Inspector

    public void OnButtonClicked()
    {
        // Llama al BackgroundPreviewManager para mostrar la preview
        PreviewFondos.Instance.ShowPreview(fondoSprite);
    }
}
