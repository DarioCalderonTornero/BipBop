using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FPSButtonController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button fpsButton;
    [SerializeField] private TextMeshProUGUI fpsText;

    // -1 = AUTO
    private readonly int[] fpsValues = new int[] { 60, 90, 120, -1 };

    private int currentIndex = 0;

    private void Awake()
    {
        int saved = FPSManager.GetSavedFPS();

        // Buscar índice guardado
        for (int i = 0; i < fpsValues.Length; i++)
        {
            if (fpsValues[i] == saved)
            {
                currentIndex = i;
                break;
            }
        }

        ApplyFPS(false); // solo refresca texto

        fpsButton.onClick.AddListener(ChangeFPS);
    }

    private void ChangeFPS()
    {
        currentIndex++;

        if (currentIndex >= fpsValues.Length)
            currentIndex = 0;

        ApplyFPS(true);
    }

    private void ApplyFPS(bool applyToSystem)
    {
        int value = fpsValues[currentIndex];

        if (applyToSystem)
            FPSManager.SetFPS(value);

        // Actualizar texto
        if (value == -1)
            fpsText.text = "AUTO";
        else
            fpsText.text = value.ToString();
    }
}
