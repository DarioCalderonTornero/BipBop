using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FondoItem : MonoBehaviour
{
    [SerializeField] private BackgroundDataSO backgroundDataSO;
    [SerializeField] private Image estadoIcono; // Ícono que muestra el estado (comprado / equipado / nada)

    [Header("Iconos de estado")]
    [SerializeField] private Sprite iconoComprado;
    [SerializeField] private Sprite iconoEquipado;

    private void Start()
    {
        // ActualizarIcono(); // Al iniciar, comprueba el estado actual
    }

    public void OnItemClicked()
    {
        if (PreviewFondos.Instance != null)
            PreviewFondos.Instance.ShowPreview(backgroundDataSO);
    }

    private void Update()
    {
        ActualizarIcono();
    }

    /// <summary>
    /// Actualiza el icono de estado del fondo según si está comprado o equipado.
    /// </summary>
    public void ActualizarIcono()
    {
        if (estadoIcono == null) return;

        string id = backgroundDataSO.id;
        bool comprado = PlayerPrefs.GetInt("Purchased_" + id, 0) == 1 || id == "DefaultBackground";
        string equipado = PlayerPrefs.GetString("SelectedBackground", "");

        if (equipado == id)
        {
            // Fondo actualmente equipado
            estadoIcono.sprite = iconoEquipado;
            estadoIcono.enabled = true;
        }
        else if (comprado)
        {
            // Fondo comprado pero no equipado
            estadoIcono.sprite = iconoComprado;
            estadoIcono.enabled = true;
        }
        else
        {
            // Fondo no comprado
            estadoIcono.enabled = false;
        }
    }
}
