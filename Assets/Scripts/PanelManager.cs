using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelManager : MonoBehaviour
{
    [SerializeField] private GameObject panelFondos;

    [SerializeField] private Button fondosButton;

    private bool isPanelOpen = false;

    private void Awake()
    {
        panelFondos.SetActive(false);

        fondosButton.onClick.AddListener(() =>
        {
            TogglePanel();
        });
    }

    public void TogglePanel()
    {
        isPanelOpen = !isPanelOpen;
        panelFondos.SetActive(isPanelOpen);
    }
}
