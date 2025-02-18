using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PreviewFondos : MonoBehaviour
{
    // Hazlo singleton para facilitar el acceso desde los botones
    public static PreviewFondos Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject previewPanel; // El panel de preview (debe estar desactivado al iniciar)
    [SerializeField] private Image previewImage;      // El Image que muestra el fondo
    [SerializeField] private FondoSelector fondoSelector; // Referencia al script que aplica el fondo

    // Guarda el sprite seleccionado para aplicar si se confirma
    private Sprite selectedSprite;

    public GameObject botonesPantallas;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // Este método se llama desde los botones de la lista de fondos
    public void ShowPreview(Sprite nuevoFondo)
    {
        selectedSprite = nuevoFondo;
        previewImage.sprite = nuevoFondo;
        previewPanel.SetActive(true);
        botonesPantallas.SetActive(false);
    }

    // Llamado por el botón de confirmar en el panel de preview
    public void ConfirmBackground()
    {
        if (selectedSprite != null)
        {
            // Guarda el nombre del sprite seleccionado en PlayerPrefs
            PlayerPrefs.SetString("SelectedBackground", selectedSprite.name);
            PlayerPrefs.Save();

            // Aplica el fondo usando el script FondoSelector (si lo tienes)
            fondoSelector.CambiarFondo(selectedSprite);
        }
        previewPanel.SetActive(false);
        botonesPantallas.SetActive(true);
    }

    // Llamado por el botón de cancelar en el panel de preview
    public void CancelPreview()
    {
        previewPanel.SetActive(false);
        botonesPantallas.SetActive(true);
    }
}
