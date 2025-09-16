using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InstructionButton : MonoBehaviour
{
    [TextArea(3, 6)] public string instructions; // Texto explicativo para este minijuego
    [SerializeField] private InstructionPanel panel; // Arrastras el panel en el inspector

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            panel.Show(instructions);
        });
    }
}
