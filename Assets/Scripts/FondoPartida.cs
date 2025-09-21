using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FondoPartida : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Image backgroundRenderer;      
    [SerializeField] private Sprite defaultBackground;        
    [SerializeField] private BackgroundCatalogSO catalogoFondos; 

    private void Start()
    {
        string equippedID = PlayerPrefs.GetString("SelectedBackground", "DefaultBackground");
        Debug.Log($"[FondoPartida] ID cargado desde PlayerPrefs: {equippedID}");

        BackgroundDataSO backgroundData = catalogoFondos.backgroundDataSO.Find(b => b.id == equippedID);

        if (backgroundData != null && backgroundData.sprite != null)
        {
            backgroundRenderer.sprite = backgroundData.sprite;
            Debug.Log($"[FondoPartida] Fondo cargado correctamente: {backgroundData.id} -> {backgroundData.sprite.name}");
        }
        else
        {
            backgroundRenderer.sprite = defaultBackground;
            Debug.Log($"[FondoPartida] No se encontró el fondo con ID '{equippedID}' en el catálogo. Se aplica default: {defaultBackground.name}");
        }
    }
}
