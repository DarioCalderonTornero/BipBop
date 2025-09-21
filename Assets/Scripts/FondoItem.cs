using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FondoItem : MonoBehaviour
{
    [SerializeField] private BackgroundDataSO backgroundDataSO;
    public void OnItemClicked()
    {
        if (PreviewFondos.Instance != null)
            PreviewFondos.Instance.ShowPreview(backgroundDataSO);
    }
}
