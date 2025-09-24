using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MaskBackgroundImage : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite defaultBackground;
    [SerializeField] private BackgroundCatalogSO catalogoFondos;

    private void Start()
    {
        string equippedID = PlayerPrefs.GetString("SelectedBackground", "DefaultBackground");

        BackgroundDataSO backgroundData = catalogoFondos.backgroundDataSO.Find(b => b.id == equippedID);

        if (backgroundData != null && backgroundData.sprite != null)
        {
            backgroundImage.sprite = backgroundData.sprite;
        }
        else
        {
            backgroundImage.sprite = defaultBackground;
        }
    }
}
