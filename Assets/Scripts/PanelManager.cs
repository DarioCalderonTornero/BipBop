using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelManager : MonoBehaviour
{
    public GameObject panelFondos;

    private bool isPanelOpen = false;

    public void TogglePanel()
    {
        isPanelOpen = !isPanelOpen;
        panelFondos.SetActive(isPanelOpen);
    }
}
